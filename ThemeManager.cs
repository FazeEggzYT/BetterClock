// Dark / light colours for the settings window. The brushes live in the window's resources
// and are animated, so switching theme fades smoothly.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace BetterClock
{
    public static class ThemeManager
    {
        public static bool IsDark(AppSettings s)
        {
            if (s.ColorTheme == ColorThemeMode.Dark) return true;
            if (s.ColorTheme == ColorThemeMode.Light) return false;
            return !SystemUsesLightTheme();
        }

        private static bool SystemUsesLightTheme()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", false))
                {
                    if (k == null) return false;
                    object v = k.GetValue("AppsUseLightTheme");
                    return v is int && (int)v == 1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Dictionary<string, Color> Palette(AppSettings s)
        {
            bool dark = IsDark(s);
            Color accent = ColorUtil.Parse(s.AccentColor, Color.FromRgb(0x4D, 0x7C, 0xFF));
            var p = new Dictionary<string, Color>();
            if (dark)
            {
                p["BgBrush"] = Color.FromRgb(0x0C, 0x0D, 0x17);
                p["PanelBrush"] = Color.FromRgb(0x15, 0x17, 0x27);
                p["Panel2Brush"] = Color.FromRgb(0x1D, 0x20, 0x36);
                p["InkBrush"] = Color.FromRgb(0xE9, 0xEB, 0xF7);
                p["MutedBrush"] = Color.FromRgb(0x8A, 0x90, 0xAE);
                p["LineBrush"] = Color.FromRgb(0x2D, 0x31, 0x52);
                p["ControlBrush"] = Color.FromRgb(0x23, 0x27, 0x45);
            }
            else
            {
                p["BgBrush"] = Color.FromRgb(0xE9, 0xEC, 0xF6);
                p["PanelBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF);
                p["Panel2Brush"] = Color.FromRgb(0xF3, 0xF5, 0xFB);
                p["InkBrush"] = Color.FromRgb(0x1F, 0x23, 0x40);
                p["MutedBrush"] = Color.FromRgb(0x68, 0x6D, 0x8A);
                p["LineBrush"] = Color.FromRgb(0xD6, 0xDA, 0xEC);
                p["ControlBrush"] = Color.FromRgb(0xEE, 0xF0, 0xF9);
            }
            p["AccentBrush"] = accent;
            p["AccentSoftBrush"] = Color.FromArgb(0x40, accent.R, accent.G, accent.B);
            return p;
        }

        public static void Apply(ResourceDictionary res, AppSettings s, bool animate)
        {
            foreach (KeyValuePair<string, Color> kv in Palette(s))
            {
                var brush = res[kv.Key] as SolidColorBrush;
                if (brush == null || brush.IsFrozen)
                {
                    res[kv.Key] = new SolidColorBrush(kv.Value);
                }
                else if (animate && UiAnim.Enabled)
                {
                    var a = new ColorAnimation(kv.Value, UiAnim.Dur(350));
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, a);
                }
                else
                {
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
                    brush.Color = kv.Value;
                }
            }
        }

        // Makes the Windows title bar dark too (Windows 10 build 18985+ and Windows 11).
        public static void ApplyTitleBar(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int on = dark ? 1 : 0;
                NativeMethods.DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int));
                NativeMethods.DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int));
            }
            catch (Exception)
            {
            }
        }
    }
}
