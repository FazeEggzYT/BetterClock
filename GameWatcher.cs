// Switches the looks of the overlays automatically while a listed game (program) is running.
// Each game has its own saved looks in %APPDATA%\BetterClock\profiles\<program>.ini.
// While a game is running, any change you make in the settings window edits THAT game's looks.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Threading;

namespace BetterClock
{
    public class GameWatcher
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private string _active;   // the program whose looks are on screen right now (null = normal looks)

        public event Action ProfileChanged;

        public GameWatcher()
        {
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += delegate { Check(); };
            _timer.Start();
        }

        public string Active { get { return _active; } }

        private AppSettings S { get { return AppHost.Settings; } }

        // ------------------------------------------------------------ the list of games

        public static List<string> Games(AppSettings s)
        {
            var list = new List<string>();
            foreach (string raw in (s.GameList ?? "").Split(';'))
            {
                string n = Normalize(raw);
                if (n.Length > 0 && !list.Contains(n)) list.Add(n);
            }
            return list;
        }

        public static string Normalize(string name)
        {
            string n = (name ?? "").Trim().ToLowerInvariant();
            foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c.ToString(), "");
            if (n.Length == 0) return "";
            if (!n.EndsWith(".exe")) n += ".exe";
            return n;
        }

        public bool Add(string name)
        {
            string n = Normalize(name);
            if (n.Length == 0) return false;
            List<string> list = Games(S);
            if (list.Contains(n)) return false;
            list.Add(n);
            S.GameList = string.Join(";", list.ToArray());
            return true;
        }

        public void Remove(string name)
        {
            List<string> list = Games(S);
            list.Remove(name);
            S.GameList = string.Join(";", list.ToArray());
            try { File.Delete(ProfilePath(name)); } catch (Exception) { }
            if (_active == name) SwitchTo(null);
        }

        // ------------------------------------------------------------ profile files

        private static string ProfileFolder
        {
            get { return Path.Combine(AppSettings.FolderPath, "profiles"); }
        }

        public static string ProfilePath(string program)
        {
            return Path.Combine(ProfileFolder, program + ".ini");
        }

        public bool HasProfile(string program)
        {
            return File.Exists(ProfilePath(program));
        }

        // Saves the looks currently on screen into a game's file.
        private void WriteProfile(string program)
        {
            try
            {
                Directory.CreateDirectory(ProfileFolder);
                var holder = new AppSettings();
                holder.Clock.CopyFrom(S.Clock, false);
                holder.Timer.CopyFrom(S.Timer, false);
                holder.Countdown.CopyFrom(S.Countdown, false);
                holder.SaveTo(ProfilePath(program));
            }
            catch (Exception)
            {
            }
        }

        // Called when settings are saved: keeps the running game's file up to date.
        public void SaveActiveProfile()
        {
            if (_active != null) WriteProfile(_active);
        }

        // Copies the normal looks into a game's profile (so it starts like what you have now).
        public void CopyCurrentLooksTo(string program)
        {
            if (_active == program) return;
            AppSettings src = new AppSettings();
            if (_active == null)
            {
                src.Clock.CopyFrom(S.Clock, false);
                src.Timer.CopyFrom(S.Timer, false);
                src.Countdown.CopyFrom(S.Countdown, false);
            }
            else
            {
                return;   // another game's looks are on screen; do it when no game is running
            }
            try
            {
                Directory.CreateDirectory(ProfileFolder);
                src.SaveTo(ProfilePath(program));
            }
            catch (Exception)
            {
            }
        }

        // ------------------------------------------------------------ watching

        private static bool IsRunning(string program)
        {
            string name = program.EndsWith(".exe") ? program.Substring(0, program.Length - 4) : program;
            try
            {
                Process[] found = Process.GetProcessesByName(name);
                bool any = found.Length > 0;
                foreach (Process p in found) p.Dispose();
                return any;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Check()
        {
            string target = null;
            if (S.AutoSwitchGames)
            {
                foreach (string g in Games(S))
                {
                    if (IsRunning(g)) { target = g; break; }
                }
            }
            if (target != _active) SwitchTo(target);
        }

        private void SwitchTo(string target)
        {
            // leave the current game: keep its looks, then bring the normal looks back
            if (_active != null) WriteProfile(_active);

            if (target == null)
            {
                S.EndProfile();
                _active = null;
            }
            else
            {
                S.BeginProfile();   // parks the normal looks so they are what gets saved
                S.ResetToBase();    // every game starts from the normal looks
                _active = target;
                AppSettings loaded = null;
                try { if (File.Exists(ProfilePath(target))) loaded = AppSettings.LoadFrom(ProfilePath(target)); }
                catch (Exception) { }

                if (loaded != null)
                {
                    S.Clock.CopyFrom(loaded.Clock, false);
                    S.Timer.CopyFrom(loaded.Timer, false);
                    S.Countdown.CopyFrom(loaded.Countdown, false);
                }
                // no file yet: the game simply starts with the normal looks and keeps its own copy from now on
                WriteProfile(target);
            }

            AppHost.ApplySettings();
            if (ProfileChanged != null) ProfileChanged();
        }

        // Forget everything (used after import / reset).
        public void Forget()
        {
            S.DropProfile();
            _active = null;
        }

        public void Stop()
        {
            _timer.Stop();
            SaveActiveProfile();
        }
    }
}
