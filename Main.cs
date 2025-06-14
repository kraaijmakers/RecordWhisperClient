using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecordWhisperClient.Services;
using RecordWhisperClient.UI;

namespace RecordWhisperClient
{
    public partial class MainForm : Form
    {
        private bool isExiting = false;
        private EventWaitHandle showConfigEvent;
        private Thread eventMonitorThread;

        // Settings
        private bool verboseNotifications = false;
        private bool showRecordingNotifications = true;
        private bool copyToClipboard = false;
        private string recordingsPath = "";
        private string folderSuffix = "_Recording";
        private string transcriptionLanguage = "auto";
        private string whisperServerUrl = "http://127.0.0.1:8080";
        private string apiKey = "";
        private bool transcriptionEnabled = true;
        private bool hotkeyEnabled = true;
        private bool hotkeyCtrl = true;
        private bool hotkeyShift = true;
        private bool hotkeyAlt = false;
        private string hotkeyKey = "R";
        private int inputDeviceIndex = -1;
        private bool volumeActivationEnabled = false;
        private float volumeThreshold = 0.01f;
        private int silenceTimeoutMs = 2000;

        // Services
        private AudioRecordingService audioService;
        private WhisperTranscriptionService whisperService;
        private HotkeyManager hotkeyManager;
        private SystemTrayManager systemTrayManager;
        private SettingsManager settingsManager;

        public MainForm()
        {
            InitializeComponent();
            Logger.Initialize();
            Logger.Info("Application starting up");
            InitializeServices();
            SetupInterProcessCommunication();
            LoadSettings();
            SetupRecording();
            SetupAudioService();
            RegisterEventHandlers();
            RegisterGlobalHotkey();
            
            if (settingsManager.IsFirstRun)
            {
                ShowFirstRunConfiguration();
            }
            else
            {
                // Initialize tray immediately to prevent app being unresponsive
                systemTrayManager.Initialize(verboseNotifications, copyToClipboard, transcriptionEnabled);
                
                // Test connection to Whisper server in background without blocking
                _ = Task.Run(async () => await TestWhisperConnectionAndStartup());
            }
            Logger.Info("Application startup completed");
        }

        private void InitializeComponent()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.Size = new Size(0, 0);
        }

        private void InitializeServices()
        {
            audioService = new AudioRecordingService();
            settingsManager = new SettingsManager();
            hotkeyManager = new HotkeyManager();
            systemTrayManager = new SystemTrayManager();
        }

