using System;
using System.IO;
using System.Timers;
using NAudio.Wave;
using RecordWhisperClient.Services;

namespace RecordWhisperClient.Services
{
    public class AudioRecordingService
    {
        public event EventHandler RecordingStarted;
        public event EventHandler<RecordingStoppedEventArgs> RecordingStopped;

        private WaveInEvent waveIn;
        private WaveInEvent monitoringWaveIn;
        private WaveFileWriter waveFileWriter;
        private string currentRecordingPath;
        private bool isRecording = false;
        private int inputDeviceIndex = -1;
        
        // Volume threshold settings
        private bool volumeActivationEnabled = false;
        private float volumeThreshold = 0.01f; // Default threshold (1%)
        private int silenceTimeoutMs = 2000; // 2 seconds of silence before stopping
        private Timer silenceTimer;
        private bool isMonitoring = false;
        private string recordingsBasePath = "";
        private string recordingFolderSuffix = "";

        public bool IsRecording => isRecording;
        public string CurrentRecordingPath => currentRecordingPath;
        public bool IsMonitoring => isMonitoring;
        public bool VolumeActivationEnabled => volumeActivationEnabled;

        public void SetInputDevice(int deviceIndex)
        {
            inputDeviceIndex = deviceIndex;
            Logger.Info($"Input device set to index: {deviceIndex}");
        }

        public void SetVolumeActivation(bool enabled, float threshold = 0.01f, int silenceTimeoutMs = 2000)
        {
            volumeActivationEnabled = enabled;
            volumeThreshold = threshold;
            this.silenceTimeoutMs = silenceTimeoutMs;
            
            Logger.Info($"Volume activation set: enabled={enabled}, threshold={threshold:P1}, timeout={silenceTimeoutMs}ms");
            
            if (enabled && !isMonitoring && !isRecording)
            {
                StartVolumeMonitoring();
            }
            else if (!enabled && isMonitoring)
            {
                StopVolumeMonitoring();
            }
        }

        public void StartVolumeMonitoring()
        {
            if (isMonitoring || !volumeActivationEnabled)
                return;

            try
            {
                Logger.Info("Starting volume monitoring for automatic recording");
                
                monitoringWaveIn = new WaveInEvent();
                
                // Set the input device if specified
                if (inputDeviceIndex >= 0 && inputDeviceIndex < WaveIn.DeviceCount)
                {
                    monitoringWaveIn.DeviceNumber = inputDeviceIndex;
                }
                
                monitoringWaveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, mono
                monitoringWaveIn.DataAvailable += OnMonitoringDataAvailable;
                
                // Setup silence timer
                silenceTimer = new Timer(silenceTimeoutMs);
                silenceTimer.Elapsed += OnSilenceTimeout;
                silenceTimer.AutoReset = false;
                
                monitoringWaveIn.StartRecording();
                isMonitoring = true;
                Logger.Info("Volume monitoring started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start volume monitoring", ex);
                isMonitoring = false;
            }
        }

        public void StopVolumeMonitoring()
        {
            if (!isMonitoring)
                return;

            try
            {
                Logger.Info("Stopping volume monitoring");
                
                if (monitoringWaveIn != null)
                {
                    monitoringWaveIn.StopRecording();
                    monitoringWaveIn.DataAvailable -= OnMonitoringDataAvailable;
                    monitoringWaveIn.Dispose();
                    monitoringWaveIn = null;
                }
                
                if (silenceTimer != null)
                {
                    silenceTimer.Stop();
                    silenceTimer.Elapsed -= OnSilenceTimeout;
                    silenceTimer.Dispose();
                    silenceTimer = null;
                }
                
                isMonitoring = false;
                Logger.Info("Volume monitoring stopped");
            }
            catch (Exception ex)
            {
                Logger.Error("Error stopping volume monitoring", ex);
            }
        }

        public void StartRecording(string recordingsPath, string folderSuffix)
        {
            // Store paths for volume activation use
            recordingsBasePath = recordingsPath;
            recordingFolderSuffix = folderSuffix;
            
            Logger.Info("Starting recording session");
            try
            {
                // Stop volume monitoring when manually starting recording
                if (isMonitoring)
                {
                    StopVolumeMonitoring();
                }
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string recordingFolder = Path.Combine(recordingsPath, $"{timestamp}{folderSuffix}");
                Logger.Info($"Creating recording folder: {recordingFolder}");
                
                Directory.CreateDirectory(recordingFolder);
                
                currentRecordingPath = Path.Combine(recordingFolder, "audio.wav");
                Logger.Info($"Recording to file: {currentRecordingPath}");

                waveIn = new WaveInEvent();
                
                // Set the input device if specified
                if (inputDeviceIndex >= 0 && inputDeviceIndex < WaveIn.DeviceCount)
                {
                    waveIn.DeviceNumber = inputDeviceIndex;
                    var deviceInfo = WaveIn.GetCapabilities(inputDeviceIndex);
                    Logger.Info($"Using input device {inputDeviceIndex}: {deviceInfo.ProductName}");
                }
                else
                {
                    Logger.Info("Using default input device");
                }
                
                waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, mono
                Logger.Info($"Audio format: {waveIn.WaveFormat.SampleRate}Hz, {waveIn.WaveFormat.Channels} channel(s)");
                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += OnRecordingStopped;

                waveFileWriter = new WaveFileWriter(currentRecordingPath, waveIn.WaveFormat);

                waveIn.StartRecording();
                isRecording = true;
                Logger.Info("Recording started successfully");

                RecordingStarted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start recording", ex);
                throw;
            }
        }

