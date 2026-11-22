// A collapsible card: click the title to fold the body in or out (animated).
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BetterClock
{
    public class AnimatedSection : Border
    {
        private readonly StackPanel _body;
        private readonly Border _bodyClip;
        private readonly RotateTransform _chevron;
        private bool _open;
        private readonly string _title;

        public AnimatedSection(string title, bool open)
        {
            _title = title ?? "";
            SetResourceReference(BackgroundProperty, "Panel2Brush");
            CornerRadius = new CornerRadius(12);
            Margin = new Thickness(0, 0, 0, 10);

            var root = new StackPanel();
            Child = root;

            // ---- header ----
            var header = new Grid();
            header.Background = Brushes.Transparent;
            header.Cursor = Cursors.Hand;
            header.Margin = new Thickness(16, 12, 16, 12);

            var text = new TextBlock();
            text.Text = title;
            text.FontSize = 14;
            text.FontWeight = FontWeights.SemiBold;
            text.SetResourceReference(TextBlock.ForegroundProperty, "InkBrush");
            header.Children.Add(text);

            var arrow = new Path();
            arrow.Data = Geometry.Parse("M 0 0 L 5 5 L 10 0");
            arrow.StrokeThickness = 2;
            arrow.HorizontalAlignment = HorizontalAlignment.Right;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            arrow.SetResourceReference(Shape.StrokeProperty, "MutedBrush");
            arrow.RenderTransformOrigin = new Point(0.5, 0.5);
            _chevron = new RotateTransform(0);
            arrow.RenderTransform = _chevron;
            header.Children.Add(arrow);
            root.Children.Add(header);

            // ---- body ----
            _body = new StackPanel();
            _body.Margin = new Thickness(16, 0, 16, 14);
            _bodyClip = new Border();
            _bodyClip.ClipToBounds = true;
            _bodyClip.Child = _body;
            root.Children.Add(_bodyClip);

            header.MouseLeftButtonUp += delegate { SetOpen(!_open, true); };
            SetOpen(open, false);
        }

        public StackPanel Body { get { return _body; } }
        public string Title { get { return _title; } }

        public void SetOpen(bool open, bool animate)
        {
            _open = open;
            double angle = open ? 180 : 0;

            if (!animate || !UiAnim.Enabled)
            {
                _bodyClip.BeginAnimation(HeightProperty, null);
                _chevron.BeginAnimation(RotateTransform.AngleProperty, null);
                _chevron.Angle = angle;
                _bodyClip.Height = double.NaN;
                _bodyClip.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var spin = new DoubleAnimation(angle, UiAnim.Dur(220));
            spin.EasingFunction = ease;
            _chevron.BeginAnimation(RotateTransform.AngleProperty, spin);

            _bodyClip.Visibility = Visibility.Visible;
            _bodyClip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double full = _bodyClip.DesiredSize.Height;
            double from = double.IsNaN(_bodyClip.Height) ? (open ? 0 : full) : _bodyClip.Height;

            var h = new DoubleAnimation(from, open ? full : 0, UiAnim.Dur(240));
            h.EasingFunction = ease;
            h.Completed += delegate
            {
                if (_open)
                {
                    _bodyClip.BeginAnimation(HeightProperty, null);
                    _bodyClip.Height = double.NaN;
                }
                else
                {
                    _bodyClip.BeginAnimation(HeightProperty, null);
                    _bodyClip.Height = double.NaN;
                    _bodyClip.Visibility = Visibility.Collapsed;
                }
            };
            _bodyClip.BeginAnimation(HeightProperty, h);
        }
    }
}
