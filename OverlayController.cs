// The brain: decides what the overlay shows (Clock, Timer or Countdown) and what the keys do.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;

namespace BetterClock
{
    public enum OverlayMode
    {
        None,
        Clock,
        Timer,
        Countdown
    }

    public class OverlayController
    {
        private readonly DispatcherTimer _tick;
        private readonly Stopwatch _timer = new Stopwatch();
        private readonly Stopwatch _countdown = new Stopwatch();

        private OverlayMode _last = OverlayMode.None;   // the Timer/Countdown you touched last
        private DateTime _clockDeadline = DateTime.MinValue;
        private bool _timerPausedByHide;
        private bool _countdownPausedByHide;
        private bool _countdownFinished;

        // chained countdowns / Pomodoro
        private List<PlanStep> _steps;       // null = one plain countdown
        private string _planKey = null;
        private int _step;

        // warnings and timer target
        private int _lastWarn = -1;
        private string _warnKey = null;
        private readonly List<int> _warnSet = new List<int>();
        private bool _targetReached;

        // alarm, break reminder, session, on-screen notice
        private DateTime _alarmDone = DateTime.MinValue;   // the day the alarm last rang
        private bool _alarmRinging;
        private DateTime _sessionStart = DateTime.Now;
        private DateTime _breakStart = DateTime.Now;
        private string _notice;
        private DateTime _noticeUntil = DateTime.MinValue;

        public OverlayController()
        {
            _tick = new DispatcherTimer();
            _tick.Interval = TimeSpan.FromMilliseconds(100);
            _tick.Tick += OnTick;
            _tick.Start();
        }

        private AppSettings S { get { return AppHost.Settings; } }

        public bool IsVisible(OverlayMode mode)
        {
            OverlayGroup w = AppHost.GetOverlay(mode);
            return w != null && w.IsShown;
        }

        // ------------------------------------------------------------ keys

        public void ToggleClock() { Toggle(OverlayMode.Clock); }
        public void ToggleTimer() { Toggle(OverlayMode.Timer); }
        public void ToggleCountdown() { Toggle(OverlayMode.Countdown); }

        // Shows an overlay (used by "show now" buttons in the settings window).
        public void ShowMode(OverlayMode mode) { Show(mode); }
        public void HideMode(OverlayMode mode) { Hide(mode); }

        // Start/Stop works on the Timer or Countdown you used last (or the one that is on screen).
        public void StartStop() { StartStop(Target()); }
        public void Reset() { Reset(Target()); }

        public void StartStop(OverlayMode mode)
        {
            if (mode == OverlayMode.Timer && IsVisible(OverlayMode.Timer))
            {
                _last = OverlayMode.Timer;
                _timerPausedByHide = false;
                if (_timer.IsRunning) _timer.Stop(); else _timer.Start();
            }
            else if (mode == OverlayMode.Countdown && IsVisible(OverlayMode.Countdown))
            {
                _last = OverlayMode.Countdown;
                _countdownPausedByHide = false;
                if (_countdownFinished) ResetCountdown();
                if (CurrentTotal() < 1) return;

                if (_countdown.IsRunning) _countdown.Stop(); else _countdown.Start();
                SoundService.Stop();
            }
            Refresh();
        }

        // Reset puts the timer back to 00:00 / the countdown back to its full time, and stops it.
        public void Reset(OverlayMode mode)
        {
            if (mode == OverlayMode.Timer && IsVisible(OverlayMode.Timer))
            {
                if (_timer.Elapsed.TotalSeconds >= 1) TimerHistory.Add(_timer.Elapsed.TotalSeconds);
                _timer.Reset();
                _timerPausedByHide = false;
            }
            else if (mode == OverlayMode.Countdown && IsVisible(OverlayMode.Countdown))
            {
                ResetCountdown();
                SoundService.Stop();
            }
            Refresh();
        }

        public void HideAll()
        {
            Hide(OverlayMode.Clock);
            Hide(OverlayMode.Timer);
            Hide(OverlayMode.Countdown);
        }

        // ------------------------------------------------------------ internals

        private OverlayMode Target()
        {
            if (_last != OverlayMode.None && IsVisible(_last)) return _last;
            if (IsVisible(OverlayMode.Timer)) return OverlayMode.Timer;
            if (IsVisible(OverlayMode.Countdown)) return OverlayMode.Countdown;
            return OverlayMode.None;
        }

        private void Toggle(OverlayMode mode)
        {
            if (IsVisible(mode)) Hide(mode);
            else Show(mode);
        }

