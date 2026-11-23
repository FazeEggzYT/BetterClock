// Draws the big numbers when an outline, gradient, letter spacing or line height is used (v1.4.5).
// Without those effects the normal TextBlock is used instead, exactly as before.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BetterClock
{
    public class NumberText : FrameworkElement
    {
        private string _text = "";
        private string _hidden = "";                 // characters that keep their space but are not drawn (blinking colon)
        private FontFamily _family = new FontFamily("Segoe UI");
        private bool _bold;
        private bool _italic;
        private double _size = 40;
        private double _spacing;
        private double _lineHeight = 1.0;
        private double _outline;
        private Color _outlineColor = Colors.Black;
        private Color _fill = Colors.White;
        private Color _fill2 = Colors.White;
        private bool _gradient;
        private double _angle = 90;

        private GeometryGroup _geo;
        private double _w;
        private double _h;
        private bool _dirty = true;

        public NumberText()
        {
            IsHitTestVisible = false;
            HorizontalAlignment = HorizontalAlignment.Center;
        }

        // The text to draw. Cheap when nothing changed.
        public void SetText(string text, string hidden)
        {
            if (text == _text && hidden == _hidden) return;
            _text = text ?? "";
            _hidden = hidden ?? "";
            Relayout();
        }

        public void SetFont(FontFamily family, bool bold, bool italic, double size)
        {
            _family = family;
            _bold = bold;
            _italic = italic;
            _size = Math.Max(4, size);
            Relayout();
        }

        public void SetSpacing(double spacing, double lineHeightPercent, double outline, Color outlineColor)
        {
            _spacing = Math.Max(0, spacing);
            _lineHeight = Math.Max(0.3, lineHeightPercent / 100.0);
            _outline = Math.Max(0, outline);
            _outlineColor = outlineColor;
            Relayout();
        }

        // The colours only need a redraw, not a new layout.
        public void SetFill(Color fill, bool gradient, Color fill2, double angle)
        {
            if (fill == _fill && gradient == _gradient && fill2 == _fill2 && Math.Abs(angle - _angle) < 0.01) return;
            _fill = fill;
            _gradient = gradient;
            _fill2 = fill2;
            _angle = angle;
            InvalidateVisual();
        }

        private void Relayout()
        {
            _dirty = true;
            InvalidateMeasure();
            InvalidateVisual();
        }

        private FormattedText Make(string s)
        {
            var face = new Typeface(_family,
                _italic ? FontStyles.Italic : FontStyles.Normal,
                _bold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal);
            return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, _size, Brushes.White);
        }

        private void Rebuild()
        {
            _dirty = false;
            var group = new GeometryGroup();
            group.FillRule = FillRule.Nonzero;

            // every digit gets the width of the widest one, so the numbers do not jiggle
            double digitW = 0;
            for (char d = '0'; d <= '9'; d++)
            {
                double dw = Make(d.ToString()).WidthIncludingTrailingWhitespace;
                if (dw > digitW) digitW = dw;
            }

            double natural = Make("0").Height;
            double box = natural * _lineHeight;
            double pad = _outline;
            double y = pad + (box - natural) / 2;
            double x = pad;

            for (int i = 0; i < _text.Length; i++)
            {
                char c = _text[i];
                FormattedText ft = Make(c.ToString());
                double adv = ft.WidthIncludingTrailingWhitespace;
                double offset = 0;
                if (c >= '0' && c <= '9')
                {
                    offset = (digitW - adv) / 2;
                    adv = digitW;
                }
                if (c != ' ' && _hidden.IndexOf(c) < 0)
                {
                    Geometry g = ft.BuildGeometry(new Point(x + offset, y));
                    if (g != null && !g.IsEmpty()) group.Children.Add(g);
                }
                x += adv;
                if (i < _text.Length - 1) x += _spacing;
            }

            _geo = group;
            _w = x + pad;
            _h = box + pad * 2;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_dirty) Rebuild();
            return new Size(_w, _h);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_dirty) Rebuild();
            if (_geo == null || _geo.Children.Count == 0) return;

            if (_outline > 0.01)
            {
                var pen = new Pen(new SolidColorBrush(_outlineColor), _outline * 2);
                pen.LineJoin = PenLineJoin.Round;
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
                dc.DrawGeometry(null, pen, _geo);
            }

            Brush fill;
            if (_gradient) fill = new LinearGradientBrush(_fill, _fill2, _angle);
            else fill = new SolidColorBrush(_fill);
            dc.DrawGeometry(fill, null, _geo);
        }
    }
}
