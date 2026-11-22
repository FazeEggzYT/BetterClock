// Remembers the results of the timer (saved when you press Reset).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BetterClock
{
    public class HistoryEntry
    {
        public DateTime When;
        public int Seconds;
    }

    public static class TimerHistory
    {
        public static event Action Changed;

        private static List<HistoryEntry> _list;
        private const int Max = 100;

        private static string FilePath
        {
            get { return Path.Combine(AppSettings.FolderPath, "history.txt"); }
        }

        private static void Load()
        {
            if (_list != null) return;
            _list = new List<HistoryEntry>();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int bar = line.IndexOf('|');
                    if (bar <= 0) continue;
                    DateTime when;
                    int secs;
                    if (!DateTime.TryParseExact(line.Substring(0, bar), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out when)) continue;
                    if (!int.TryParse(line.Substring(bar + 1), out secs)) continue;
                    var e = new HistoryEntry();
                    e.When = when;
                    e.Seconds = secs;
                    _list.Add(e);
                }
            }
            catch (Exception)
            {
                // a damaged history file must never stop the app
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(AppSettings.FolderPath);
                var lines = new List<string>();
                foreach (HistoryEntry e in _list)
                    lines.Add(e.When.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "|" + e.Seconds);
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception)
            {
            }
        }

        public static void Add(double seconds)
        {
            Load();
            var e = new HistoryEntry();
            e.When = DateTime.Now;
            e.Seconds = (int)Math.Floor(seconds);
            _list.Add(e);
            while (_list.Count > Max) _list.RemoveAt(0);
            Save();
            if (Changed != null) Changed();
        }

        public static void Clear()
        {
            Load();
            _list.Clear();
            Save();
            if (Changed != null) Changed();
        }

        // Newest first, one result per line, plus the best (shortest) time.
        public static string Text(int max)
        {
            Load();
            if (_list.Count == 0) return "";
            var sb = new StringBuilder();
            int best = int.MaxValue;
            foreach (HistoryEntry e in _list) if (e.Seconds < best) best = e.Seconds;
            sb.Append("Saved results: " + _list.Count + "   Shortest: " + OverlayController.FormatTime(best));
            int shown = 0;
            for (int i = _list.Count - 1; i >= 0 && shown < max; i--, shown++)
            {
                sb.Append("\n" + _list[i].When.ToString("d MMM  HH:mm", CultureInfo.InvariantCulture) + "     " + OverlayController.FormatTime(_list[i].Seconds));
            }
            return sb.ToString();
        }
    }
}
