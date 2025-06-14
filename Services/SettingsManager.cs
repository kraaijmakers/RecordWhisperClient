using System;
using System.Collections.Generic;
using System.IO;
using RecordWhisperClient.Services;

namespace RecordWhisperClient.Services
{
    public class SettingsManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Recorder & Whisper.cpp client",
            "config.ini");
        
        private Dictionary<string, Dictionary<string, string>> _config = new Dictionary<string, Dictionary<string, string>>();
        
        public bool IsFirstRun { get; private set; } = false;

        public void LoadConfig()
        {
            _config.Clear();

            string settingsDir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            if (!File.Exists(ConfigPath))
            {
                IsFirstRun = true;
                CreateDefaultConfig();
                return;
            }

            try
            {
                string currentSection = "";
                var lines = File.ReadAllLines(ConfigPath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!_config.ContainsKey(currentSection))
                            _config[currentSection] = new Dictionary<string, string>();
                    }
                    else if (trimmedLine.Contains("=") && !string.IsNullOrEmpty(currentSection))
                    {
                        string[] parts = trimmedLine.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            _config[currentSection][parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Config load failed", ex);
                CreateDefaultConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                var lines = new List<string>();
                lines.Add("; Recorder & Whisper.cpp client Configuration File");
                lines.Add("; Generated automatically - modify with care");
                lines.Add($"; Last updated: {DateTime.Now}");
                lines.Add("");

                foreach (var section in _config)
                {
                    lines.Add($"[{section.Key}]");
                    foreach (var kvp in section.Value)
                    {
                        lines.Add($"{kvp.Key}={kvp.Value}");
                    }
                    lines.Add("");
                }

                File.WriteAllLines(ConfigPath, lines);
            }
            catch (Exception ex)
            {
                Logger.Error("Config save failed", ex);
            }
        }

        private void CreateDefaultConfig()
        {
            _config["Server"] = new Dictionary<string, string>
            {
                ["WhisperServerUrl"] = "http://127.0.0.1:8080"
            };

            _config["Recording"] = new Dictionary<string, string>
            {
                ["RecordingsPath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Recordings"),
                ["FolderSuffix"] = "_Recording"
            };

            _config["Options"] = new Dictionary<string, string>
            {
                ["VerboseNotifications"] = "false",
                ["CopyToClipboard"] = "true",
                ["TranscriptionLanguage"] = "auto",
                ["ApiKey"] = ""
            };

            _config["Hotkey"] = new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                ["Ctrl"] = "true",
                ["Shift"] = "true",
                ["Alt"] = "false",
                ["Key"] = "R"
            };

            SaveConfig();
        }

        public string GetValue(string section, string key, string defaultValue = "")
        {
            if (_config.ContainsKey(section) && _config[section].ContainsKey(key))
                return _config[section][key];
            return defaultValue;
        }

        public bool GetBoolValue(string section, string key, bool defaultValue = false)
        {
            string value = GetValue(section, key, defaultValue.ToString().ToLower());
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        public void SetValue(string section, string key, string value)
        {
            if (!_config.ContainsKey(section))
                _config[section] = new Dictionary<string, string>();
            _config[section][key] = value;
        }

        public void SetBoolValue(string section, string key, bool value)
        {
            SetValue(section, key, value.ToString().ToLower());
        }

        public int GetIntValue(string section, string key, int defaultValue = 0)
        {
            string value = GetValue(section, key, defaultValue.ToString());
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        public void SetIntValue(string section, string key, int value)
        {
            SetValue(section, key, value.ToString());
        }

        public double GetDoubleValue(string section, string key, double defaultValue = 0.0)
        {
            string value = GetValue(section, key, defaultValue.ToString());
            return double.TryParse(value, out double result) ? result : defaultValue;
        }

        public void SetDoubleValue(string section, string key, double value)
        {
            SetValue(section, key, value.ToString());
        }
    }
}