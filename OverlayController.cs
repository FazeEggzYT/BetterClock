// The brain: decides what the overlay shows (Clock, Timer or Countdown) and what the keys do.
using System;
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
                if (S.CountdownTotalSeconds < 1) return;

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
        }

        private double CountdownRemainingSeconds()
        {
            double left = S.CountdownTotalSeconds - _countdown.Elapsed.TotalSeconds;
            return left < 0 ? 0 : left;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // The countdown can reach zero even while its overlay is hidden.
            if (_countdown.IsRunning && CountdownRemainingSeconds() <= 0)
            {
                FinishCountdown();
            }

            if (IsVisible(OverlayMode.Clock) && !AppHost.MoveMode && !S.ClockHoldOpen && DateTime.Now >= _clockDeadline)
            {
                Hide(OverlayMode.Clock);
            }
            Refresh();
        }

        private void FinishCountdown()
        {
            _countdown.Stop();
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
            }

            o = AppHost.GetOverlay(OverlayMode.Timer);
            if (o != null && o.IsShown)
            {
                o.SetText(FormatTime((int)Math.Floor(_timer.Elapsed.TotalSeconds)));
                if (_timer.IsRunning) o.SetRole(OverlayRole.Running);
                else if (_timer.Elapsed.TotalSeconds >= 1) o.SetRole(OverlayRole.Paused);
                else o.SetRole(OverlayRole.Normal);
            }

            o = AppHost.GetOverlay(OverlayMode.Countdown);
            if (o != null && o.IsShown)
            {
                o.SetText(FormatTime((int)Math.Ceiling(CountdownRemainingSeconds())));
                if (_countdownFinished)
                {
                    bool on = true;
                    if (S.CountdownFlash)
                    {
                        long half = DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond * 500);
                        on = (half % 2) == 0;
                    }
                    o.SetRole(on ? OverlayRole.Finished : OverlayRole.Normal);
                }
                else if (_countdown.IsRunning) o.SetRole(OverlayRole.Running);
                else if (_countdown.Elapsed.TotalSeconds >= 1) o.SetRole(OverlayRole.Paused);
                else o.SetRole(OverlayRole.Normal);
            }
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