        private void SetupInterProcessCommunication()
        {
            try
            {
                // Create named event for inter-process communication
                showConfigEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "RecorderWhisperClientShowConfig");
                
                // Start monitoring thread
                eventMonitorThread = new Thread(MonitorShowConfigEvent)
                {
                    IsBackground = true,
                    Name = "ConfigEventMonitor"
                };
                eventMonitorThread.Start();
                
                Logger.Info("Inter-process communication setup completed");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to setup inter-process communication", ex);
            }
        }

        private void MonitorShowConfigEvent()
        {
            try
            {
                while (!isExiting)
                {
                    if (showConfigEvent.WaitOne(1000)) // Wait with timeout
                    {
                        if (!isExiting)
                        {
                            Logger.Info("Received signal from second instance to show configuration");
                            this.Invoke(new Action(() => OpenConfiguration()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in configuration event monitor thread", ex);
            }
        }

        private void SetupAudioService()
        {
            audioService.SetInputDevice(inputDeviceIndex);
            audioService.SetVolumeActivation(volumeActivationEnabled, volumeThreshold, silenceTimeoutMs);
            Logger.Info($"Audio service configured with input device index: {inputDeviceIndex}, volume activation: {volumeActivationEnabled}");
        }

        private void SetupRecording()
        {
            Logger.Info($"Setting up recording directory: {recordingsPath}");
            try
            {
                if (!Directory.Exists(recordingsPath))
                {
                    Directory.CreateDirectory(recordingsPath);
                    Logger.Info("Recording directory created");
                }
                else
                {
                    Logger.Info("Recording directory already exists");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to setup recording directory", ex);
            }
        }

        private void RegisterEventHandlers()
        {
            // Audio service events
            audioService.RecordingStarted += OnRecordingStarted;
            audioService.RecordingStopped += OnRecordingStopped;

            // Hotkey manager events
            hotkeyManager.HotkeyPressed += (s, e) => ToggleRecording();

            // System tray events
            systemTrayManager.ToggleRecordingRequested += (s, e) => ToggleRecording();
            systemTrayManager.OpenRecordingsFolderRequested += (s, e) => OpenRecordingsFolder();
            systemTrayManager.OpenConfigurationRequested += (s, e) => OpenConfiguration();
            systemTrayManager.ExitApplicationRequested += (s, e) => ExitApplication();
            systemTrayManager.CopyToClipboardToggled += (s, e) => ToggleCopyToClipboard();
            systemTrayManager.TranscriptionToggled += (s, e) => ToggleTranscription();
        }

        private void SetupWhisperClient()
        {
            whisperService = new WhisperTranscriptionService(whisperServerUrl, transcriptionLanguage, apiKey);
        }

        private void UpdateHotkeyConfiguration()
        {
            Logger.Info($"Updating hotkey configuration: Enabled={hotkeyEnabled}, Ctrl={hotkeyCtrl}, Shift={hotkeyShift}, Alt={hotkeyAlt}, Key={hotkeyKey}");
            
            if (hotkeyEnabled)
            {
                hotkeyManager.SetHotkey(hotkeyCtrl, hotkeyShift, hotkeyAlt, hotkeyKey);
            }
            else
            {
                hotkeyManager.UnregisterHotkey();
                Logger.Info("Global hotkey disabled by user configuration");
            }
        }

        private async Task TestWhisperConnectionAndStartup()
        {
            try
            {
                Logger.Info("Starting Whisper server connection test");
                
                // Use a shorter timeout for startup test to prevent hanging
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    bool isConnected = await TestConnectionWithTimeout(cts.Token);
                    
                    if (isConnected)
                    {
                        Logger.Info("Whisper server connection test passed");
                        // Show startup notification since connection is good
                        this.Invoke(new Action(() => PlayStartupSound()));
                    }
                    else
                    {
                        Logger.Warning("Whisper server connection test failed");
                        // Show startup notification but with warning
                        this.Invoke(new Action(() => 
                        {
                            PlayStartupSound();
                            systemTrayManager.ShowNotification("Whisper Server Warning", 
                                "Cannot reach Whisper server - transcription disabled until server is available", 
                                ToolTipIcon.Warning);
                        }));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Warning("Whisper connection test timed out during startup");
                this.Invoke(new Action(() => 
                {
                    PlayStartupSound();
                    systemTrayManager.ShowNotification("Whisper Server Timeout", 
                        "Connection test timed out - check server availability in Configuration", 
                        ToolTipIcon.Warning);
                }));
            }
            catch (Exception ex)
            {
                Logger.Error("Error during Whisper connection test", ex);
                this.Invoke(new Action(() => 
                {
                    PlayStartupSound();
                    systemTrayManager.ShowNotification("Whisper Server Error", 
                        "Connection test failed - check Configuration settings", 
                        ToolTipIcon.Error);
                }));
            }
        }

        private async Task<bool> TestConnectionWithTimeout(CancellationToken cancellationToken)
        {
            try
            {
                var testTask = whisperService.TestConnection();
                var completedTask = await Task.WhenAny(testTask, Task.Delay(Timeout.Infinite, cancellationToken));
                
                if (completedTask == testTask)
                {
                    return await testTask;
                }
                else
                {
                    throw new OperationCanceledException("Connection test timed out");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task TestWhisperConnection()
        {
            try
            {
                Logger.Info("Starting Whisper server connection test");
                bool isConnected = await whisperService.TestConnection();
                
                if (isConnected)
                {
                    Logger.Info("Whisper server connection test passed");
                    systemTrayManager.ShowNotification("Connection Test", "Whisper server is accessible", System.Windows.Forms.ToolTipIcon.Info);
                }
                else
                {
                    Logger.Warning("Whisper server connection test failed");
                    
                    // Show error dialog on UI thread for configuration changes
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Warning: Cannot reach Whisper server at:\n{whisperServerUrl}\n\nTranscription will not work until the server is accessible.\n\nPlease check:\n• Server URL is correct\n• Server is running\n• Network connectivity\n• Firewall settings", 
                                       "Whisper Server Connection Warning", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Warning);
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error during Whisper connection test", ex);
                
                // Show error dialog on UI thread for configuration changes
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show($"Error testing connection to Whisper server:\n\n{ex.Message}\n\nTranscription may not work until the connection issue is resolved.\n\nCheck Configuration settings and server availability.", 
                                   "Whisper Server Connection Error", 
                                   MessageBoxButtons.OK, 
                                   MessageBoxIcon.Error);
                }));
            }
        }

        private void RegisterGlobalHotkey()
        {
            hotkeyManager.RegisterHotkey(this.Handle);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            hotkeyManager.ProcessHotkeyMessage(ref m);
        }

        private void ToggleRecording()
        {
            Logger.Info($"Toggle recording requested - current state: {(audioService.IsRecording ? "recording" : "ready")}");
            if (audioService.IsRecording)
            {
                audioService.StopRecording();
            }
            else
            {
                audioService.StartRecording(recordingsPath, folderSuffix);
            }
        }

        private void OnRecordingStarted(object sender, EventArgs e)
        {
            systemTrayManager.UpdateRecordingState(true);
            
            if (showRecordingNotifications)
            {
                systemTrayManager.ShowNotification("Recording Started", $"Recording to: {Path.GetFileName(Path.GetDirectoryName(audioService.CurrentRecordingPath))}");
            }
        }

        private async void OnRecordingStopped(object sender, RecordingStoppedEventArgs e)
        {
            systemTrayManager.UpdateRecordingState(false);

            if (showRecordingNotifications && verboseNotifications)
            {
                systemTrayManager.ShowNotification("Recording Stopped", $"Saved to: {Path.GetFileName(Path.GetDirectoryName(e.AudioFilePath))}");
            }

            if (transcriptionEnabled)
            {
                await TranscribeRecording(e.AudioFilePath);
            }
            else
            {
                Logger.Info("Transcription disabled - skipping transcription step");
            }
        }

        private async Task TranscribeRecording(string audioFilePath)
        {
            try
            {
                if (verboseNotifications)
                {
                    systemTrayManager.ShowNotification("Transcription", "Sending to Whisper server...");
                }

                string transcription = await whisperService.TranscribeAudio(audioFilePath);

                if (!string.IsNullOrWhiteSpace(transcription))
                {
                    await whisperService.SaveTranscription(audioFilePath, transcription, audioService);

                    if (copyToClipboard)
                    {
                        Logger.Info("Copying transcription to clipboard");
                        await CopyToClipboardSafe(transcription);
                        PlayClipboardCopySound();
                    }

                    if (verboseNotifications)
                    {
                        systemTrayManager.ShowNotification("Transcription Complete", copyToClipboard ? "Text saved and copied to clipboard" : "Text saved alongside audio file");
                    }
                }
                else
                {
                    if (verboseNotifications)
                    {
                        systemTrayManager.ShowNotification("Transcription", "No speech detected in recording");
                    }
                }
            }
            catch (Exception ex)
            {
                systemTrayManager.ShowNotification("Transcription Error", ex.Message);
            }
        }

        private void ToggleCopyToClipboard()
        {
            copyToClipboard = !copyToClipboard;
            systemTrayManager.UpdateMenuStates(verboseNotifications, copyToClipboard);
        }

        private void ToggleTranscription()
        {
            transcriptionEnabled = !transcriptionEnabled;
            SaveSettings();
            string status = transcriptionEnabled ? "enabled" : "disabled";
            systemTrayManager.ShowNotification("Transcription", $"Transcription {status}");
            Logger.Info($"Transcription toggled: {status}");
        }

        private async Task CopyToClipboardSafe(string text)
        {
            try
            {
                // Clean the text first
                string cleanText = CleanTextForClipboard(text);
                Logger.Info($"Preparing to copy text to clipboard. Length: {cleanText.Length} characters");

                // Try multiple times as clipboard can sometimes be locked
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => Clipboard.SetText(cleanText, TextDataFormat.UnicodeText)));
                        }
                        else
                        {
                            Clipboard.SetText(cleanText, TextDataFormat.UnicodeText);
                        }
                        Logger.Info("Successfully copied transcription to clipboard");
                        return;
                    }
                    catch (System.Runtime.InteropServices.ExternalException ex)
                    {
                        Logger.Warning($"Clipboard attempt {attempt + 1} failed: {ex.Message}");
                        if (attempt < 2)
                        {
                            await Task.Delay(100); // Wait a bit before retrying
                        }
                    }
                }
                
                Logger.Error("Failed to copy to clipboard after 3 attempts");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during clipboard copy operation", ex);
            }
        }

        private string CleanTextForClipboard(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Trim whitespace
            text = text.Trim();
            
            // Replace any null characters that might cause issues
            text = text.Replace('\0', ' ');
            
            // Normalize line endings
            text = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
            
            // Remove any other problematic characters that might render as asterisks
            var cleanText = new StringBuilder();
            foreach (char c in text)
            {
                // Keep printable characters, whitespace, and common punctuation
                if (char.IsControl(c))
                {
                    if (c == '\t' || c == '\n' || c == '\r')
                    {
                        cleanText.Append(c);
                    }
                    // Skip other control characters
                }
                else
                {
                    cleanText.Append(c);
                }
            }
            
            return cleanText.ToString();
        }

        private void PlayClipboardCopySound()
        {
            try
            {
                // Play a short system sound to indicate clipboard copy
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Ignore errors if sound can't be played
            }
        }

        private void PlayStartupSound()
        {
            try
            {
                Logger.Info("Playing startup sound and showing notification");
                System.Media.SystemSounds.Question.Play();

                string message = "Audio Recorder is ready";
                if (copyToClipboard)
                {
                    message += " - Copy to clipboard enabled";
                }
                systemTrayManager.ShowNotification("Recorder & Whisper.cpp client Started", message);
                Logger.Info("Startup sequence completed");
            }
            catch (Exception ex)
            {
                Logger.Error("Error playing startup sound", ex);
            }
        }

        private void OpenRecordingsFolder()
        {
            Process.Start("explorer.exe", recordingsPath);
        }

        private void LoadSettings()
        {
            Logger.Info("Loading application settings");
            try
            {
                settingsManager.LoadConfig();

                whisperServerUrl = settingsManager.GetValue("Server", "WhisperServerUrl", "http://127.0.0.1:8080");
                recordingsPath = settingsManager.GetValue("Recording", "RecordingsPath", 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Recordings"));
                folderSuffix = settingsManager.GetValue("Recording", "FolderSuffix", "_Recording");
                verboseNotifications = settingsManager.GetBoolValue("Options", "VerboseNotifications", false);
                showRecordingNotifications = settingsManager.GetBoolValue("Options", "ShowRecordingNotifications", true);
                copyToClipboard = settingsManager.GetBoolValue("Options", "CopyToClipboard", true);
                transcriptionEnabled = settingsManager.GetBoolValue("Options", "TranscriptionEnabled", true);
                transcriptionLanguage = settingsManager.GetValue("Options", "TranscriptionLanguage", "auto");
                apiKey = settingsManager.GetValue("Options", "ApiKey", "");

                // Load hotkey settings
                hotkeyEnabled = settingsManager.GetBoolValue("Hotkey", "Enabled", true);
                hotkeyCtrl = settingsManager.GetBoolValue("Hotkey", "Ctrl", true);
                hotkeyShift = settingsManager.GetBoolValue("Hotkey", "Shift", true);
                hotkeyAlt = settingsManager.GetBoolValue("Hotkey", "Alt", false);
                hotkeyKey = settingsManager.GetValue("Hotkey", "Key", "R");
                
                // Load input device setting
                inputDeviceIndex = settingsManager.GetIntValue("Audio", "InputDeviceIndex", -1);
                
                // Load volume activation settings
                volumeActivationEnabled = settingsManager.GetBoolValue("Audio", "VolumeActivationEnabled", false);
                volumeThreshold = (float)settingsManager.GetDoubleValue("Audio", "VolumeThreshold", 0.01);
                silenceTimeoutMs = settingsManager.GetIntValue("Audio", "SilenceTimeoutMs", 2000);

                SetupWhisperClient();
                UpdateHotkeyConfiguration();

                Logger.Info($"Settings loaded - Server: {whisperServerUrl}, Path: {recordingsPath}, Suffix: {folderSuffix}");
                Logger.Info($"Options - Verbose: {verboseNotifications}, Clipboard: {copyToClipboard}, Language: {transcriptionLanguage}");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load settings, using defaults", ex);
                whisperServerUrl = "http://127.0.0.1:8080";
                recordingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Recordings");
                folderSuffix = "_Recording";
                verboseNotifications = false;
                showRecordingNotifications = true;
                copyToClipboard = true;
                transcriptionEnabled = true;
                transcriptionLanguage = "auto";
                apiKey = "";
                hotkeyEnabled = true;
                hotkeyCtrl = true;
                hotkeyShift = true;
                hotkeyAlt = false;
                hotkeyKey = "R";
                inputDeviceIndex = -1;
                volumeActivationEnabled = false;
                volumeThreshold = 0.01f;
                silenceTimeoutMs = 2000;
                SetupWhisperClient();
                UpdateHotkeyConfiguration();
                Logger.Info("Using default settings due to config load failure");
            }
        }

        private void SaveSettings()
        {
            Logger.Info("Saving application settings");
            try
            {
                settingsManager.SetValue("Server", "WhisperServerUrl", whisperServerUrl);
                settingsManager.SetValue("Recording", "RecordingsPath", recordingsPath);
                settingsManager.SetValue("Recording", "FolderSuffix", folderSuffix);
                settingsManager.SetBoolValue("Options", "VerboseNotifications", verboseNotifications);
                settingsManager.SetBoolValue("Options", "ShowRecordingNotifications", showRecordingNotifications);
                settingsManager.SetBoolValue("Options", "CopyToClipboard", copyToClipboard);
                settingsManager.SetBoolValue("Options", "TranscriptionEnabled", transcriptionEnabled);
                settingsManager.SetValue("Options", "TranscriptionLanguage", transcriptionLanguage);
                settingsManager.SetValue("Options", "ApiKey", apiKey);
                
                // Save hotkey settings
                settingsManager.SetBoolValue("Hotkey", "Enabled", hotkeyEnabled);
                settingsManager.SetBoolValue("Hotkey", "Ctrl", hotkeyCtrl);
                settingsManager.SetBoolValue("Hotkey", "Shift", hotkeyShift);
                settingsManager.SetBoolValue("Hotkey", "Alt", hotkeyAlt);
                settingsManager.SetValue("Hotkey", "Key", hotkeyKey);
                
                // Save input device setting
                settingsManager.SetIntValue("Audio", "InputDeviceIndex", inputDeviceIndex);
                
                // Save volume activation settings
                settingsManager.SetBoolValue("Audio", "VolumeActivationEnabled", volumeActivationEnabled);
                settingsManager.SetDoubleValue("Audio", "VolumeThreshold", volumeThreshold);
                settingsManager.SetIntValue("Audio", "SilenceTimeoutMs", silenceTimeoutMs);
                
                settingsManager.SaveConfig();
                Logger.Info("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
                systemTrayManager.ShowNotification("Settings Error", "Failed to save settings");
            }
        }

        private void ShowFirstRunConfiguration()
        {
            Logger.Info("First run detected - showing configuration dialog");
            systemTrayManager.Initialize(verboseNotifications, copyToClipboard, transcriptionEnabled);
            
            using (var configForm = new ConfigurationForm(whisperServerUrl, recordingsPath, folderSuffix, verboseNotifications, showRecordingNotifications, copyToClipboard, transcriptionEnabled, transcriptionLanguage, apiKey, inputDeviceIndex, volumeActivationEnabled, volumeThreshold, silenceTimeoutMs, hotkeyEnabled, hotkeyCtrl, hotkeyShift, hotkeyAlt, hotkeyKey))
            {
                configForm.Text = "First Run Configuration - Recorder & Whisper.cpp client";
                
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    string oldServerUrl = whisperServerUrl;
                    
                    whisperServerUrl = configForm.WhisperServerUrl;
                    recordingsPath = configForm.RecordingsPath;
                    folderSuffix = configForm.FolderSuffix;
                    verboseNotifications = configForm.VerboseNotifications;
                    showRecordingNotifications = configForm.ShowRecordingNotifications;
                    copyToClipboard = configForm.CopyToClipboard;
                    transcriptionEnabled = configForm.TranscriptionEnabled;
                    transcriptionLanguage = configForm.TranscriptionLanguage;
                    apiKey = configForm.ApiKey;
                    hotkeyEnabled = configForm.HotkeyEnabled;
                    hotkeyCtrl = configForm.HotkeyCtrl;
                    hotkeyShift = configForm.HotkeyShift;
                    hotkeyAlt = configForm.HotkeyAlt;
                    hotkeyKey = configForm.HotkeyKey;
                    inputDeviceIndex = configForm.InputDeviceIndex;
                    volumeActivationEnabled = configForm.VolumeActivationEnabled;
                    volumeThreshold = configForm.VolumeThreshold;
                    silenceTimeoutMs = configForm.SilenceTimeoutMs;

                    SetupWhisperClient();
                    systemTrayManager.UpdateMenuStates(verboseNotifications, copyToClipboard);
                    SetupRecording();
                    SetupAudioService();
                    UpdateHotkeyConfiguration();
                    SaveSettings();
                    
                    // Test connection if server URL changed
                    if (oldServerUrl != whisperServerUrl)
                    {
                        _ = Task.Run(async () => await TestWhisperConnection());
                    }
                    
                    PlayStartupSound();
                    Logger.Info("First run configuration completed");
                }
                else
                {
                    Logger.Info("First run configuration cancelled - using defaults");
                    PlayStartupSound();
                }
            }
        }

        private void OpenConfiguration()
        {
            using (var configForm = new ConfigurationForm(whisperServerUrl, recordingsPath, folderSuffix, verboseNotifications, showRecordingNotifications, copyToClipboard, transcriptionEnabled, transcriptionLanguage, apiKey, inputDeviceIndex, volumeActivationEnabled, volumeThreshold, silenceTimeoutMs, hotkeyEnabled, hotkeyCtrl, hotkeyShift, hotkeyAlt, hotkeyKey))
            {
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    string oldServerUrl = whisperServerUrl;
                    string oldApiKey = apiKey;
                    
                    whisperServerUrl = configForm.WhisperServerUrl;
                    recordingsPath = configForm.RecordingsPath;
                    folderSuffix = configForm.FolderSuffix;
                    verboseNotifications = configForm.VerboseNotifications;
                    showRecordingNotifications = configForm.ShowRecordingNotifications;
                    copyToClipboard = configForm.CopyToClipboard;
                    transcriptionEnabled = configForm.TranscriptionEnabled;
                    transcriptionLanguage = configForm.TranscriptionLanguage;
                    apiKey = configForm.ApiKey;
                    hotkeyEnabled = configForm.HotkeyEnabled;
                    hotkeyCtrl = configForm.HotkeyCtrl;
                    hotkeyShift = configForm.HotkeyShift;
                    hotkeyAlt = configForm.HotkeyAlt;
                    hotkeyKey = configForm.HotkeyKey;
                    inputDeviceIndex = configForm.InputDeviceIndex;
                    volumeActivationEnabled = configForm.VolumeActivationEnabled;
                    volumeThreshold = configForm.VolumeThreshold;
                    silenceTimeoutMs = configForm.SilenceTimeoutMs;

                    SetupWhisperClient();
                    systemTrayManager.UpdateMenuStates(verboseNotifications, copyToClipboard);
                    SetupAudioService();
                    UpdateHotkeyConfiguration();

                    SaveSettings();
                    
                    // Test connection if server URL or API key changed
                    if (oldServerUrl != whisperServerUrl || oldApiKey != apiKey)
                    {
                        _ = Task.Run(async () => await TestWhisperConnection());
                    }
                    else
                    {
                        systemTrayManager.ShowNotification("Configuration", "Settings updated");
                    }
                }
            }
        }

        private void ExitApplication()
        {
            Logger.Info("Application exit requested");
            if (audioService.IsRecording)
            {
                Logger.Info("Stopping recording before exit");
                audioService.StopRecording();
            }

            isExiting = true;
            hotkeyManager.UnregisterHotkey();
            systemTrayManager.Hide();
            Logger.Info("Application exiting");
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isExiting)
            {
                // If not explicitly exiting, just hide to system tray
                e.Cancel = true;
                this.Hide();
            }
            // If isExiting is true, allow the form to close normally
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.Info("Disposing application resources");
                try
                {
                    isExiting = true;
                    
                    hotkeyManager?.UnregisterHotkey();
                    systemTrayManager?.Dispose();
                    audioService?.Dispose();
                    whisperService?.Dispose();
                    
                    // Clean up inter-process communication
                    showConfigEvent?.Set(); // Wake up waiting thread
                    eventMonitorThread?.Join(2000); // Wait for thread to exit
                    showConfigEvent?.Dispose();
                    
                    Logger.Info("All resources disposed successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during resource disposal", ex);
                }
            }
            base.Dispose(disposing);
        }
    }

    // Program entry point
    public static class Program
    {
        private static Mutex applicationMutex = null;

        [STAThread]
        public static void Main()
        {
            const string mutexName = "RecorderWhisperClientSingleInstance";
            bool createdNew;

            try
            {
                applicationMutex = new Mutex(true, mutexName, out createdNew);

                if (!createdNew)
                {
                    // Application is already running, signal it to show configuration
                    SignalRunningInstanceToShowConfig();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            finally
            {
                applicationMutex?.ReleaseMutex();
                applicationMutex?.Dispose();
            }
        }

        private static void SignalRunningInstanceToShowConfig()
        {
            try
            {
                // Use a named event to signal the running instance
                using (var showConfigEvent = EventWaitHandle.OpenExisting("RecorderWhisperClientShowConfig"))
                {
                    showConfigEvent.Set();
                }
            }
            catch
            {
                // Fallback to message box if event not found or can't be signaled
                MessageBox.Show("Recorder & Whisper.cpp client is already running.", 
                               "Application Already Running", 
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Information);
            }
        }
    }
}