        private void Show(OverlayMode mode)
        {
            if (mode == OverlayMode.Clock)
            {
                _clockDeadline = DateTime.Now.AddSeconds(S.ClockShowForSeconds);
            }
            else if (mode == OverlayMode.Timer)
            {
                _last = OverlayMode.Timer;
                if (_timerPausedByHide) { _timer.Start(); _timerPausedByHide = false; }
            }
            else if (mode == OverlayMode.Countdown)
            {
                _last = OverlayMode.Countdown;
                if (_countdownPausedByHide) { _countdown.Start(); _countdownPausedByHide = false; }
            }

            Refresh();
            AppHost.ShowOverlay(mode);
        }

        private void Hide(OverlayMode mode)
        {
            if (mode == OverlayMode.Clock && _alarmRinging) StopAlarm();
            if (mode == OverlayMode.Timer)
            {
                if (IsVisible(mode) && !S.TimerKeepRunningHidden && _timer.IsRunning)
                {
                    _timer.Stop();
                    _timerPausedByHide = true;
                }
            }
            else if (mode == OverlayMode.Countdown && IsVisible(mode))
            {
                SoundService.Stop();
                if (_countdownFinished)
                {
                    ResetCountdown();   // "Toggle off" after the alarm = done, ready for next time
                }
                else if (!S.CountdownKeepRunningHidden && _countdown.IsRunning)
                {
                    _countdown.Stop();
                    _countdownPausedByHide = true;
                }
            }
            AppHost.HideOverlay(mode);
        }

        private void ResetCountdown()
        {
            _countdown.Reset();
            _countdownFinished = false;
            _countdownPausedByHide = false;
            _step = 0;
            _lastWarn = -1;
        }

        // Rebuilds the list of steps when the plan settings change.
        private void SyncPlan()
        {
            string key = CountdownPlan.Key(S);
            if (key == _planKey) return;
            _planKey = key;
            _steps = CountdownPlan.Build(S);
            if (_steps == null || _step >= _steps.Count) _step = 0;
        }

        // Length of the countdown (or of the current step) in seconds.
        private int CurrentTotal()
        {
            SyncPlan();
            return _steps != null ? _steps[_step].Seconds : S.CountdownTotalSeconds;
        }

        private double CountdownRemainingSeconds()
        {
            double left = CurrentTotal() - _countdown.Elapsed.TotalSeconds;
            return left < 0 ? 0 : left;
        }

        // Moves a chained countdown on to its next step. False = there is no next step (it is finished).
        private bool AdvanceStep()
        {
            if (_steps == null) return false;
            int next = _step + 1;
            if (next >= _steps.Count)
            {
                if (!CountdownPlan.Loops(S)) return false;
                next = 0;
            }
            _step = next;
            _countdown.Reset();
            _countdown.Start();
            _lastWarn = -1;
            SoundService.PlayChime();
            return true;
        }

        private bool WarnAt(int seconds)
        {
            string text = S.CountdownWarnList ?? "";
            if (text != _warnKey)
            {
                _warnKey = text;
                _warnSet.Clear();
                foreach (string part in text.Split(new char[] { ',', ';', ' ' }))
                {
                    int v;
                    if (int.TryParse(part.Trim(), out v) && v > 0) _warnSet.Add(v);
                }
            }
            return _warnSet.Contains(seconds);
        }

        private void CheckWarnings()
        {
            if (!S.CountdownWarnBeeps || !_countdown.IsRunning || _countdownFinished) return;
            int whole = (int)Math.Ceiling(CountdownRemainingSeconds());
            if (whole == _lastWarn) return;
            _lastWarn = whole;
            if (whole < 1 || whole >= CurrentTotal()) return;
            if (WarnAt(whole)) SoundService.PlayWarning();
        }

        private void CheckTarget()
        {
            if (!S.TimerTargetEnabled) { _targetReached = false; return; }
            bool reached = _timer.Elapsed.TotalSeconds >= S.TimerTargetSeconds;
            if (reached && !_targetReached)
            {
                _targetReached = true;
                if (S.TimerTargetBeep) SoundService.PlayChime();
            }
            else if (!reached)
            {
                _targetReached = false;
            }
        }

        // Green while there is plenty of time, amber then red as it runs low.
        private OverlayRole RunningRole(double remaining)
        {
            if (S.CountdownLowColors)
            {
                double whole = Math.Ceiling(remaining);
                if (whole <= S.CountdownRedAt) return OverlayRole.Danger;
                if (whole <= S.CountdownAmberAt) return OverlayRole.Warning;
            }
            return OverlayRole.Running;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // The countdown can reach zero even while its overlay is hidden.
            SyncPlan();
            if (_countdown.IsRunning && !_countdownFinished && CountdownRemainingSeconds() <= 0)
            {
                if (!AdvanceStep()) FinishCountdown();
            }
            CheckWarnings();
            CheckTarget();

            CheckAlarm(DateTime.Now);
            CheckBreak(DateTime.Now);

            if (IsVisible(OverlayMode.Clock) && !AppHost.MoveMode && !S.ClockHoldOpen && !_alarmRinging && DateTime.Now >= _clockDeadline)
            {
                Hide(OverlayMode.Clock);
            }
            Refresh();
        }

