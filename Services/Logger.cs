using System;
using System.IO;

namespace RecordWhisperClient.Services
{
    public class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Recorder & Whisper.cpp client", "recorder-whisper-client.log");

        public static void Initialize()
        {
            try
            {
                string logDir = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                if (File.Exists(LogFilePath))
                {
                    var fileInfo = new FileInfo(LogFilePath);
                    if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
                    {
                        string backupPath = LogFilePath.Replace(".log", $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.Move(LogFilePath, backupPath);
                    }
                }
            }
            catch
            {
                // Ignore logging setup errors
            }
        }

        public static void Info(string message)
        {
            LogMessage("INFO", message);
        }

        public static void Warning(string message)
        {
            LogMessage("WARN", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            string fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            LogMessage("ERROR", fullMessage);
            if (ex != null)
            {
                LogMessage("ERROR", $"Stack trace: {ex.StackTrace}");
            }
        }

        private static void LogMessage(string level, string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }
    }
}