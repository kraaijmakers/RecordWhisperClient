using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RecordWhisperClient.UI;
using NAudio.Wave;

namespace RecordWhisperClient.UI
{
    public partial class ConfigurationForm : Form
    {
        public string WhisperServerUrl { get; private set; }
        public string RecordingsPath { get; private set; }
        public string FolderSuffix { get; private set; }
        public bool VerboseNotifications { get; private set; }
        public bool ShowRecordingNotifications { get; private set; }
        public bool CopyToClipboard { get; private set; }
        public bool TranscriptionEnabled { get; private set; }
        public string TranscriptionLanguage { get; private set; }
        public string ApiKey { get; private set; }
        public int InputDeviceIndex { get; private set; }
        public bool VolumeActivationEnabled { get; private set; }
        public float VolumeThreshold { get; private set; }
        public int SilenceTimeoutMs { get; private set; }
        public bool HotkeyEnabled { get; private set; }
        public bool HotkeyCtrl { get; private set; }
        public bool HotkeyShift { get; private set; }
        public bool HotkeyAlt { get; private set; }
        public string HotkeyKey { get; private set; }

        private TextBox urlTextBox;
        private TextBox pathTextBox;
        private TextBox suffixTextBox;
        private TextBox languageTextBox;
        private TextBox apiKeyTextBox;
        private ComboBox notificationModeComboBox;
        private CheckBox clipboardCheckBox;
        private CheckBox transcriptionEnabledCheckBox;
        private ComboBox inputDeviceComboBox;
        private CheckBox volumeActivationCheckBox;
        private TrackBar volumeThresholdTrackBar;
        private NumericUpDown silenceTimeoutNumeric;
        private Button browseButton;
        private Button testConnectionButton;
        private Button openLogsButton;
        private Button okButton;
        private Button cancelButton;
        private Label urlLabel;
        private Label pathLabel;
        private Label suffixLabel;
        private Label languageLabel;
        private Label apiKeyLabel;
        private Label inputDeviceLabel;
        private Label volumeThresholdLabel;
        private Label silenceTimeoutLabel;
        private Label notificationModeLabel;
        private Label optionsLabel;
        private Label hotkeyLabel;
        private CheckBox hotkeyEnabledCheckBox;
        private CheckBox hotkeyCtrlCheckBox;
        private CheckBox hotkeyShiftCheckBox;
        private CheckBox hotkeyAltCheckBox;
        private ComboBox hotkeyKeyComboBox;
        private Label versionLabel;

        public ConfigurationForm(string currentUrl, string currentPath, string currentSuffix, bool verboseNotifs, bool showRecordingNotifications, bool copyClipboard, bool transcriptionEnabled, string currentLanguage, string currentApiKey = "", int inputDeviceIndex = -1, bool volumeActivationEnabled = false, float volumeThreshold = 0.01f, int silenceTimeoutMs = 2000, bool hotkeyEnabled = true, bool hotkeyCtrl = true, bool hotkeyShift = true, bool hotkeyAlt = false, string hotkeyKey = "R")
        {
            WhisperServerUrl = currentUrl;
            RecordingsPath = currentPath;
            FolderSuffix = currentSuffix;
            VerboseNotifications = verboseNotifs;
            ShowRecordingNotifications = showRecordingNotifications;
            CopyToClipboard = copyClipboard;
            TranscriptionEnabled = transcriptionEnabled;
            TranscriptionLanguage = currentLanguage;
            ApiKey = currentApiKey;
            InputDeviceIndex = inputDeviceIndex;
            VolumeActivationEnabled = volumeActivationEnabled;
            VolumeThreshold = volumeThreshold;
            SilenceTimeoutMs = silenceTimeoutMs;
            HotkeyEnabled = hotkeyEnabled;
            HotkeyCtrl = hotkeyCtrl;
            HotkeyShift = hotkeyShift;
            HotkeyAlt = hotkeyAlt;
            HotkeyKey = hotkeyKey;
            InitializeComponent();
            ApplyTheme();
            EnableDarkModeForTitleBar();
            
            // Set initial state of hotkey controls
            HotkeyEnabled_CheckedChanged(null, null);
        }

