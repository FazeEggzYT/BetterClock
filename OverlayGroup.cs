// One overlay (Clock, Timer or Countdown) as the rest of the app sees it.
// It owns one window per monitor it is shown on, so "All monitors" simply uses several windows.
using System.Collections.Generic;

namespace BetterClock
{
    public class OverlayGroup
    {
        private readonly OverlayMode _mode;
        private readonly List<OverlayWindow> _windows = new List<OverlayWindow>();
        private string _text = "00:00";
        private OverlayRole _role = OverlayRole.Normal;
        private double _progress = -1;
        private string _caption = null;
        private bool _pulse;
        private bool _wanted;
        private bool _move;

        public OverlayGroup(OverlayMode mode)
        {
            _mode = mode;
            _windows.Add(NewWindow());
        }

        private OverlayWindow NewWindow()
        {
            var w = new OverlayWindow(_mode);
            w.SetText(_text);
            w.SetRole(_role);
            w.SetProgress(_progress);
            w.SetCaption(_caption);
            w.SetPulse(_pulse);
            w.SetMoveMode(_move);
            return w;
        }

        public bool IsShown { get { return _wanted; } }

        public void SetText(string text)
        {
            _text = text;
            foreach (OverlayWindow w in _windows) w.SetText(text);
        }

        public void SetRole(OverlayRole role)
        {
            _role = role;
            foreach (OverlayWindow w in _windows) w.SetRole(role);
        }

        public void SetProgress(double fraction)
        {
            _progress = fraction;
            foreach (OverlayWindow w in _windows) w.SetProgress(fraction);
        }

        public void SetCaption(string text)
        {
            _caption = text;
            foreach (OverlayWindow w in _windows) w.SetCaption(text);
        }

        public void SetPulse(bool on)
        {
            _pulse = on;
            foreach (OverlayWindow w in _windows) w.SetPulse(on);
        }

        public void SetMoveMode(bool on)
        {
            _move = on;
            foreach (OverlayWindow w in _windows) w.SetMoveMode(on);
        }

        // Shows (or re-places) the overlay on every monitor it should be on.
        public void ShowNow()
        {
            _wanted = true;
            ModeStyle st = AppHost.Settings.StyleFor(_mode);
            List<ScreenInfo> targets = MonitorHelper.Targets(st.MonitorMode, st.MonitorIndex);

            while (_windows.Count < targets.Count) _windows.Add(NewWindow());

            for (int i = 0; i < _windows.Count; i++)
            {
                if (i < targets.Count) _windows[i].ShowOn(targets[i]);
                else _windows[i].HideOverlay();
            }
        }

        public void HideNow()
        {
            _wanted = false;
            foreach (OverlayWindow w in _windows) w.HideOverlay();
        }

        // Called when any setting changed.
        public void ApplySettings()
        {
            foreach (OverlayWindow w in _windows) w.ApplySettings();
            if (_wanted) ShowNow();
        }

        public void Close()
        {
            foreach (OverlayWindow w in _windows) w.Close();
            _windows.Clear();
        }
    }
}
