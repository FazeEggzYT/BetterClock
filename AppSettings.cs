// All user settings, saved to %APPDATA%\BetterClock\settings.ini
// Every public instance field is saved and loaded automatically.
// Each mode (Clock / Timer / Countdown) has its own ModeStyle: position, monitor and look.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace BetterClock
{
    public enum BackgroundStyle { Solid, Gradient, None }
    public enum ColorThemeMode { Dark, Light, System }
    public enum ShowAnimation { Fade, Slide, Pop, None }
    public enum DigitStyle { Normal, Flip, Led, Terminal, Analog }
    public enum OverlayIcon { None, Clock, Hourglass, Stopwatch, Bell, Star, Bolt, Flag, Trophy, Skull, Heart, Gamepad, Custom }
    public enum ImageFit { Cover, Fit, Stretch }
    public enum ProgressStyle { None, Bar, Ring }
    public enum CountdownPlanKind { Single, Chain, Pomodoro }

    // Where one overlay sits and how it looks.
    public class ModeStyle
    {
        // ---- where ----
        public OverlayPosition Position = OverlayPosition.TopCenter;
        public int EdgeMargin = 24;
        public MonitorMode MonitorMode = MonitorMode.GameWindow;
        public int MonitorIndex = 0;
        public bool UseCustomPosition = false;   // true = use the exact X / Y below instead of the 3x3 grid
        public int CustomX = 100;                // pixels from the left of the monitor
        public int CustomY = 100;                // pixels from the top of the monitor

        // ---- label ----
        public bool ShowLabel = false;
        public string LabelText = "CLOCK";
        public int LabelSize = 12;
        public string LabelColor = "#9AA0B5";

        // ---- text ----
        public string FontFamily = "Segoe UI Semibold";
        public int FontSize = 54;
        public bool FontBold = false;
        public bool FontItalic = false;
        public bool TextShadow = true;

        public string TextColor = "#FFFFFF";
        public string RunningColor = "#7CFFB2";
        public string PausedColor = "#FFB35C";
        public string FinishedColor = "#FF5C5C";

        // ---- background ----
        public BackgroundStyle BackgroundStyle = BackgroundStyle.Solid;
        public string BackgroundColor = "#101018";
        public string BackgroundColor2 = "#2A1650";
        public int GradientAngle = 45;
        public int BackgroundOpacity = 80;

        // ---- shape and border ----
        public int CornerRadius = 14;
        public int PaddingH = 26;
        public int PaddingV = 10;
        public string BorderColor = "#FFFFFF";
        public int BorderThickness = 0;

        // ---- how the numbers are drawn ----
        public DigitStyle DigitStyle = DigitStyle.Normal;
        public bool BlinkColon = false;

        // ---- extra text ----
        public string TextAbove = "";
        public string TextBelow = "";
        public int ExtraTextSize = 16;
        public string ExtraTextColor = "#C8CCDD";
        public bool ShowDayName = false;     // clock only
        public bool ShowDate = false;        // clock only

        // ---- small icon next to the numbers ----
        public OverlayIcon Icon = OverlayIcon.None;
        public string IconPath = "";
        public int IconSize = 36;
        public bool IconOnRight = false;

        // ---- picture behind the numbers ----
        public string BackgroundImagePath = "";
        public int BackgroundImageOpacity = 60;
        public ImageFit BackgroundImageFit = ImageFit.Cover;

        // ---- progress bar / ring, low-time colours, glow pulse ----
        public ProgressStyle ProgressStyle = ProgressStyle.None;
        public int ProgressThickness = 6;
        public string WarnColor = "#FFB020";     // countdown running low
        public string DangerColor = "#FF4040";   // countdown almost out
        public string TargetColor = "#4DFF88";   // timer reached its target
        public int PulseSeconds = 10;            // glow pulses in the last N seconds (0 = off)
        public string PulseColor = "#FF4040";

        // ---- shadow ----
        public bool ShadowEnabled = true;
        public int ShadowBlur = 14;

        // ---- size and animation ----
        public int Scale = 100;
        public int OverallOpacity = 100;
        public int FadeMs = 140;
        public ShowAnimation ShowAnimation = ShowAnimation.Fade;

        public static ModeStyle Make(OverlayPosition pos, string label)
        {
            var s = new ModeStyle();
            s.Position = pos;
            s.LabelText = label;
            return s;
        }

        private static readonly string[] KeepOnLookCopy = { "Position", "EdgeMargin", "MonitorMode", "MonitorIndex", "UseCustomPosition", "CustomX", "CustomY", "LabelText", "TextAbove", "TextBelow" };

        // Copies everything. With lookOnly = true the position, monitor and label text stay as they were.
        public void CopyFrom(ModeStyle other, bool lookOnly)
        {
            foreach (FieldInfo f in typeof(ModeStyle).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (lookOnly && Array.IndexOf(KeepOnLookCopy, f.Name) >= 0) continue;
                f.SetValue(this, f.GetValue(other));
            }
        }
    }

    public class AppSettings
    {
        // ================= The three overlays =================
        public ModeStyle Clock = ModeStyle.Make(OverlayPosition.TopCenter, "CLOCK");
        public ModeStyle Timer = ModeStyle.Make(OverlayPosition.TopLeft, "TIMER");
        public ModeStyle Countdown = ModeStyle.Make(OverlayPosition.TopRight, "COUNTDOWN");

        // ================= Clock behaviour =================
        public bool Clock24Hour = true;
        public bool ClockShowSeconds = true;
        public double ClockShowForSeconds = 6;
        public bool ClockHoldOpen = false;

        // ================= Timer behaviour =================
        public bool TimerKeepRunningHidden = true;
        public bool TimerTargetEnabled = false;   // turns green (and beeps) when it reaches the target
        public int TimerTargetSeconds = 300;
        public bool TimerTargetBeep = true;

        // ================= Countdown behaviour =================
        public int CountdownTotalSeconds = 60;
        public bool CountdownKeepRunningHidden = true;
        public bool CountdownAutoShowAtZero = true;
        public bool CountdownFlash = true;
        public bool CountdownSoundEnabled = true;
        public string CountdownSoundPath = "";
        public int CountdownSoundVolume = 80;
        public int CountdownSoundRepeat = 2;
        public bool CountdownOvertime = false;        // keep counting up (+00:05) after zero
        public bool CountdownLowColors = true;        // amber, then red, as time runs low
        public int CountdownAmberAt = 30;             // seconds left
        public int CountdownRedAt = 10;
        public bool CountdownWarnBeeps = false;       // short beeps at the seconds listed below
        public string CountdownWarnList = "10,5,3,2,1";
        public CountdownPlanKind CountdownPlan = CountdownPlanKind.Single;
        public string CountdownChain = "25:00 Work;5:00 Break";   // time and name, separated by ;
        public bool CountdownChainLoop = false;
        public int PomoWorkMin = 25;
        public int PomoShortMin = 5;
        public int PomoLongMin = 15;
        public int PomoRounds = 4;

        // ================= Hotkeys (empty = not set) =================
        public string KeyClockToggle = "Ctrl+Alt+C";
        public string KeyTimerToggle = "Ctrl+Alt+T";
        public string KeyCountdownToggle = "Ctrl+Alt+D";
        public string KeyStartStop = "Ctrl+Alt+S";
        public string KeyReset = "Ctrl+Alt+R";
        public string KeyHideAll = "Ctrl+Alt+H";
        public string KeyOpenSettings = "Ctrl+Alt+B";
        public string KeyTimerStartStop = "";
        public string KeyTimerReset = "";
        public string KeyCountdownStartStop = "";
        public string KeyCountdownReset = "";
        public string KeyMoveMode = "Ctrl+Alt+M";   // unlock / lock dragging the overlays

        // ================= The app itself =================
        public ColorThemeMode ColorTheme = ColorThemeMode.Dark;
        public string AccentColor = "#4D7CFF";
        public bool AnimationsEnabled = true;
        public int AnimationSpeed = 100;     // percent: 50 = slow, 200 = fast
        public int UiScale = 100;            // percent
        public bool StartWithWindows = false;
        public bool ShowSplash = true;
        public bool ShowStartupMessage = true;
        public bool OpenSettingsOnLaunch = true;
        public bool CheckForUpdates = true;
        public bool AutoInstallUpdates = false;   // install new versions by itself when Better Clock starts
        public string LastSeenVersion = "";

        // ================= Per-game looks =================
        public bool AutoSwitchGames = true;
        public string GameList = "";          // program names separated by ';' (for example valorant.exe;csgo.exe)

        // While a game profile is active the 'normal' looks are parked here so they are what gets saved.
        private ModeStyle[] _baseLooks;

        public bool ProfileActive { get { return _baseLooks != null; } }

        public void BeginProfile()
        {
            if (_baseLooks != null) return;
            _baseLooks = new ModeStyle[] { Clone(Clock), Clone(Timer), Clone(Countdown) };
        }

        // Puts the parked normal looks back and forgets them.
        public void EndProfile()
        {
            if (_baseLooks == null) return;
            Clock.CopyFrom(_baseLooks[0], false);
            Timer.CopyFrom(_baseLooks[1], false);
            Countdown.CopyFrom(_baseLooks[2], false);
            _baseLooks = null;
        }

        // Puts the parked normal looks back on the live looks but keeps the profile active.
        public void ResetToBase()
        {
            if (_baseLooks == null) return;
            Clock.CopyFrom(_baseLooks[0], false);
            Timer.CopyFrom(_baseLooks[1], false);
            Countdown.CopyFrom(_baseLooks[2], false);
        }

        // Forget the parked looks without restoring them (used after import / reset).
        public void DropProfile() { _baseLooks = null; }

        public static ModeStyle Clone(ModeStyle s)
        {
            var c = new ModeStyle();
            c.CopyFrom(s, false);
            return c;
        }

        public ModeStyle StyleFor(OverlayMode mode)
        {
            if (mode == OverlayMode.Timer) return Timer;
            if (mode == OverlayMode.Countdown) return Countdown;
            return Clock;
        }

        // -------------------------------------------------------------

        public static string FolderPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetterClock");
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(FolderPath, "settings.ini"); }
        }

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        public static FieldInfo Field(string name)
        {
            return typeof(AppSettings).GetField(name, Flags);
        }

        public void CopyFrom(AppSettings other)
        {
            foreach (FieldInfo f in typeof(AppSettings).GetFields(Flags))
            {
                if (f.FieldType == typeof(ModeStyle))
                    ((ModeStyle)f.GetValue(this)).CopyFrom((ModeStyle)f.GetValue(other), false);
                else
                    f.SetValue(this, f.GetValue(other));
            }
        }

        public static AppSettings Load()
        {
            var s = new AppSettings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                foreach (string raw in File.ReadAllLines(FilePath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    try
                    {
                        Assign(s, line.Substring(0, eq).Trim(), line.Substring(eq + 1).Trim());
                    }
                    catch (Exception)
                    {
                        // ignore a bad line and keep the default
                    }
                }
            }
            catch (Exception)
            {
                // A broken settings file must never stop the app from starting.
            }
            return s;
        }

        public static AppSettings LoadFrom(string path)
        {
            var s = new AppSettings();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                try { Assign(s, line.Substring(0, eq).Trim(), line.Substring(eq + 1).Trim()); }
                catch (Exception) { }
            }
            return s;
        }

        private static void Assign(AppSettings s, string key, string val)
        {
            int dot = key.IndexOf('.');
            if (dot > 0)
            {
                FieldInfo pf = Field(key.Substring(0, dot));
                if (pf != null && pf.FieldType == typeof(ModeStyle))
                    SetParsed(pf.GetValue(s), key.Substring(dot + 1), val);
                return;
            }

            FieldInfo f = Field(key);
            if (f != null && f.FieldType != typeof(ModeStyle))
            {
                SetParsed(s, key, val);
                return;
            }

            // An older settings file kept the look in one place. Use it for all three overlays.
            if (typeof(ModeStyle).GetField(key, Flags) != null)
            {
                SetParsed(s.Clock, key, val);
                SetParsed(s.Timer, key, val);
                SetParsed(s.Countdown, key, val);
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                File.WriteAllLines(FilePath, ToLines().ToArray());
            }
            catch (Exception)
            {
            }
        }

        public void SaveTo(string path)
        {
            File.WriteAllLines(path, ToLines().ToArray());
        }

        private List<string> ToLines()
        {
            var lines = new List<string>();
            lines.Add("# Better Clock settings (you can edit this file, or use the settings window)");
            foreach (FieldInfo f in typeof(AppSettings).GetFields(Flags))
            {
                if (f.FieldType == typeof(ModeStyle)) continue;
                lines.Add(f.Name + "=" + Text(f.GetValue(this)));
            }
            foreach (FieldInfo f in typeof(AppSettings).GetFields(Flags))
            {
                if (f.FieldType != typeof(ModeStyle)) continue;
                object style = f.GetValue(this);
                if (_baseLooks != null)
                {
                    if (f.Name == "Clock") style = _baseLooks[0];
                    else if (f.Name == "Timer") style = _baseLooks[1];
                    else if (f.Name == "Countdown") style = _baseLooks[2];
                }
                foreach (FieldInfo sf in typeof(ModeStyle).GetFields(Flags))
                {
                    lines.Add(f.Name + "." + sf.Name + "=" + Text(sf.GetValue(style)));
                }
            }
            return lines;
        }

        private static string Text(object v)
        {
            if (v == null) return "";
            if (v is double) return ((double)v).ToString(CultureInfo.InvariantCulture);
            return v.ToString();
        }

        private static void SetParsed(object owner, string name, string val)
        {
            FieldInfo f = owner.GetType().GetField(name, Flags);
            if (f == null) return;
            object parsed = Parse(f.FieldType, val);
            if (parsed != null) f.SetValue(owner, parsed);
        }

        private static object Parse(Type t, string v)
        {
            if (t == typeof(bool)) { bool b; return bool.TryParse(v, out b) ? (object)b : null; }
            if (t == typeof(int))
            {
                int n;
                return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? (object)n : null;
            }
            if (t == typeof(double))
            {
                double d;
                return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? (object)d : null;
            }
            if (t == typeof(string)) return v;
            if (t.IsEnum)
            {
                try { return Enum.Parse(t, v, true); } catch (Exception) { return null; }
            }
            return null;
        }
    }

    // Small helpers so the settings window can read and write any field by name.
    public static class Reflect
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        public static FieldInfo Field(object owner, string name)
        {
            return owner.GetType().GetField(name, Flags);
        }

        public static object Get(object owner, string name)
        {
            return Field(owner, name).GetValue(owner);
        }

        public static void Set(object owner, string name, object value)
        {
            Field(owner, name).SetValue(owner, value);
        }
    }
}
