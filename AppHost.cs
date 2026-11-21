// Holds the things that live for the whole run of the app.
using System;
using System.Collections.Generic;

namespace BetterClock
{
    public class HotkeyDef
    {
        public string Label;
        public string Field;     // name of the setting that stores the key text
        public bool AllowEmpty;  // optional keys may be left empty
        public string DefaultKey;
        public Action Action;
    }

    public static class AppHost
    {
        public static AppSettings Settings = new AppSettings();
        public static readonly Dictionary<OverlayMode, OverlayGroup> Overlays = new Dictionary<OverlayMode, OverlayGroup>();
        public static OverlayController Controller;
        public static HotkeyHelper Hotkeys;
        public static TrayIcon Tray;
        public static GameWatcher Games;

        public static readonly List<HotkeyDef> HotkeyDefs = new List<HotkeyDef>();

        // Filled in when a key can't be registered, so the UI can tell the user.
        public static readonly List<string> HotkeyProblems = new List<string>();

        public static void InitSettings()
        {
            Settings = AppSettings.Load();
            // The registry is the truth for "start with Windows".
            Settings.StartWithWindows = AutoStart.IsEnabled();
        }

        public static void InitOverlay()
        {
            Overlays[OverlayMode.Clock] = new OverlayGroup(OverlayMode.Clock);
            Overlays[OverlayMode.Timer] = new OverlayGroup(OverlayMode.Timer);
            Overlays[OverlayMode.Countdown] = new OverlayGroup(OverlayMode.Countdown);
            Controller = new OverlayController();
        }

        public static void InitHotkeys()
        {
            if (Hotkeys == null) Hotkeys = new HotkeyHelper();

            HotkeyDefs.Clear();
            AddKey("Show / hide the clock", "KeyClockToggle", "Ctrl+Alt+C", false, delegate { Controller.ToggleClock(); });
            AddKey("Show / hide the timer", "KeyTimerToggle", "Ctrl+Alt+T", false, delegate { Controller.ToggleTimer(); });
            AddKey("Show / hide the countdown", "KeyCountdownToggle", "Ctrl+Alt+D", false, delegate { Controller.ToggleCountdown(); });
            AddKey("Start / stop (timer or countdown)", "KeyStartStop", "Ctrl+Alt+S", false, delegate { Controller.StartStop(); });
            AddKey("Reset (timer or countdown)", "KeyReset", "Ctrl+Alt+R", false, delegate { Controller.Reset(); });
            AddKey("Hide all overlays", "KeyHideAll", "Ctrl+Alt+H", true, delegate { Controller.HideAll(); });
            AddKey("Open settings", "KeyOpenSettings", "Ctrl+Alt+B", false, delegate { SettingsWindow.ShowSingleton(); });
            AddKey("Move overlays (unlock / lock dragging)", "KeyMoveMode", "Ctrl+Alt+M", true, delegate { ToggleMoveMode(); });
            AddKey("Timer only: start / stop", "KeyTimerStartStop", "", true, delegate { Controller.StartStop(OverlayMode.Timer); });
            AddKey("Timer only: reset", "KeyTimerReset", "", true, delegate { Controller.Reset(OverlayMode.Timer); });
            AddKey("Countdown only: start / stop", "KeyCountdownStartStop", "", true, delegate { Controller.StartStop(OverlayMode.Countdown); });
            AddKey("Countdown only: reset", "KeyCountdownReset", "", true, delegate { Controller.Reset(OverlayMode.Countdown); });

            ReloadHotkeys();
        }

        private static void AddKey(string label, string field, string def, bool allowEmpty, Action action)
        {
            HotkeyDefs.Add(new HotkeyDef { Label = label, Field = field, DefaultKey = def, AllowEmpty = allowEmpty, Action = action });
        }

        // (Re)registers every key from the current settings.
        public static void ReloadHotkeys()
        {
            if (Hotkeys == null) return;
            Hotkeys.UnregisterAll();
            HotkeyProblems.Clear();

            foreach (HotkeyDef d in HotkeyDefs)
            {
                string keyText = (string)Reflect.Get(Settings, d.Field);
                if (d.AllowEmpty && string.IsNullOrEmpty(keyText)) continue;
                HotkeyBinding b = HotkeyBinding.Parse(keyText);
                if (b == null)
                {
                    HotkeyProblems.Add(d.Label + ": \"" + keyText + "\" is not a valid key.");
                    continue;
                }
                if (Hotkeys.Register(b.Modifiers, b.Key, d.Action) == 0)
                {
                    HotkeyProblems.Add(d.Label + ": " + b + " is already used by another program.");
                }
            }
        }

        // Frees all keys (used while you are choosing a new key in the settings window).
        public static void SuspendHotkeys()
        {
            if (Hotkeys != null) Hotkeys.UnregisterAll();
        }

        public static OverlayGroup GetOverlay(OverlayMode mode)
        {
            OverlayGroup g;
            return Overlays.TryGetValue(mode, out g) ? g : null;
        }

        public static void ShowOverlay(OverlayMode mode)
        {
            OverlayGroup g = GetOverlay(mode);
            if (g != null) g.ShowNow();
        }

        public static void HideOverlay(OverlayMode mode)
        {
            OverlayGroup g = GetOverlay(mode);
            if (g != null) g.HideNow();
        }

        // Call after any setting changed so the real overlays update straight away.
        public static void ApplySettings()
        {
            foreach (KeyValuePair<OverlayMode, OverlayGroup> kv in Overlays) kv.Value.ApplySettings();
        }

        // Saves the settings (and the looks of a running game's profile).
        public static void SaveSettings()
        {
            Settings.Save();
            if (Games != null) Games.SaveActiveProfile();
        }

        // ------------------------------------------------------------ move mode

        public static bool MoveMode;
        private static readonly List<OverlayMode> _shownByMove = new List<OverlayMode>();

        // Lets you drag every overlay with the mouse. Turning it off locks them (click-through) again.
        public static void SetMoveMode(bool on)
        {
            if (on == MoveMode) return;
            MoveMode = on;
            if (on)
            {
                _shownByMove.Clear();
                foreach (KeyValuePair<OverlayMode, OverlayGroup> kv in Overlays)
                {
                    if (!kv.Value.IsShown)
                    {
                        _shownByMove.Add(kv.Key);
                        Controller.ShowMode(kv.Key);
                    }
                    kv.Value.SetMoveMode(true);
                }
            }
            else
            {
                foreach (KeyValuePair<OverlayMode, OverlayGroup> kv in Overlays) kv.Value.SetMoveMode(false);
                foreach (OverlayMode m in _shownByMove) Controller.HideMode(m);
                _shownByMove.Clear();
                SaveSettings();
            }
            if (MoveModeChanged != null) MoveModeChanged();
        }

        public static void ToggleMoveMode() { SetMoveMode(!MoveMode); }

        public static event Action MoveModeChanged;
        public static event Action<OverlayMode> OverlayMoved;

        public static void RaiseMoved(OverlayMode mode)
        {
            ApplySettings();
            Settings.Save();
            if (OverlayMoved != null) OverlayMoved(mode);
        }

        public static void Shutdown()
        {
            SoundService.Stop();
            if (Tray != null) { Tray.Dispose(); Tray = null; }
            if (Games != null) { Games.Stop(); Games = null; }
            foreach (KeyValuePair<OverlayMode, OverlayGroup> kv in Overlays) kv.Value.Close();
            Overlays.Clear();
            if (Hotkeys != null) { Hotkeys.Dispose(); Hotkeys = null; }
            SaveSettings();
        }
    }
}