        private void FinishCountdown()
        {
            if (!S.CountdownOvertime) _countdown.Stop();   // with overtime it keeps counting up
            _countdownFinished = true;
            SoundService.PlayCountdownAlarm(S, false);

            if (!IsVisible(OverlayMode.Countdown) && S.CountdownAutoShowAtZero)
            {
                Show(OverlayMode.Countdown);
            }
        }

        private void Refresh()
        {
            OverlayGroup o = AppHost.GetOverlay(OverlayMode.Clock);
            if (o != null && o.IsShown)
            {
                o.SetText(FormatClock(S, DateTime.Now));
                o.SetRole(OverlayRole.Normal);
                o.SetInfo(BuildInfo(S, DateTime.Now, _sessionStart));
                o.SetCaption(CurrentNotice());
            }

            o = AppHost.GetOverlay(OverlayMode.Timer);
            if (o != null && o.IsShown)
            {
                double el = _timer.Elapsed.TotalSeconds;
                o.SetText(FormatTime((int)Math.Floor(el)));
                if (S.TimerTargetEnabled && el >= S.TimerTargetSeconds) o.SetRole(OverlayRole.Target);
                else if (_timer.IsRunning) o.SetRole(OverlayRole.Running);
                else if (el >= 1) o.SetRole(OverlayRole.Paused);
                else o.SetRole(OverlayRole.Normal);
                o.SetProgress(S.TimerTargetEnabled && S.TimerTargetSeconds > 0 ? el / S.TimerTargetSeconds : -1);
            }

            o = AppHost.GetOverlay(OverlayMode.Countdown);
            if (o != null && o.IsShown)
            {
                int total = CurrentTotal();
                double rem = CountdownRemainingSeconds();
                if (_countdownFinished && S.CountdownOvertime)
                    o.SetText("+" + FormatTime((int)Math.Floor(_countdown.Elapsed.TotalSeconds - total)));
                else
                    o.SetText(FormatTime((int)Math.Ceiling(rem)));
                o.SetCaption(_steps != null ? _steps[_step].Name : null);

                if (_countdownFinished)
                {
                    bool on = true;
                    if (S.CountdownFlash)
                    {
                        long half = DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond * 500);
                        on = (half % 2) == 0;
                    }
                    OverlayRole doneRole = S.CountdownOvertime ? OverlayRole.Overtime : OverlayRole.Finished;
                    o.SetRole(on ? doneRole : OverlayRole.Normal);
                }
                else if (_countdown.IsRunning) o.SetRole(RunningRole(rem));
                else if (_countdown.Elapsed.TotalSeconds >= 1) o.SetRole(OverlayRole.Paused);
                else o.SetRole(OverlayRole.Normal);

                o.SetProgress(_countdownFinished ? 0 : (total > 0 ? rem / total : 0));
                bool pulse = _countdown.IsRunning && !_countdownFinished
                    && S.Countdown.PulseSeconds > 0 && Math.Ceiling(rem) <= S.Countdown.PulseSeconds;
                o.SetPulse(pulse);
            }
        }

        // ------------------------------------------------------------ alarm, break reminder, notices

        private string CurrentNotice()
        {
            if (_notice == null) return null;
            if (DateTime.Now >= _noticeUntil) { _notice = null; return null; }
            return _notice;
        }

        private void ShowNotice(string text, double seconds, bool hold)
        {
            _notice = text;
            _noticeUntil = hold ? DateTime.MaxValue : DateTime.Now.AddSeconds(seconds);
        }

        private void CheckAlarm(DateTime now)
        {
            if (!S.AlarmEnabled || _alarmRinging) return;
            TimeSpan ts;
            if (!TimeSpan.TryParse(S.AlarmTime, CultureInfo.InvariantCulture, out ts)) return;
            if (ts < TimeSpan.Zero || ts.TotalHours >= 24) return;

            DateTime at = now.Date + ts;
            // rings within the first hour after the set time (so an alarm is not lost if the PC was busy)
            if (now >= at && now < at.AddHours(1) && _alarmDone != now.Date)
            {
                _alarmDone = now.Date;
                if (!S.AlarmRepeatDaily)
                {
                    S.AlarmEnabled = false;
                    AppHost.SaveSettings();
                }
                RingAlarm();
            }
        }

        private void RingAlarm()
        {
            _alarmRinging = true;
            string label = string.IsNullOrEmpty(S.AlarmLabel) ? "Alarm" : S.AlarmLabel;
            ShowNotice(label, 0, true);
            Show(OverlayMode.Clock);
            SoundService.PlayCountdownAlarm(S, true);
        }

