// The overlay itself: always on top, click-through, never steals focus from the game.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BetterClock
{
    public class OverlayWindow : Window
    {
        private readonly OverlayView _view;
        private readonly DispatcherTimer _keepOnTop;
        private IntPtr _hwnd = IntPtr.Zero;
        private int _showVersion;
        private bool _wanted;
        private ScreenInfo _screen;
        private readonly OverlayMode _mode;

        private ModeStyle St { get { return AppHost.Settings.StyleFor(_mode); } }

        public OverlayWindow(OverlayMode mode)
        {
            _mode = mode;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = -20000;
            Top = -20000;
            Opacity = 0;
            Title = "Better Clock Overlay";

            _view = new OverlayView();
            _view.Kind = mode;
            _view.Apply(St);
            Content = _view;

            // Some games push their own window above ours. This nudges us back to the top.
            _keepOnTop = new DispatcherTimer();
            _keepOnTop.Interval = TimeSpan.FromSeconds(1);
            _keepOnTop.Tick += delegate { ReassertTopmost(); };

            SizeChanged += delegate { if (IsVisible) Reposition(); };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _hwnd = new WindowInteropHelper(this).Handle;
            int ex = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
            ex |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            if (!_move) ex |= NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, ex);
        }

        // ------------------------------------------------------------ move mode (drag with the mouse)

        private bool _move;

        public void SetMoveMode(bool on)
        {
            _move = on;
            if (_hwnd != IntPtr.Zero)
            {
                int ex = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
                if (on) ex &= ~NativeMethods.WS_EX_TRANSPARENT;
                else ex |= NativeMethods.WS_EX_TRANSPARENT;
                NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, ex);
            }
            Cursor = on ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.Arrow;
            _view.SetMoveOutline(on);
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!_move) return;
            try { DragMove(); } catch (InvalidOperationException) { return; }
            CommitDrag();
        }

        // After a drag: remember the exact pixel position (and the monitor it was dropped on).
        private void CommitDrag()
        {
            if (_hwnd == IntPtr.Zero) return;
            double scale = 1.0;
            PresentationSource src = PresentationSource.FromVisual(this);
            if (src != null && src.CompositionTarget != null) scale = src.CompositionTarget.TransformToDevice.M11;

            int om = (int)Math.Round(_view.OuterMargin * scale);
            int px = (int)Math.Round(Left * scale) + om;
            int py = (int)Math.Round(Top * scale) + om;
            int cx = px + (int)(ActualWidth * scale / 2);
            int cy = py + (int)(ActualHeight * scale / 2);

            ModeStyle st = St;
            ScreenInfo target = _screen;
            if (st.MonitorMode != MonitorMode.All)
            {
                List<ScreenInfo> all = MonitorHelper.All();
                for (int i = 0; i < all.Count; i++)
                {
                    if (cx >= all[i].Left && cx < all[i].Right && cy >= all[i].Top && cy < all[i].Bottom)
                    {
                        target = all[i];
                        st.MonitorMode = MonitorMode.Specific;
                        st.MonitorIndex = i;
                        break;
                    }
                }
            }
            if (target == null) return;

            st.UseCustomPosition = true;
            st.CustomX = Math.Max(0, px - target.Left);
            st.CustomY = Math.Max(0, py - target.Top);
            AppHost.RaiseMoved(_mode);
        }

        public void SetText(string text) { _view.SetText(text); }
        public void SetRole(OverlayRole role) { _view.SetRole(role); }
        public void SetProgress(double fraction) { _view.SetProgress(fraction); }
        public void SetCaption(string text) { _view.SetCaption(text); }
        public void SetPulse(bool on) { _view.SetPulse(on); }

        // True from the moment we were asked to show until we were asked to hide.
        public bool IsShown
        {
            get { return _wanted; }
        }

        private double TargetOpacity
        {
            get { return ColorUtil.Clamp(St.OverallOpacity, 5, 100) / 100.0; }
        }

        private TimeSpan FadeTime
        {
            get
            {
                if (St.ShowAnimation == ShowAnimation.None) return TimeSpan.Zero;
                return TimeSpan.FromMilliseconds(ColorUtil.Clamp(St.FadeMs, 0, 2000));
            }
        }

        // Called when any setting changes.
        public void ApplySettings()
        {
            _view.Apply(St);
            if (IsVisible)
            {
                UpdateLayout();
                Reposition();
                if (_wanted)
                {
                    BeginAnimation(OpacityProperty, null);
                    Opacity = TargetOpacity;
                }
            }
        }

        // Show on the given monitor (pixels). Fades in.
        public void ShowOn(ScreenInfo screen)
        {
            if (screen == null) return;
            _screen = screen;
            _showVersion++;
            _wanted = true;

            bool wasVisible = IsVisible;
            if (!wasVisible)
            {
                Opacity = 0;
                Show();
            }
            UpdateLayout();
            Reposition();
            _keepOnTop.Start();
            if (!wasVisible) _view.PlayShow(St.ShowAnimation, St.Position);

            if (FadeTime.TotalMilliseconds < 1)
            {
                BeginAnimation(OpacityProperty, null);
                Opacity = TargetOpacity;
            }
            else
            {
                var fade = new DoubleAnimation(Opacity, TargetOpacity, FadeTime);
                BeginAnimation(OpacityProperty, fade);
            }
        }

        public void HideOverlay()
        {
            _wanted = false;
            if (!IsVisible) return;
            int version = ++_showVersion;

            if (FadeTime.TotalMilliseconds < 1)
            {
                _keepOnTop.Stop();
                BeginAnimation(OpacityProperty, null);
                Opacity = 0;
                Hide();
                return;
            }

            var fade = new DoubleAnimation(Opacity, 0, FadeTime);
            fade.Completed += delegate
            {
                // If it was shown again during the fade, leave it alone.
                if (version == _showVersion)
                {
                    _keepOnTop.Stop();
                    Hide();
                }
            };
            BeginAnimation(OpacityProperty, fade);
        }

        // Works out the pixel position from the monitor, the 3x3 position and the margin.
        public void Reposition()
        {
            if (_screen == null || _hwnd == IntPtr.Zero) return;

            double scale = 1.0;
            PresentationSource src = PresentationSource.FromVisual(this);
            if (src != null && src.CompositionTarget != null)
            {
                scale = src.CompositionTarget.TransformToDevice.M11;
            }

            int w = (int)Math.Ceiling(ActualWidth * scale);
            int h = (int)Math.Ceiling(ActualHeight * scale);
            int m = ColorUtil.Clamp(St.EdgeMargin, 0, 1000);
            int om = (int)Math.Round(_view.OuterMargin * scale);   // glow space around the pill

            int x;
            int y;
            if (St.UseCustomPosition)
            {
                // exact pixels from the top-left of the monitor (the pill, not the glow)
                x = _screen.Left + St.CustomX - om;
                y = _screen.Top + St.CustomY - om;
                int maxX = _screen.Right - w + om;
                int maxY = _screen.Bottom - h + om;
                if (x > maxX) x = maxX;
                if (y > maxY) y = maxY;
                if (x < _screen.Left - om) x = _screen.Left - om;
                if (y < _screen.Top - om) y = _screen.Top - om;
            }
            else
            {
                int pos = (int)St.Position;
                int col = pos % 3;   // 0 left, 1 centre, 2 right
                int row = pos / 3;   // 0 top, 1 middle, 2 bottom

                if (col == 0) x = _screen.Left + m - om;
                else if (col == 1) x = _screen.Left + (_screen.Width - w) / 2;
                else x = _screen.Right - w - m + om;

                if (row == 0) y = _screen.Top + m - om;
                else if (row == 1) y = _screen.Top + (_screen.Height - h) / 2;
                else y = _screen.Bottom - h - m + om;
            }

            NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, x, y, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }

        private void ReassertTopmost()
        {
            if (_hwnd == IntPtr.Zero || !IsVisible) return;
            NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }
}