        private void InitializeComponent()
        {
            this.Text = "Configuration";
            this.Size = new Size(520, 839);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // URL label
            urlLabel = new Label
            {
                Text = "Whisper Server URL (OpenAI-compatible transcription service):",
                Location = new Point(12, 12),
                Size = new Size(480, 20),
                AutoSize = false
            };
            this.Controls.Add(urlLabel);

            // URL text box
            urlTextBox = new TextBox
            {
                Text = WhisperServerUrl,
                Location = new Point(12, 35),
                Size = new Size(380, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(urlTextBox);

            // Test Connection button
            testConnectionButton = new Button
            {
                Text = "Test Connection",
                Location = new Point(397, 35),
                Size = new Size(95, 23),
                FlatStyle = FlatStyle.Flat
            };
            testConnectionButton.Click += TestConnectionButton_Click;
            this.Controls.Add(testConnectionButton);

            // Path label
            pathLabel = new Label
            {
                Text = "Recordings Folder Path:",
                Location = new Point(12, 70),
                Size = new Size(200, 20),
                AutoSize = false
            };
            this.Controls.Add(pathLabel);

            // Path text box
            pathTextBox = new TextBox
            {
                Text = RecordingsPath,
                Location = new Point(12, 93),
                Size = new Size(400, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(pathTextBox);

            // Browse button
            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(417, 93),
                Size = new Size(75, 23),
                FlatStyle = FlatStyle.Flat
            };
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            // Suffix label
            suffixLabel = new Label
            {
                Text = "Folder Name Suffix:",
                Location = new Point(12, 128),
                Size = new Size(200, 20),
                AutoSize = false
            };
            this.Controls.Add(suffixLabel);

            // Suffix text box
            suffixTextBox = new TextBox
            {
                Text = FolderSuffix,
                Location = new Point(12, 151),
                Size = new Size(200, 23)
            };
            this.Controls.Add(suffixTextBox);

            // Language label
            languageLabel = new Label
            {
                Text = "Transcription Language (auto, en, es, fr, etc.):",
                Location = new Point(12, 186),
                Size = new Size(300, 20),
                AutoSize = false
            };
            this.Controls.Add(languageLabel);

            // Language text box
            languageTextBox = new TextBox
            {
                Text = TranscriptionLanguage,
                Location = new Point(12, 209),
                Size = new Size(200, 23)
            };
            this.Controls.Add(languageTextBox);

            // API Key label
            apiKeyLabel = new Label
            {
                Text = "API Key (optional, for OpenAI-compatible services):",
                Location = new Point(12, 244),
                Size = new Size(300, 20),
                AutoSize = false
            };
            this.Controls.Add(apiKeyLabel);

            // API Key text box
            apiKeyTextBox = new TextBox
            {
                Text = ApiKey,
                Location = new Point(12, 267),
                Size = new Size(480, 23),
                UseSystemPasswordChar = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(apiKeyTextBox);

            // Input Device label
            inputDeviceLabel = new Label
            {
                Text = "Audio Input Device:",
                Location = new Point(12, 302),
                Size = new Size(200, 20),
                AutoSize = false
            };
            this.Controls.Add(inputDeviceLabel);

            // Input Device dropdown
            inputDeviceComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(12, 325),
                Size = new Size(480, 23),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(inputDeviceComboBox);

            // Volume Activation checkbox
            volumeActivationCheckBox = new CheckBox
            {
                Text = "Enable Volume Activation (automatic recording on sound)",
                Checked = VolumeActivationEnabled,
                Location = new Point(12, 360),
                Size = new Size(480, 23),
                FlatStyle = FlatStyle.Flat
            };
            volumeActivationCheckBox.CheckedChanged += VolumeActivation_CheckedChanged;
            this.Controls.Add(volumeActivationCheckBox);

            // Volume Threshold label
            volumeThresholdLabel = new Label
            {
                Text = $"Volume Threshold: {(VolumeThreshold * 100):F0}%",
                Location = new Point(12, 393),
                Size = new Size(200, 20),
                AutoSize = false
            };
            this.Controls.Add(volumeThresholdLabel);

            // Volume Threshold trackbar
            volumeThresholdTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = 20,
                Value = (int)(VolumeThreshold * 100),
                Location = new Point(12, 416),
                Size = new Size(300, 45),
                TickFrequency = 5
            };
            volumeThresholdTrackBar.ValueChanged += VolumeThreshold_ValueChanged;
            this.Controls.Add(volumeThresholdTrackBar);

            // Silence Timeout label
            silenceTimeoutLabel = new Label
            {
                Text = "Silence Timeout (seconds):",
                Location = new Point(330, 393),
                Size = new Size(150, 20),
                AutoSize = false
            };
            this.Controls.Add(silenceTimeoutLabel);

            // Silence Timeout numeric
            silenceTimeoutNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 30,
                Value = SilenceTimeoutMs / 1000,
                Location = new Point(330, 416),
                Size = new Size(60, 23),
                DecimalPlaces = 0
            };
            this.Controls.Add(silenceTimeoutNumeric);

            // Options label
            optionsLabel = new Label
            {
                Text = "Options:",
                Location = new Point(12, 475),
                Size = new Size(200, 20),
                AutoSize = false
            };
            this.Controls.Add(optionsLabel);

            // Notification Mode label
            notificationModeLabel = new Label
            {
                Text = "Notification Mode:",
                Location = new Point(12, 498),
                Size = new Size(150, 20),
                AutoSize = false
            };
            this.Controls.Add(notificationModeLabel);

            // Notification Mode dropdown
            notificationModeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(12, 521),
                Size = new Size(200, 23),
                FlatStyle = FlatStyle.Flat
            };
            notificationModeComboBox.Items.AddRange(new string[] { "None", "Recording", "Verbose" });
            
            // Set initial selection based on current settings
            if (!ShowRecordingNotifications && !VerboseNotifications)
                notificationModeComboBox.SelectedIndex = 0; // None
            else if (ShowRecordingNotifications && !VerboseNotifications)
                notificationModeComboBox.SelectedIndex = 1; // Recording
            else
                notificationModeComboBox.SelectedIndex = 2; // Verbose
                
            this.Controls.Add(notificationModeComboBox);

            // Copy to clipboard checkbox
            clipboardCheckBox = new CheckBox
            {
                Text = "Automatically copy to Clipboard",
                Checked = CopyToClipboard,
                Location = new Point(12, 554),
                Size = new Size(200, 23),
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(clipboardCheckBox);

            // Transcription enabled checkbox
            transcriptionEnabledCheckBox = new CheckBox
            {
                Text = "Enable Transcription",
                Checked = TranscriptionEnabled,
                Location = new Point(12, 577),
                Size = new Size(200, 23),
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(transcriptionEnabledCheckBox);

            // Hotkey label
            hotkeyLabel = new Label
            {
                Text = "Recording Hotkey:",
                Location = new Point(12, 612),
                Size = new Size(200, 20),
                AutoSize = false
            };
            this.Controls.Add(hotkeyLabel);

            // Hotkey Enabled checkbox
            hotkeyEnabledCheckBox = new CheckBox
            {
                Text = "Enable Global Hotkey",
                Checked = HotkeyEnabled,
                Location = new Point(12, 635),
                Size = new Size(150, 23),
                FlatStyle = FlatStyle.Flat
            };
            hotkeyEnabledCheckBox.CheckedChanged += HotkeyEnabled_CheckedChanged;
            this.Controls.Add(hotkeyEnabledCheckBox);

            // Hotkey Ctrl checkbox
            hotkeyCtrlCheckBox = new CheckBox
            {
                Text = "Ctrl",
                Checked = HotkeyCtrl,
                Location = new Point(12, 658),
                Size = new Size(60, 23),
                FlatStyle = FlatStyle.Flat
            };
            hotkeyCtrlCheckBox.CheckedChanged += HotkeyModifier_CheckedChanged;
            this.Controls.Add(hotkeyCtrlCheckBox);

            // Hotkey Shift checkbox
            hotkeyShiftCheckBox = new CheckBox
            {
                Text = "Shift",
                Checked = HotkeyShift,
                Location = new Point(80, 658),
                Size = new Size(60, 23),
                FlatStyle = FlatStyle.Flat
            };
            hotkeyShiftCheckBox.CheckedChanged += HotkeyModifier_CheckedChanged;
            this.Controls.Add(hotkeyShiftCheckBox);

            // Hotkey Alt checkbox
            hotkeyAltCheckBox = new CheckBox
            {
                Text = "Alt",
                Checked = HotkeyAlt,
                Location = new Point(148, 658),
                Size = new Size(50, 23),
                FlatStyle = FlatStyle.Flat
            };
            hotkeyAltCheckBox.CheckedChanged += HotkeyModifier_CheckedChanged;
            this.Controls.Add(hotkeyAltCheckBox);

            // Hotkey Key dropdown
            hotkeyKeyComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(206, 658),
                Size = new Size(80, 23),
                FlatStyle = FlatStyle.Flat
            };
            
            // Add key options
            string[] keys = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", 
                             "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", 
                             "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                             "SPACE" };
            hotkeyKeyComboBox.Items.AddRange(keys);
            hotkeyKeyComboBox.Text = HotkeyKey;
            this.Controls.Add(hotkeyKeyComboBox);

            // Populate input devices
            PopulateInputDevices();

            // Version label
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var productName = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Recorder & Whisper.cpp client";
            
            versionLabel = new Label
            {
                Text = $"{productName} v{version?.ToString(3) ?? "1.0.0"}",
                Location = new Point(12, 693),
                Size = new Size(480, 20),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 8.5f, FontStyle.Italic)
            };
            this.Controls.Add(versionLabel);

            // Open logs button
            openLogsButton = new Button
            {
                Text = "Open Log Folder",
                Location = new Point(12, 757),
                Size = new Size(120, 23),
                FlatStyle = FlatStyle.Flat
            };
            openLogsButton.Click += OpenLogsButton_Click;
            this.Controls.Add(openLogsButton);

            // OK button
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(335, 757),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            // Cancel button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(416, 757),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            
            // Set initial state of volume controls
            VolumeActivation_CheckedChanged(null, null);
        }

        private void VolumeActivation_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = volumeActivationCheckBox.Checked;
            volumeThresholdLabel.Enabled = enabled;
            volumeThresholdTrackBar.Enabled = enabled;
            silenceTimeoutLabel.Enabled = enabled;
            silenceTimeoutNumeric.Enabled = enabled;
        }

