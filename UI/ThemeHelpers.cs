using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RecordWhisperClient.UI
{
    public static class ThemeHelper
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static bool IsLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    var registryValueObject = key?.GetValue(RegistryValueName);
                    if (registryValueObject == null)
                        return true; // Default to light theme
                    return (int)registryValueObject > 0;
                }
            }
            catch
            {
                return true; // Default to light theme on error
            }
        }

        public static Color GetBackgroundColor() => IsLightTheme() ? Color.White : Color.FromArgb(32, 32, 32);
        public static Color GetForegroundColor() => IsLightTheme() ? Color.Black : Color.White;
        public static Color GetControlBackgroundColor() => IsLightTheme() ? Color.White : Color.FromArgb(45, 45, 48);
        public static Color GetBorderColor() => IsLightTheme() ? Color.FromArgb(200, 200, 200) : Color.FromArgb(70, 70, 74);
        public static Color GetButtonBackgroundColor() => IsLightTheme() ? SystemColors.ButtonFace : Color.FromArgb(62, 62, 66);
        public static Color GetButtonForegroundColor() => IsLightTheme() ? SystemColors.ButtonHighlight : Color.White;
        public static Color GetMenuSelectionColor() => IsLightTheme() ? Color.FromArgb(200, 230, 255) : Color.FromArgb(62, 62, 66);
        public static Color GetMenuBorderColor() => IsLightTheme() ? Color.FromArgb(200, 200, 200) : Color.FromArgb(100, 100, 104);
        
        public static Color GetAccentColor()
        {
            try
            {
                // Try Windows 10/11 accent color from registry
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    var accentColorObj = key?.GetValue("AccentColor");
                    if (accentColorObj != null)
                    {
                        uint accentColorDword = Convert.ToUInt32(accentColorObj);
                        // Windows stores color as ABGR, we need to convert to ARGB
                        byte a = (byte)((accentColorDword >> 24) & 0xFF);
                        byte b = (byte)((accentColorDword >> 16) & 0xFF);
                        byte g = (byte)((accentColorDword >> 8) & 0xFF);
                        byte r = (byte)(accentColorDword & 0xFF);
                        
                        // Create color with full opacity if alpha is 0
                        if (a == 0) a = 255;
                        
                        return Color.FromArgb(a, r, g, b);
                    }
                }

                // Fallback: try alternative registry location
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent"))
                {
                    var accentColorObj = key?.GetValue("AccentColorMenu");
                    if (accentColorObj != null)
                    {
                        uint accentColorDword = Convert.ToUInt32(accentColorObj);
                        byte a = 255; // Force full opacity
                        byte b = (byte)((accentColorDword >> 16) & 0xFF);
                        byte g = (byte)((accentColorDword >> 8) & 0xFF);
                        byte r = (byte)(accentColorDword & 0xFF);
                        return Color.FromArgb(a, r, g, b);
                    }
                }
            }
            catch
            {
                // Continue to fallback colors
            }

            return Color.Red;
        }
    }

    public class ThemedToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ThemedToolStripRenderer() : base(new ThemedColorTable()) { }
    }

    public class ThemedColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => ThemeHelper.GetMenuSelectionColor();
        public override Color MenuItemSelectedGradientBegin => ThemeHelper.GetMenuSelectionColor();
        public override Color MenuItemSelectedGradientEnd => ThemeHelper.GetMenuSelectionColor();
        public override Color MenuItemBorder => ThemeHelper.GetMenuBorderColor();
        public override Color MenuBorder => ThemeHelper.GetMenuBorderColor();
        public override Color ToolStripDropDownBackground => ThemeHelper.GetControlBackgroundColor();
        public override Color ImageMarginGradientBegin => ThemeHelper.GetControlBackgroundColor();
        public override Color ImageMarginGradientMiddle => ThemeHelper.GetControlBackgroundColor();
        public override Color ImageMarginGradientEnd => ThemeHelper.GetControlBackgroundColor();
        public override Color MenuStripGradientBegin => ThemeHelper.GetControlBackgroundColor();
        public override Color MenuStripGradientEnd => ThemeHelper.GetControlBackgroundColor();
        public override Color SeparatorDark => ThemeHelper.GetBorderColor();
        public override Color SeparatorLight => ThemeHelper.GetBorderColor();
    }
}