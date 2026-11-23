// Loads a font from a .ttf / .otf file the person picked (v1.4.5).
// The file is copied into the Better Clock folder so it keeps working if the original moves.
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace BetterClock
{
    public static class FontFiles
    {
        private static readonly Dictionary<string, FontFamily> Cache = new Dictionary<string, FontFamily>();

        // Returns the font, or null when there is no file or it cannot be read.
        public static FontFamily Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                if (!File.Exists(path)) return null;
                string key = path + "|" + File.GetLastWriteTimeUtc(path).Ticks;
                FontFamily cached;
                if (Cache.TryGetValue(key, out cached)) return cached;

                string dir = Path.Combine(AppSettings.FolderPath, "fonts");
                Directory.CreateDirectory(dir);
                string target = Path.Combine(dir, Path.GetFileName(path));
                bool same = File.Exists(target) && new FileInfo(target).Length == new FileInfo(path).Length;
                if (!same && !string.Equals(Path.GetFullPath(path), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                    File.Copy(path, target, true);

                var glyphs = new GlyphTypeface(new Uri(target));
                var names = new List<string>();
                foreach (string n in glyphs.Win32FamilyNames.Values) if (!names.Contains(n)) names.Add(n);
                foreach (string n in glyphs.FamilyNames.Values) if (!names.Contains(n)) names.Add(n);

                var baseUri = new Uri(dir + Path.DirectorySeparatorChar);
                FontFamily found = null;
                foreach (string n in names)
                {
                    FontFamily f = new FontFamily(baseUri, "./#" + n);
                    if (HasFaces(f)) { found = f; break; }
                }
                Cache[key] = found;
                return found;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool HasFaces(FontFamily f)
        {
            try
            {
                foreach (Typeface t in f.GetTypefaces()) return true;
            }
            catch (Exception)
            {
            }
            return false;
        }
    }
}
