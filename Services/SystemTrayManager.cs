using System;
using System.Drawing;
using System.Windows.Forms;
using RecordWhisperClient.UI;
using RecordWhisperClient.Services;

namespace RecordWhisperClient.Services
{
    public class SystemTrayManager
    {
        public event EventHandler ToggleRecordingRequested;
        public event EventHandler OpenRecordingsFolderRequested;
        public event EventHandler OpenConfigurationRequested;
        public event EventHandler ExitApplicationRequested;
        public event EventHandler CopyToClipboardToggled;
        public event EventHandler TranscriptionToggled;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private Icon currentIcon;
        private bool isConnectionOk = true;
        private bool isTranscriptionEnabled = false;
        private bool isCurrentlyRecording = false;

        public void Initialize(bool verboseNotifications, bool copyToClipboard, bool transcriptionEnabled = false)
        {
            isTranscriptionEnabled = transcriptionEnabled;
            CreateContextMenu(copyToClipboard, transcriptionEnabled);
            CreateNotifyIcon();
        }

        private void CreateContextMenu(bool copyToClipboard, bool transcriptionEnabled)
        {
            contextMenu = new ContextMenuStrip();
            ApplyContextMenuTheme();

            contextMenu.Items.Add("Start/Stop Recording (Ctrl+Shift+R)", null, (s, e) => ToggleRecordingRequested?.Invoke(this, EventArgs.Empty));
            contextMenu.Items.Add("-");

            var transcriptionItem = (ToolStripMenuItem)contextMenu.Items.Add("Enable Transcription", null, (s, e) => TranscriptionToggled?.Invoke(this, EventArgs.Empty));
            transcriptionItem.Checked = transcriptionEnabled;

            var clipboardItem = (ToolStripMenuItem)contextMenu.Items.Add("Automatically copy to Clipboard", null, (s, e) => CopyToClipboardToggled?.Invoke(this, EventArgs.Empty));
            clipboardItem.Checked = copyToClipboard;

            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Open Recordings Folder", null, (s, e) => OpenRecordingsFolderRequested?.Invoke(this, EventArgs.Empty));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Configuration...", null, (s, e) => OpenConfigurationRequested?.Invoke(this, EventArgs.Empty));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplicationRequested?.Invoke(this, EventArgs.Empty));
        }

        private void CreateNotifyIcon()
        {
            currentIcon = CreateRecordingIcon(false);
            notifyIcon = new NotifyIcon()
            {
                Icon = currentIcon,
                ContextMenuStrip = contextMenu,
                Visible = true,
                Text = "Recorder & Whisper.cpp client - Ready"
            };

            notifyIcon.DoubleClick += (s, e) => ToggleRecordingRequested?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateRecordingState(bool isRecording)
        {
            isCurrentlyRecording = isRecording;
            UpdateIcon();
            notifyIcon.Text = isRecording ? "Recorder & Whisper.cpp client - Recording..." : "Recorder & Whisper.cpp client - Ready";
        }

        public void UpdateConnectionStatus(bool connectionOk)
        {
            isConnectionOk = connectionOk;
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            // Dispose previous icon to prevent resource leaks
            currentIcon?.Dispose();
            
            currentIcon = CreateRecordingIcon(isCurrentlyRecording);
            notifyIcon.Icon = currentIcon;
        }

        public void UpdateMenuStates(bool copyToClipboard, bool transcriptionEnabled = false)
        {
            isTranscriptionEnabled = transcriptionEnabled;
            foreach (ToolStripItem item in contextMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text == "Enable Transcription")
                    {
                        menuItem.Checked = transcriptionEnabled;
                    }
                    else if (menuItem.Text == "Automatically copy to Clipboard")
                    {
                        menuItem.Checked = copyToClipboard;
                    }
                }
            }
            UpdateIcon();
        }

        public void ShowNotification(string title, string message, System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.Info)
        {
            notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }

        private Icon CreateRecordingIcon(bool isRecording)
        {
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                if (isRecording)
                {
                    Color accentColor = ThemeHelper.GetAccentColor();
                    using (var brush = new SolidBrush(accentColor))
                    {
                        g.FillEllipse(brush, 2, 2, 12, 12);
                    }

                    Color borderColor = Color.FromArgb(Math.Max(0, accentColor.R - 30),
                                                       Math.Max(0, accentColor.G - 30),
                                                       Math.Max(0, accentColor.B - 30));
                    using (var pen = new Pen(borderColor, 1))
                    {
                        g.DrawEllipse(pen, 2, 2, 12, 12);
                    }
                }
                else
                {
                    g.FillEllipse(Brushes.Gray, 2, 2, 12, 12);
                    g.DrawEllipse(Pens.Black, 2, 2, 12, 12);
                }

                // Add warning overlay if connection is not OK and transcription is enabled
                if (!isConnectionOk && isTranscriptionEnabled)
                {
                    DrawWarningOverlay(g);
                }
            }

            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void DrawWarningOverlay(Graphics g)
        {
            // Draw warning triangle in bottom right corner
            int triangleSize = 6;
            int x = 16 - triangleSize - 1;
            int y = 16 - triangleSize - 1;

            // Create triangle points
            Point[] trianglePoints = new Point[]
            {
                new Point(x + triangleSize / 2, y),      // Top point
                new Point(x, y + triangleSize),          // Bottom left
                new Point(x + triangleSize, y + triangleSize) // Bottom right
            };

            // Fill triangle with yellow/orange warning color
            using (var warningBrush = new SolidBrush(Color.Orange))
            {
                g.FillPolygon(warningBrush, trianglePoints);
            }

            // Draw triangle border
            using (var warningPen = new Pen(Color.DarkOrange, 1))
            {
                g.DrawPolygon(warningPen, trianglePoints);
            }

            // Draw exclamation mark
            using (var textBrush = new SolidBrush(Color.Black))
            {
                // Draw exclamation mark line
                g.FillRectangle(textBrush, x + triangleSize / 2, y + 1, 1, 3);
                // Draw exclamation mark dot
                g.FillRectangle(textBrush, x + triangleSize / 2, y + 5, 1, 1);
            }
        }

        private void ApplyContextMenuTheme()
        {
            if (contextMenu != null)
            {
                contextMenu.BackColor = ThemeHelper.GetControlBackgroundColor();
                contextMenu.ForeColor = ThemeHelper.GetForegroundColor();
                contextMenu.Renderer = new ThemedToolStripRenderer();
            }
        }

        public void Dispose()
        {
            Logger.Info("Disposing system tray resources");
            try
            {
                currentIcon?.Dispose();
                notifyIcon?.Dispose();
                contextMenu?.Dispose();
                Logger.Info("System tray resources disposed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during system tray disposal", ex);
            }
        }

        public void Hide()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                currentIcon?.Dispose();
                currentIcon = null;
            }
        }
    }
}