// The special ways of drawing the numbers: seven-segment LED, flip-clock cards, analog face.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BetterClock
{
    // ---------------------------------------------------------------- LED (seven segments)
    public class SegmentDisplay : FrameworkElement
    {
        // segments: a=1 b=2 c=4 d=8 e=16 f=32 g=64
        private static readonly int[] Digits = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

        private string _text = "";
        private Brush _brush = Brushes.White;
        private double _height = 50;
        private bool _hideColon;
        public char SepChar = ':';          // the separator that blinks

        public void Set(string text, Brush brush, double digitHeight, bool hideColon)
        {
            bool relayout = text != _text || Math.Abs(digitHeight - _height) > 0.01;
            _text = text;
            _brush = brush;
            _height = Math.Max(8, digitHeight);
            _hideColon = hideColon;
            if (relayout) InvalidateMeasure();
            InvalidateVisual();
        }

        private double DigitW { get { return _height * 0.56; } }
        private double Gap { get { return _height * 0.14; } }

        private double WidthOf(char c)
        {
            if (c >= '0' && c <= '9') return DigitW;
            if (c == ':' || c == '.') return _height * 0.22;
            if (c == '-') return DigitW * 0.7;
            if (c == ' ') return DigitW * 0.5;
            return LetterText(c, Brushes.White).Width;
        }

        private FormattedText LetterText(char c, Brush b)
        {
            return new FormattedText(c.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), _height * 0.38, b);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double w = 0;
            for (int i = 0; i < _text.Length; i++)
            {
                w += WidthOf(_text[i]);
                if (i < _text.Length - 1) w += Gap;
            }
            return new Size(w, _height);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double x = 0;
            double t = DigitW * 0.2;
            for (int i = 0; i < _text.Length; i++)
            {
                char c = _text[i];
                double w = WidthOf(c);
                if (c >= '0' && c <= '9')
                {
                    DrawDigit(dc, x, Digits[c - '0'], w, t);
                }
                else if (c == ':')
                {
                    if (!(_hideColon && c == SepChar))
                    {
                        double s = t * 1.1;
                        dc.DrawRectangle(_brush, null, new Rect(x + (w - s) / 2, _height * 0.30, s, s));
                        dc.DrawRectangle(_brush, null, new Rect(x + (w - s) / 2, _height * 0.64, s, s));
                    }
                }
                else if (c == '.')
                {
                    if (!(_hideColon && c == SepChar))
                    {
                        double s = t * 1.1;
                        dc.DrawRectangle(_brush, null, new Rect(x + (w - s) / 2, _height - s, s, s));
                    }
                }
                else if (c == '-')
                {
                    if (!(_hideColon && c == SepChar))
                        dc.DrawRectangle(_brush, null, new Rect(x + w * 0.1, _height / 2 - t / 2, w * 0.8, t));
                }
                else if (c != ' ')
                {
                    FormattedText ft = LetterText(c, _brush);
                    dc.DrawText(ft, new Point(x, _height - ft.Height));
                }
                x += w + Gap;
            }
        }

        private void DrawDigit(DrawingContext dc, double x, int mask, double w, double t)
        {
            double h = _height;
            double half = h / 2;
            double r = t * 0.35;
            double vLen = half - t * 0.9;
            // a b c d e f g
            Rect[] seg =
            {
                new Rect(x + t * 0.6, 0, w - t * 1.2, t),                    // a
                new Rect(x + w - t, t * 0.7, t, vLen),                       // b
                new Rect(x + w - t, half + t * 0.2, t, vLen),                // c
                new Rect(x + t * 0.6, h - t, w - t * 1.2, t),                // d
                new Rect(x, half + t * 0.2, t, vLen),                        // e
                new Rect(x, t * 0.7, t, vLen),                               // f
                new Rect(x + t * 0.6, half - t / 2, w - t * 1.2, t)          // g
            };
            for (int i = 0; i < 7; i++)
            {
                bool on = (mask & (1 << i)) != 0;
                if (!on) dc.PushOpacity(0.07);
                dc.DrawRoundedRectangle(_brush, null, seg[i], r, r);
                if (!on) dc.Pop();
            }
        }
    }

    // ---------------------------------------------------------------- analog clock face
    public class AnalogFace : FrameworkElement
    {
        private double _diameter = 120;
        private Brush _ink = Brushes.White;
        private Brush _accent = Brushes.Red;
        private bool _seconds = true;
        private DateTime _time = DateTime.Now;

        public void Set(DateTime time, double diameter, Brush ink, Brush accent, bool showSeconds)
        {
            bool relayout = Math.Abs(diameter - _diameter) > 0.01;
            _time = time;
            _diameter = Math.Max(30, diameter);
            _ink = ink;
            _accent = accent;
            _seconds = showSeconds;
            if (relayout) InvalidateMeasure();
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(_diameter, _diameter);
        }

        private static Point OnCircle(Point c, double radius, double degrees)
        {
            double a = (degrees - 90) * Math.PI / 180.0;
            return new Point(c.X + radius * Math.Cos(a), c.Y + radius * Math.Sin(a));
        }

        private static Pen RoundPen(Brush b, double thickness)
        {
            var p = new Pen(b, thickness);
            p.StartLineCap = PenLineCap.Round;
            p.EndLineCap = PenLineCap.Round;
            return p;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double r = _diameter / 2 - 3;
            var c = new Point(_diameter / 2, _diameter / 2);

            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), RoundPen(_ink, Math.Max(1.5, _diameter / 60)), c, r, r);

            for (int i = 0; i < 12; i++)
            {
                bool big = i % 3 == 0;
                double len = big ? r * 0.16 : r * 0.08;
                dc.DrawLine(RoundPen(_ink, big ? Math.Max(2, _diameter / 45) : Math.Max(1, _diameter / 80)),
                    OnCircle(c, r - len, i * 30), OnCircle(c, r - 3, i * 30));
            }

            double hour = (_time.Hour % 12 + _time.Minute / 60.0) * 30;
            double min = (_time.Minute + _time.Second / 60.0) * 6;
            double sec = (_time.Second + _time.Millisecond / 1000.0) * 6;

            dc.DrawLine(RoundPen(_ink, Math.Max(3, _diameter / 22)), c, OnCircle(c, r * 0.5, hour));
            dc.DrawLine(RoundPen(_ink, Math.Max(2, _diameter / 34)), c, OnCircle(c, r * 0.76, min));
            if (_seconds)
            {
                dc.DrawLine(RoundPen(_accent, Math.Max(1.2, _diameter / 70)), OnCircle(c, -r * 0.15, sec), OnCircle(c, r * 0.86, sec));
            }
            dc.DrawEllipse(_seconds ? _accent : _ink, null, c, _diameter / 28, _diameter / 28);
        }
    }

    // ---------------------------------------------------------------- flip-clock cards
    public class FlipDisplay : StackPanel
    {
        private readonly List<TextBlock> _glyphs = new List<TextBlock>();
        private string _last = "";
        public char SepChar = ':';          // the separator that blinks

        private static bool IsSep(char c)
        {
            return c == ':' || c == ' ' || c == '.' || c == '-';
        }

        public FlipDisplay()
        {
            Orientation = Orientation.Horizontal;
        }

        public void Set(string text, double fontSize, Brush fg, bool hideColon)
        {
            bool rebuild = text.Length != _last.Length;
            if (rebuild)
            {
                Children.Clear();
                _glyphs.Clear();
                for (int i = 0; i < text.Length; i++)
                {
                    var tb = new TextBlock();
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.RenderTransformOrigin = new Point(0.5, 0.5);
                    tb.RenderTransform = new ScaleTransform(1, 1);
                    Typography.SetNumeralAlignment(tb, FontNumeralAlignment.Tabular);
                    _glyphs.Add(tb);

                    if (IsSep(text[i]))
                    {
                        tb.Margin = new Thickness(3, 0, 3, 0);
                        Children.Add(tb);
                    }
                    else
                    {
                        var grid = new Grid();
                        grid.Children.Add(tb);
                        var line = new Rectangle();
                        line.Height = Math.Max(1.5, fontSize / 28);
                        line.Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
                        line.VerticalAlignment = VerticalAlignment.Center;
                        grid.Children.Add(line);

                        var card = new Border();
                        card.CornerRadius = new CornerRadius(Math.Max(4, fontSize / 9));
                        card.Padding = new Thickness(fontSize * 0.16, fontSize * 0.03, fontSize * 0.16, fontSize * 0.03);
                        card.Margin = new Thickness(2, 0, 2, 0);
                        card.Background = new LinearGradientBrush(Color.FromRgb(0x34, 0x37, 0x4C), Color.FromRgb(0x17, 0x19, 0x26), 90);
                        card.Child = grid;
                        Children.Add(card);
                    }
                }
            }

            for (int i = 0; i < text.Length; i++)
            {
                TextBlock tb = _glyphs[i];
                string ch = text[i].ToString();
                tb.FontSize = fontSize * 0.9;
                tb.Foreground = fg;
                if (text[i] == SepChar) tb.Opacity = hideColon ? 0 : 1;

                if (tb.Text != ch)
                {
                    tb.Text = ch;
                    if (!rebuild && UiAnim.Enabled && !IsSep(text[i]))
                    {
                        var st = (ScaleTransform)tb.RenderTransform;
                        var a = new DoubleAnimation(0.05, 1, TimeSpan.FromMilliseconds(170));
                        a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                        st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
                    }
                }
            }
            _last = text;
        }
    }
}
