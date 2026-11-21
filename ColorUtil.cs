using System;
using System.Globalization;
using System.Windows.Media;

namespace BetterClock
{
    public static class ColorUtil
    {
        // Accepts #RRGGBB or #AARRGGBB (the # is optional). Returns fallback if it can't be read.
        public static Color Parse(string text, Color fallback)
        {
            Color c;
            return TryParse(text, out c) ? c : fallback;
        }

        public static bool TryParse(string text, out Color color)
        {
            color = Colors.White;
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim();
            if (t.StartsWith("#")) t = t.Substring(1);
            if (t.Length != 6 && t.Length != 8) return false;

            uint v;
            if (!uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return false;

            byte a = 255;
            if (t.Length == 8) a = (byte)((v >> 24) & 0xFF);
            byte r = (byte)((v >> 16) & 0xFF);
            byte g = (byte)((v >> 8) & 0xFF);
            byte b = (byte)(v & 0xFF);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        public static string ToHex(Color c)
        {
            if (c.A == 255) return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
        }

        public static Color WithAlpha(Color c, double percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            return Color.FromArgb((byte)Math.Round(255.0 * percent / 100.0), c.R, c.G, c.B);
        }

        public static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