        private void VolumeThreshold_ValueChanged(object sender, EventArgs e)
        {
            volumeThresholdLabel.Text = $"Volume Threshold: {volumeThresholdTrackBar.Value}%";
        }

        private void PopulateInputDevices()
        {
            try
            {
                inputDeviceComboBox.Items.Clear();
                
                // Add "Default Device" as first option
                inputDeviceComboBox.Items.Add("Default Device");
                
                // Add all available input devices
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var capabilities = WaveIn.GetCapabilities(i);
                    inputDeviceComboBox.Items.Add($"{capabilities.ProductName}");
                }
                
                // Set selection based on current InputDeviceIndex
                if (InputDeviceIndex == -1)
                {
                    inputDeviceComboBox.SelectedIndex = 0; // Default Device
                }
                else if (InputDeviceIndex < WaveIn.DeviceCount)
                {
                    inputDeviceComboBox.SelectedIndex = InputDeviceIndex + 1; // +1 because of "Default Device" at index 0
                }
                else
                {
                    inputDeviceComboBox.SelectedIndex = 0; // Fallback to default
                }
            }
            catch (Exception ex)
            {
                // If there's an error enumerating devices, just add a default option
                inputDeviceComboBox.Items.Clear();
                inputDeviceComboBox.Items.Add("Default Device");
                inputDeviceComboBox.SelectedIndex = 0;
                
                MessageBox.Show($"Error enumerating audio devices: {ex.Message}\nUsing default device.", 
                               "Audio Device Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select recordings folder";
                folderDialog.SelectedPath = pathTextBox.Text;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    pathTextBox.Text = folderDialog.SelectedPath;
                }
            }
        }

        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable button during test
                testConnectionButton.Enabled = false;
                testConnectionButton.Text = "Testing...";
                