        public void StopRecording()
        {
            Logger.Info("Stopping recording session");
            try
            {
                if (waveIn != null)
                {
                    Logger.Info("Stopping wave input");
                    waveIn.StopRecording();
                    waveIn.Dispose();
                    waveIn = null;
                }

                if (waveFileWriter != null)
                {
                    Logger.Info("Closing wave file writer");
                    waveFileWriter.Dispose();
                    waveFileWriter = null;
                }

                isRecording = false;
                Logger.Info($"Recording stopped. File saved to: {currentRecordingPath}");

                if (File.Exists(currentRecordingPath))
                {
                    var fileInfo = new FileInfo(currentRecordingPath);
                    Logger.Info($"Recording file size: {fileInfo.Length} bytes ({fileInfo.Length / 1024.0:F1} KB)");
                }

                RecordingStopped?.Invoke(this, new RecordingStoppedEventArgs(currentRecordingPath));
                
                // Restart volume monitoring if it was enabled
                if (volumeActivationEnabled && !isMonitoring)
                {
                    StartVolumeMonitoring();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to stop recording", ex);
                throw;
            }
        }

        public string GetAudioDuration(string audioFilePath)
        {
            try
            {
                using (var audioFile = new AudioFileReader(audioFilePath))
                {
                    return audioFile.TotalTime.ToString(@"mm\:ss");
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
                waveFileWriter.Flush();
                
                // Reset silence timer if we're recording and volume activation is enabled
                if (volumeActivationEnabled && silenceTimer != null)
                {
                    float volume = CalculateRmsVolume(e.Buffer, e.BytesRecorded);
                    
                    if (volume > volumeThreshold)
                    {
                        // Sound detected - reset silence timer
                        silenceTimer.Stop();
                        silenceTimer.Start();
                    }
                }
            }
        }

        private void OnMonitoringDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!volumeActivationEnabled || isRecording)
                return;

            try
            {
                // Calculate RMS volume level
                float volume = CalculateRmsVolume(e.Buffer, e.BytesRecorded);
                
                if (volume > volumeThreshold)
                {
                    Logger.Info($"Volume threshold exceeded: {volume:P2} > {volumeThreshold:P2} - starting recording");
                    
                    // Stop monitoring and start recording
                    StopVolumeMonitoring();
                    StartRecording(recordingsBasePath, recordingFolderSuffix);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing monitoring data", ex);
            }
        }

        private void OnSilenceTimeout(object sender, ElapsedEventArgs e)
        {
            if (isRecording)
            {
                Logger.Info("Silence timeout reached - stopping recording");
                StopRecording();
            }
        }

        private float CalculateRmsVolume(byte[] buffer, int bytesRecorded)
        {
            if (bytesRecorded == 0)
                return 0;

            float sum = 0;
            int sampleCount = bytesRecorded / 2; // 16-bit samples = 2 bytes each
            
            for (int i = 0; i < bytesRecorded; i += 2)
            {
                if (i + 1 < bytesRecorded)
                {
                    // Convert bytes to 16-bit sample
                    short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    float normalizedSample = sample / 32768f; // Normalize to -1.0 to 1.0
                    sum += normalizedSample * normalizedSample;
                }
            }
            
            if (sampleCount > 0)
            {
                return (float)Math.Sqrt(sum / sampleCount);
            }
            
            return 0;
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Dispose();
                waveFileWriter = null;
            }
        }

        public void Dispose()
        {
            Logger.Info("Disposing audio recording resources");
            try
            {
                StopVolumeMonitoring();
                waveIn?.Dispose();
                waveFileWriter?.Dispose();
                Logger.Info("Audio recording resources disposed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during audio recording disposal", ex);
            }
        }
    }

    public class RecordingStoppedEventArgs : EventArgs
    {
        public string AudioFilePath { get; }

        public RecordingStoppedEventArgs(string audioFilePath)
        {
            AudioFilePath = audioFilePath;
        }
    }
}