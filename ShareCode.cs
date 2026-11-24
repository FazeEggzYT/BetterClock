// Share codes: a short text that holds the look of the three overlays (v1.4.8).
// A code starts with "BC1:" and holds only the settings that differ from the normal look.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace BetterClock
{
    public static class ShareCode
    {
        private const string Prefix = "BC1:";

        // Position and texts stay as the person has them.
        private static readonly string[] Keep = { "Position", "EdgeMargin", "MonitorMode", "MonitorIndex", "UseCustomPosition", "CustomX", "CustomY", "LabelText", "TextAbove", "TextBelow" };

        private static readonly OverlayMode[] Modes = { OverlayMode.Clock, OverlayMode.Timer, OverlayMode.Countdown };

        // The look of all three overlays as plain text (only what differs from the normal look).
        public static string Snapshot(AppSettings s)
        {
            var sb = new StringBuilder();
            var fresh = new AppSettings();
            foreach (OverlayMode m in Modes)
            {
                ModeStyle now = s.StyleFor(m);
                ModeStyle def = fresh.StyleFor(m);
                sb.Append("[" + m + "]\n");
                foreach (FieldInfo f in typeof(ModeStyle).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (Array.IndexOf(Keep, f.Name) >= 0) continue;
                    string a = AppSettings.FieldText(f.GetValue(now));
                    string b = AppSettings.FieldText(f.GetValue(def));
                    if (a != b) sb.Append(f.Name + "=" + a + "\n");
                }
            }
            return sb.ToString();
        }

        // Puts a snapshot on the three overlays. Returns false when the text is not a look.
        public static bool Restore(AppSettings s, string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var fresh = new AppSettings();
            var looks = new Dictionary<OverlayMode, ModeStyle>();
            ModeStyle current = null;
            bool any = false;
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    OverlayMode m;
                    try { m = (OverlayMode)Enum.Parse(typeof(OverlayMode), line.Substring(1, line.Length - 2)); }
                    catch (Exception) { current = null; continue; }
                    current = fresh.StyleFor(m);
                    looks[m] = current;
                    any = true;
                    continue;
                }
                if (current == null) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string name = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1);
                if (Array.IndexOf(Keep, name) >= 0) continue;
                FieldInfo f = typeof(ModeStyle).GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (f == null) continue;
                object parsed = AppSettings.FieldParse(f.FieldType, val);
                if (parsed != null) f.SetValue(current, parsed);
            }
            if (!any) return false;
            foreach (KeyValuePair<OverlayMode, ModeStyle> kv in looks) s.StyleFor(kv.Key).CopyFrom(kv.Value, true);
            return true;
        }

        // The code to share: "BC1:" and the compressed snapshot.
        public static string Make(AppSettings s)
        {
            byte[] raw = Encoding.UTF8.GetBytes(Snapshot(s));
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    gz.Write(raw, 0, raw.Length);
                }
                return Prefix + Convert.ToBase64String(ms.ToArray());
            }
        }

        // Returns false when the code is not a Better Clock code.
        public static bool Apply(AppSettings s, string code)
        {
            if (code == null) return false;
            string c = code.Trim();
            if (!c.StartsWith(Prefix)) return false;
            try
            {
                byte[] packed = Convert.FromBase64String(c.Substring(Prefix.Length).Trim());
                using (var input = new MemoryStream(packed))
                using (var gz = new GZipStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[4096];
                    int n;
                    while ((n = gz.Read(buffer, 0, buffer.Length)) > 0) output.Write(buffer, 0, n);
                    return Restore(s, Encoding.UTF8.GetString(output.ToArray()));
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