                // Create temporary WhisperTranscriptionService with current form values
                string testUrl = urlTextBox.Text.Trim();
                string testApiKey = apiKeyTextBox.Text.Trim();
                string testLanguage = languageTextBox.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(testUrl))
                {
                    MessageBox.Show("Please enter a Whisper Server URL before testing.", "Missing URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                using (var testService = new RecordWhisperClient.Services.WhisperTranscriptionService(testUrl, testLanguage, testApiKey))
                {
                    bool isConnected = await testService.TestConnection();
                    
                    if (isConnected)
                    {
                        MessageBox.Show("Connection test successful!\n\nThe Whisper server is accessible and responding.", 
                                       "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Connection test failed.\n\nCannot reach the Whisper server at:\n{testUrl}\n\nPlease check:\n• Server URL is correct\n• Server is running\n• Network connectivity\n• Firewall settings", 
                                       "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during connection test:\n\n{ex.Message}", 
                               "Connection Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable button
                testConnectionButton.Enabled = true;
                testConnectionButton.Text = "Test Connection";
            }
        }

        private void OpenLogsButton_Click(object sender, EventArgs e)
        {
            try
            {
                string logFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Recorder & Whisper.cpp client");
                
                if (System.IO.Directory.Exists(logFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logFolder);
                }
                else
                {
                    MessageBox.Show("Log folder does not exist yet.", "Log Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening log folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HotkeyEnabled_CheckedChanged(object sender, EventArgs e)
        {
            // Enable/disable hotkey configuration controls based on hotkey enabled status
            bool enabled = hotkeyEnabledCheckBox.Checked;
            hotkeyCtrlCheckBox.Enabled = enabled;
            hotkeyShiftCheckBox.Enabled = enabled;
            hotkeyAltCheckBox.Enabled = enabled;
            hotkeyKeyComboBox.Enabled = enabled;
        }

        private void HotkeyModifier_CheckedChanged(object sender, EventArgs e)
        {
            // Only validate if hotkey is enabled
            if (!hotkeyEnabledCheckBox.Checked)
                return;

            // Ensure at least one modifier key is always selected
            if (!hotkeyCtrlCheckBox.Checked && !hotkeyShiftCheckBox.Checked && !hotkeyAltCheckBox.Checked)
            {
                // If user tried to uncheck the last modifier, prevent it and show a message
                CheckBox checkBox = sender as CheckBox;
                if (checkBox != null)
                {
                    // Temporarily remove the event handler to prevent recursion
                    checkBox.CheckedChanged -= HotkeyModifier_CheckedChanged;
                    checkBox.Checked = true;
                    checkBox.CheckedChanged += HotkeyModifier_CheckedChanged;
                    
                    MessageBox.Show("At least one modifier key (Ctrl, Shift, or Alt) must be selected for the hotkey.", 
                                   "Hotkey Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(urlTextBox.Text))
            {
                MessageBox.Show("Please enter a valid URL.", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(pathTextBox.Text))
            {
                MessageBox.Show("Please enter a valid recordings path.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(suffixTextBox.Text))
            {
                MessageBox.Show("Please enter a folder suffix.", "Invalid Suffix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(languageTextBox.Text))
            {
                MessageBox.Show("Please enter a transcription language.", "Invalid Language", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate hotkey only if enabled
            if (hotkeyEnabledCheckBox.Checked)
            {
                if (!hotkeyCtrlCheckBox.Checked && !hotkeyShiftCheckBox.Checked && !hotkeyAltCheckBox.Checked)
                {
                    MessageBox.Show("Please select at least one modifier key (Ctrl, Shift, or Alt) for the hotkey.", "Invalid Hotkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(hotkeyKeyComboBox.Text))
                {
                    MessageBox.Show("Please select a key for the hotkey.", "Invalid Hotkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            WhisperServerUrl = urlTextBox.Text.Trim();
            RecordingsPath = pathTextBox.Text.Trim();
            FolderSuffix = suffixTextBox.Text.Trim();
            TranscriptionLanguage = languageTextBox.Text.Trim();
            ApiKey = apiKeyTextBox.Text.Trim();
            // Set notification mode based on dropdown selection
            switch (notificationModeComboBox.SelectedIndex)
            {
                case 0: // None
                    VerboseNotifications = false;
                    ShowRecordingNotifications = false;
                    break;
                case 1: // Recording
                    VerboseNotifications = false;
                    ShowRecordingNotifications = true;
                    break;
                case 2: // Verbose
                    VerboseNotifications = true;
                    ShowRecordingNotifications = true;
                    break;
                default:
                    VerboseNotifications = false;
                    ShowRecordingNotifications = true;
                    break;
            }
            CopyToClipboard = clipboardCheckBox.Checked;
            TranscriptionEnabled = transcriptionEnabledCheckBox.Checked;
            VolumeActivationEnabled = volumeActivationCheckBox.Checked;
            VolumeThreshold = volumeThresholdTrackBar.Value / 100f;
            SilenceTimeoutMs = (int)(silenceTimeoutNumeric.Value * 1000);
            HotkeyEnabled = hotkeyEnabledCheckBox.Checked;
            HotkeyCtrl = hotkeyCtrlCheckBox.Checked;
            HotkeyShift = hotkeyShiftCheckBox.Checked;
            HotkeyAlt = hotkeyAltCheckBox.Checked;
            HotkeyKey = hotkeyKeyComboBox.Text;
            
            // Set InputDeviceIndex based on selection
            if (inputDeviceComboBox.SelectedIndex == 0)
            {
                InputDeviceIndex = -1; // Default Device
            }
            else
            {
                InputDeviceIndex = inputDeviceComboBox.SelectedIndex - 1; // -1 because "Default Device" is at index 0
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeHelper.GetBackgroundColor();
            this.ForeColor = ThemeHelper.GetForegroundColor();

            // Apply theme to labels
            urlLabel.BackColor = ThemeHelper.GetBackgroundColor();
            urlLabel.ForeColor = ThemeHelper.GetForegroundColor();
            pathLabel.BackColor = ThemeHelper.GetBackgroundColor();
            pathLabel.ForeColor = ThemeHelper.GetForegroundColor();
            suffixLabel.BackColor = ThemeHelper.GetBackgroundColor();
            suffixLabel.ForeColor = ThemeHelper.GetForegroundColor();
            languageLabel.BackColor = ThemeHelper.GetBackgroundColor();
            languageLabel.ForeColor = ThemeHelper.GetForegroundColor();
            apiKeyLabel.BackColor = ThemeHelper.GetBackgroundColor();
            apiKeyLabel.ForeColor = ThemeHelper.GetForegroundColor();
            inputDeviceLabel.BackColor = ThemeHelper.GetBackgroundColor();
            inputDeviceLabel.ForeColor = ThemeHelper.GetForegroundColor();
            optionsLabel.BackColor = ThemeHelper.GetBackgroundColor();
            optionsLabel.ForeColor = ThemeHelper.GetForegroundColor();

            // Apply theme to text boxes
            urlTextBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            urlTextBox.ForeColor = ThemeHelper.GetForegroundColor();
            urlTextBox.BorderStyle = BorderStyle.FixedSingle;

            pathTextBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            pathTextBox.ForeColor = ThemeHelper.GetForegroundColor();
            pathTextBox.BorderStyle = BorderStyle.FixedSingle;

            suffixTextBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            suffixTextBox.ForeColor = ThemeHelper.GetForegroundColor();
            suffixTextBox.BorderStyle = BorderStyle.FixedSingle;

            languageTextBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            languageTextBox.ForeColor = ThemeHelper.GetForegroundColor();
            languageTextBox.BorderStyle = BorderStyle.FixedSingle;

            apiKeyTextBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            apiKeyTextBox.ForeColor = ThemeHelper.GetForegroundColor();
            apiKeyTextBox.BorderStyle = BorderStyle.FixedSingle;

            // Apply theme to checkboxes
            clipboardCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            clipboardCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            clipboardCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            clipboardCheckBox.FlatAppearance.BorderSize = 1;

            transcriptionEnabledCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            transcriptionEnabledCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            transcriptionEnabledCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            transcriptionEnabledCheckBox.FlatAppearance.BorderSize = 1;

            // Apply theme to hotkey controls
            hotkeyLabel.BackColor = ThemeHelper.GetBackgroundColor();
            hotkeyLabel.ForeColor = ThemeHelper.GetForegroundColor();

            hotkeyEnabledCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            hotkeyEnabledCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            hotkeyEnabledCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            hotkeyEnabledCheckBox.FlatAppearance.BorderSize = 1;

            hotkeyCtrlCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            hotkeyCtrlCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            hotkeyCtrlCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            hotkeyCtrlCheckBox.FlatAppearance.BorderSize = 1;

            hotkeyShiftCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            hotkeyShiftCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            hotkeyShiftCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            hotkeyShiftCheckBox.FlatAppearance.BorderSize = 1;

            hotkeyAltCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            hotkeyAltCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            hotkeyAltCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            hotkeyAltCheckBox.FlatAppearance.BorderSize = 1;

            hotkeyKeyComboBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            hotkeyKeyComboBox.ForeColor = ThemeHelper.GetForegroundColor();

            inputDeviceComboBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            inputDeviceComboBox.ForeColor = ThemeHelper.GetForegroundColor();

            volumeActivationCheckBox.BackColor = ThemeHelper.GetBackgroundColor();
            volumeActivationCheckBox.ForeColor = ThemeHelper.GetForegroundColor();
            volumeActivationCheckBox.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            volumeActivationCheckBox.FlatAppearance.BorderSize = 1;

            volumeThresholdLabel.BackColor = ThemeHelper.GetBackgroundColor();
            volumeThresholdLabel.ForeColor = ThemeHelper.GetForegroundColor();
            
            silenceTimeoutLabel.BackColor = ThemeHelper.GetBackgroundColor();
            silenceTimeoutLabel.ForeColor = ThemeHelper.GetForegroundColor();

            silenceTimeoutNumeric.BackColor = ThemeHelper.GetControlBackgroundColor();
            silenceTimeoutNumeric.ForeColor = ThemeHelper.GetForegroundColor();

            notificationModeLabel.BackColor = ThemeHelper.GetBackgroundColor();
            notificationModeLabel.ForeColor = ThemeHelper.GetForegroundColor();
            
            notificationModeComboBox.BackColor = ThemeHelper.GetControlBackgroundColor();
            notificationModeComboBox.ForeColor = ThemeHelper.GetForegroundColor();

            // Apply theme to version label
            versionLabel.BackColor = ThemeHelper.GetBackgroundColor();
            versionLabel.ForeColor = Color.FromArgb(128, 128, 128); // Slightly dimmed color for version text

            // Apply theme to buttons
            browseButton.BackColor = ThemeHelper.GetButtonBackgroundColor();
            browseButton.ForeColor = ThemeHelper.GetForegroundColor();
            browseButton.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            browseButton.FlatAppearance.BorderSize = 1;

            testConnectionButton.BackColor = ThemeHelper.GetButtonBackgroundColor();
            testConnectionButton.ForeColor = ThemeHelper.GetForegroundColor();
            testConnectionButton.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            testConnectionButton.FlatAppearance.BorderSize = 1;

            openLogsButton.BackColor = ThemeHelper.GetButtonBackgroundColor();
            openLogsButton.ForeColor = ThemeHelper.GetForegroundColor();
            openLogsButton.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            openLogsButton.FlatAppearance.BorderSize = 1;

            okButton.BackColor = ThemeHelper.GetButtonBackgroundColor();
            okButton.ForeColor = ThemeHelper.GetForegroundColor();
            okButton.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            okButton.FlatAppearance.BorderSize = 1;

            cancelButton.BackColor = ThemeHelper.GetButtonBackgroundColor();
            cancelButton.ForeColor = ThemeHelper.GetForegroundColor();
            cancelButton.FlatAppearance.BorderColor = ThemeHelper.GetBorderColor();
            cancelButton.FlatAppearance.BorderSize = 1;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private void EnableDarkModeForTitleBar()
        {
            try
            {
                if (!ThemeHelper.IsLightTheme())
                {
                    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
                    int useImmersiveDarkMode = 1;
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
                }
            }
            catch
            {
                // Ignore errors - this is a best-effort enhancement
            }
        }
    }
}