        // Rings the alarm now (the Test button).
        public void TestAlarm() { RingAlarm(); }

        // Silences the alarm and removes its message. Hiding the clock does the same.
        public void StopAlarm()
        {
            _alarmRinging = false;
            _notice = null;
            SoundService.Stop();
        }

        private void CheckBreak(DateTime now)
        {
            if (!S.BreakReminderEnabled) { _breakStart = now; return; }
            int mins = ColorUtil.Clamp(S.BreakEveryMinutes, 1, 600);
            if ((now - _breakStart).TotalMinutes < mins) return;

            _breakStart = now;
            ShowNotice("Time for a break!", 12, false);
            SoundService.PlayChime();
            if (!_alarmRinging)
            {
                Show(OverlayMode.Clock);
                _clockDeadline = DateTime.Now.AddSeconds(12);
            }
        }

        // Starts the session timer (and the break reminder) again from zero.
        public void ResetSession()
        {
            _sessionStart = DateTime.Now;
            _breakStart = DateTime.Now;
        }

        // ------------------------------------------------------------ extra clock lines (also used by the preview)

        private static string _worldKey = null;
        private static readonly List<KeyValuePair<string, TimeZoneInfo>> WorldZones = new List<KeyValuePair<string, TimeZoneInfo>>();

        public static string BuildInfo(AppSettings s, DateTime now, DateTime sessionStart)
        {
            var lines = new List<string>();

            // world clocks
            string list = s.WorldClocks ?? "";
            if (list != _worldKey)
            {
                _worldKey = list;
                WorldZones.Clear();
                foreach (string part in list.Split(';'))
                {
                    int bar = part.IndexOf('|');
                    if (bar <= 0) continue;
                    try
                    {
                        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(part.Substring(bar + 1).Trim());
                        WorldZones.Add(new KeyValuePair<string, TimeZoneInfo>(part.Substring(0, bar).Trim(), tz));
                    }
                    catch (Exception)
                    {
                        // unknown time zone name: skip it
                    }
                }
            }
            string fmt = s.Clock24Hour ? "HH:mm" : "h:mm tt";
            foreach (KeyValuePair<string, TimeZoneInfo> z in WorldZones)
            {
                DateTime there = TimeZoneInfo.ConvertTime(DateTime.UtcNow, z.Value);
                lines.Add(z.Key + "  " + there.ToString(fmt, CultureInfo.InvariantCulture));
            }

            // countdown to a date
            if (s.DateCountdownEnabled)
            {
                DateTime target;
                string name = string.IsNullOrEmpty(s.DateCountdownName) ? "" : s.DateCountdownName + ": ";
                if (DateTime.TryParse(s.DateCountdownTarget, CultureInfo.InvariantCulture, DateTimeStyles.None, out target))
                {
                    TimeSpan left = target - now;
                    if (left.TotalSeconds <= 0)
                    {
                        lines.Add(name + "now!");
                    }
                    else
                    {
                        string days = left.Days > 0 ? left.Days + "d " : "";
                        lines.Add(name + days + string.Format("{0:00}:{1:00}:{2:00}", left.Hours, left.Minutes, left.Seconds));
                    }
                }
                else
                {
                    lines.Add(name + "(type a date like 2026-12-25 18:00)");
                }
            }

            // days since a date
            if (s.DaysSinceEnabled)
            {
                DateTime since;
                string name = string.IsNullOrEmpty(s.DaysSinceName) ? "" : s.DaysSinceName + ": ";
                if (DateTime.TryParse(s.DaysSinceDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out since))
                {
                    int days = (int)(now.Date - since.Date).TotalDays;
                    lines.Add(name + days + (days == 1 ? " day" : " days"));
                }
                else
                {
                    lines.Add(name + "(type a date like 2026-01-31)");
                }
            }

            // how long you have been playing
            if (s.SessionShow)
            {
                lines.Add("Session " + FormatTime((int)(now - sessionStart).TotalSeconds));
            }

            return string.Join("\n", lines.ToArray());
        }

        // ------------------------------------------------------------ formatting (also used by the preview)

        public static string FormatClock(AppSettings s, DateTime now)
        {
            string fmt;
            if (s.Clock24Hour) fmt = s.ClockShowSeconds ? "HH:mm:ss" : "HH:mm";
            else fmt = s.ClockShowSeconds ? "h:mm:ss tt" : "h:mm tt";
            return now.ToString(fmt, CultureInfo.InvariantCulture);
        }

        // 75 -> "01:15",  3725 -> "1:02:05"
        public static string FormatTime(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int sec = totalSeconds % 60;
            if (h > 0) return string.Format("{0}:{1:00}:{2:00}", h, m, sec);
            return string.Format("{0:00}:{1:00}", m, sec);
        }
    }
}
