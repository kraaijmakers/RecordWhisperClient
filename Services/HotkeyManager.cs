using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RecordWhisperClient.Services;

namespace RecordWhisperClient.Services
{
    public class HotkeyManager
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const int MOD_CTRL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_ALT = 0x0001;

        public event EventHandler HotkeyPressed;

        private IntPtr windowHandle;
        private bool isRegistered = false;
        private int currentModifiers = MOD_CTRL | MOD_SHIFT;
        private int currentKey = 0x52; // R key

        public bool RegisterHotkey(IntPtr handle)
        {
            windowHandle = handle;
            string hotkeyDescription = GetHotkeyDescription();
            Logger.Info($"Registering global hotkey {hotkeyDescription}");
            bool success = RegisterHotKey(windowHandle, HOTKEY_ID, currentModifiers, currentKey);

            if (success)
            {
                isRegistered = true;
                Logger.Info("Global hotkey registered successfully");
            }
            else
            {
                Logger.Error("Failed to register global hotkey");
            }

            return success;
        }

        public void UnregisterHotkey()
        {
            if (isRegistered && windowHandle != IntPtr.Zero)
            {
                Logger.Info("Unregistering global hotkey");
                UnregisterHotKey(windowHandle, HOTKEY_ID);
                isRegistered = false;
            }
        }

        public bool ProcessHotkeyMessage(ref Message m)
        {
            if (m.Msg == 0x0312) // WM_HOTKEY
            {
                if (m.WParam.ToInt32() == HOTKEY_ID)
                {
                    string hotkeyDescription = GetHotkeyDescription();
                    Logger.Info($"Global hotkey activated ({hotkeyDescription})");
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            return false;
        }

        public void SetHotkey(bool ctrl, bool shift, bool alt, string key)
        {
            if (isRegistered)
            {
                UnregisterHotkey();
            }

            currentModifiers = 0;
            if (ctrl) currentModifiers |= MOD_CTRL;
            if (shift) currentModifiers |= MOD_SHIFT;
            if (alt) currentModifiers |= MOD_ALT;

            currentKey = GetVirtualKeyCode(key);

            if (windowHandle != IntPtr.Zero)
            {
                RegisterHotkey(windowHandle);
            }
        }

        private int GetVirtualKeyCode(string key)
        {
            switch (key.ToUpper())
            {
                case "A": return 0x41;
                case "B": return 0x42;
                case "C": return 0x43;
                case "D": return 0x44;
                case "E": return 0x45;
                case "F": return 0x46;
                case "G": return 0x47;
                case "H": return 0x48;
                case "I": return 0x49;
                case "J": return 0x4A;
                case "K": return 0x4B;
                case "L": return 0x4C;
                case "M": return 0x4D;
                case "N": return 0x4E;
                case "O": return 0x4F;
                case "P": return 0x50;
                case "Q": return 0x51;
                case "R": return 0x52;
                case "S": return 0x53;
                case "T": return 0x54;
                case "U": return 0x55;
                case "V": return 0x56;
                case "W": return 0x57;
                case "X": return 0x58;
                case "Y": return 0x59;
                case "Z": return 0x5A;
                case "0": return 0x30;
                case "1": return 0x31;
                case "2": return 0x32;
                case "3": return 0x33;
                case "4": return 0x34;
                case "5": return 0x35;
                case "6": return 0x36;
                case "7": return 0x37;
                case "8": return 0x38;
                case "9": return 0x39;
                case "F1": return 0x70;
                case "F2": return 0x71;
                case "F3": return 0x72;
                case "F4": return 0x73;
                case "F5": return 0x74;
                case "F6": return 0x75;
                case "F7": return 0x76;
                case "F8": return 0x77;
                case "F9": return 0x78;
                case "F10": return 0x79;
                case "F11": return 0x7A;
                case "F12": return 0x7B;
                case "SPACE": return 0x20;
                case "ENTER": return 0x0D;
                case "TAB": return 0x09;
                case "ESC": return 0x1B;
                default: return 0x52; // Default to R key
            }
        }

        private string GetKeyName(int virtualKeyCode)
        {
            switch (virtualKeyCode)
            {
                case 0x41: return "A";
                case 0x42: return "B";
                case 0x43: return "C";
                case 0x44: return "D";
                case 0x45: return "E";
                case 0x46: return "F";
                case 0x47: return "G";
                case 0x48: return "H";
                case 0x49: return "I";
                case 0x4A: return "J";
                case 0x4B: return "K";
                case 0x4C: return "L";
                case 0x4D: return "M";
                case 0x4E: return "N";
                case 0x4F: return "O";
                case 0x50: return "P";
                case 0x51: return "Q";
                case 0x52: return "R";
                case 0x53: return "S";
                case 0x54: return "T";
                case 0x55: return "U";
                case 0x56: return "V";
                case 0x57: return "W";
                case 0x58: return "X";
                case 0x59: return "Y";
                case 0x5A: return "Z";
                case 0x30: return "0";
                case 0x31: return "1";
                case 0x32: return "2";
                case 0x33: return "3";
                case 0x34: return "4";
                case 0x35: return "5";
                case 0x36: return "6";
                case 0x37: return "7";
                case 0x38: return "8";
                case 0x39: return "9";
                case 0x70: return "F1";
                case 0x71: return "F2";
                case 0x72: return "F3";
                case 0x73: return "F4";
                case 0x74: return "F5";
                case 0x75: return "F6";
                case 0x76: return "F7";
                case 0x77: return "F8";
                case 0x78: return "F9";
                case 0x79: return "F10";
                case 0x7A: return "F11";
                case 0x7B: return "F12";
                case 0x20: return "SPACE";
                case 0x0D: return "ENTER";
                case 0x09: return "TAB";
                case 0x1B: return "ESC";
                default: return "UNKNOWN";
            }
        }

        private string GetHotkeyDescription()
        {
            var parts = new List<string>();
            if ((currentModifiers & MOD_CTRL) != 0) parts.Add("Ctrl");
            if ((currentModifiers & MOD_SHIFT) != 0) parts.Add("Shift");
            if ((currentModifiers & MOD_ALT) != 0) parts.Add("Alt");
            parts.Add(GetKeyName(currentKey));
            return string.Join("+", parts);
        }

        public string GetCurrentHotkeyDescription()
        {
            return GetHotkeyDescription();
        }
    }
}