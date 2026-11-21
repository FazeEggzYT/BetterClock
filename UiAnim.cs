// Small helpers for the animated settings window. They all obey Settings > "Animations".
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BetterClock
{
    public static class UiAnim
    {
        public static bool Enabled = true;
        public static int SpeedPercent = 100;

        public static Duration Dur(int ms)
        {
            if (!Enabled) return new Duration(TimeSpan.Zero);
            double scaled = ms * 100.0 / Math.Max(25, SpeedPercent);
            return new Duration(TimeSpan.FromMilliseconds(scaled));
        }

        // Fades an element in while it slides from (fromX, fromY) to its place.
        public static void FadeSlideIn(FrameworkElement el, double fromX, double fromY, int ms)
        {
            if (!Enabled || el == null) return;
            var t = new TranslateTransform(fromX, fromY);
            el.RenderTransform = t;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var fade = new DoubleAnimation(0, 1, Dur(ms));
            fade.EasingFunction = ease;
            el.BeginAnimation(UIElement.OpacityProperty, fade);

            if (fromX != 0)
            {
                var ax = new DoubleAnimation(fromX, 0, Dur(ms));
                ax.EasingFunction = ease;
                t.BeginAnimation(TranslateTransform.XProperty, ax);
            }
            if (fromY != 0)
            {
                var ay = new DoubleAnimation(fromY, 0, Dur(ms));
                ay.EasingFunction = ease;
                t.BeginAnimation(TranslateTransform.YProperty, ay);
            }
        }

        // A quick "pop" (grow a little then settle) for things that just changed.
        public static void Pop(FrameworkElement el)
        {
            if (!Enabled || el == null) return;
            el.RenderTransformOrigin = new Point(0.5, 0.5);
            var s = new ScaleTransform(1, 1);
            el.RenderTransform = s;
            var a = new DoubleAnimation(0.94, 1, Dur(220));
            a.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };
            s.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            s.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }
    }
}
