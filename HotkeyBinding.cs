// Turns text like "Ctrl+Alt+C" into the numbers Windows wants (and back).
using System;
using System.Collections.Generic;

namespace BetterClock
{
    public class HotkeyBinding
    {
        public uint Modifiers;
        public uint Key;

        private static readonly Dictionary<string, uint> Named = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            { "Space", 0x20 }, { "Enter", 0x0D }, { "Tab", 0x09 }, { "Esc", 0x1B }, { "Backspace", 0x08 },
            { "Insert", 0x2D }, { "Delete", 0x2E }, { "Home", 0x24 }, { "End", 0x23 },
            { "PageUp", 0x21 }, { "PageDown", 0x22 },
            { "Left", 0x25 }, { "Up", 0x26 }, { "Right", 0x27 }, { "Down", 0x28 }
        };

        // Returns null if the text isn't a valid key combination.
        public static HotkeyBinding Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string[] parts = text.Split('+');
            uint mods = 0;
            uint key = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length == 0) return null;
                bool last = (i == parts.Length - 1);

                string lower = p.ToLowerInvariant();
                if (!last)
                {
                    if (lower == "ctrl" || lower == "control") mods |= NativeMethods.MOD_CONTROL;
                    else if (lower == "alt") mods |= NativeMethods.MOD_ALT;
                    else if (lower == "shift") mods |= NativeMethods.MOD_SHIFT;
                    else if (lower == "win" || lower == "windows") mods |= NativeMethods.MOD_WIN;
                    else return null;
                }
                else
                {
                    key = ParseKey(p);
                    if (key == 0) return null;
                }
            }

            var b = new HotkeyBinding();
            b.Modifiers = mods;
            b.Key = key;
            return b;
        }

        private static uint ParseKey(string p)
        {
            if (p.Length == 1)
            {
                char c = char.ToUpperInvariant(p[0]);
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) return (uint)c;
                return 0;
            }

            uint named;
            if (Named.TryGetValue(p, out named)) return named;

            if ((p[0] == 'F' || p[0] == 'f') && p.Length <= 3)
            {
                int n;
                if (int.TryParse(p.Substring(1), out n) && n >= 1 && n <= 24) return (uint)(0x70 + n - 1);
            }

            if (p.StartsWith("Num", StringComparison.OrdinalIgnoreCase) && p.Length == 4)
            {
                char d = p[3];
                if (d >= '0' && d <= '9') return (uint)(0x60 + (d - '0'));
            }
            return 0;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if ((Modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((Modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
            if ((Modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
            if ((Modifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");

            string keyName = null;
            foreach (KeyValuePair<string, uint> kv in Named)
            {
                if (kv.Value == Key) { keyName = kv.Key; break; }
            }
            if (keyName == null)
            {
                if ((Key >= 'A' && Key <= 'Z') || (Key >= '0' && Key <= '9')) keyName = ((char)Key).ToString();
                else if (Key >= 0x70 && Key <= 0x87) keyName = "F" + (Key - 0x70 + 1);
                else if (Key >= 0x60 && Key <= 0x69) keyName = "Num" + (Key - 0x60);
                else keyName = "Key" + Key;
            }
            parts.Add(keyName);
            return string.Join("+", parts.ToArray());
        }
    }
}
