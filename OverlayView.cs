// The look of the overlay (text, background, border, shadow...).
// Used by the real overlay window AND by the live preview in the settings window.
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace BetterClock
{
    public enum OverlayRole
    {
        Normal,
        Running,
        Paused,
        Finished,
        Warning,   // countdown running low
        Danger,    // countdown almost out
        Target     // timer reached its target
    }

    public class OverlayView : Border
    {
        private readonly TextBlock _label = new TextBlock();
        private readonly TextBlock _above = new TextBlock();
        private readonly TextBlock _text = new TextBlock();
        private readonly TextBlock _date = new TextBlock();
        private readonly TextBlock _below = new TextBlock();
        private readonly TextBlock _info = new TextBlock();   // extra lines (world clocks, dates, session)
        private readonly TextBlock _iconGlyph = new TextBlock();
        private readonly Image _iconImage = new Image();
        private readonly FlipDisplay _flip = new FlipDisplay();
        private readonly SegmentDisplay _seg = new SegmentDisplay();
        private readonly AnalogFace _analog = new AnalogFace();
        private readonly Border _imgLayer = new Border();
        private readonly StackPanel _row = new StackPanel();
        private readonly StackPanel _content = new StackPanel();
        private readonly NumberText _fx = new NumberText();      // numbers with outline / gradient / spacing
        private readonly Grid _numHost = new Grid();             // holds _text and _fx (the shadow sits on it)
        private readonly Grid _g2 = new Grid();                  // extra glow layers (glow strength 2 and 3)
        private readonly Grid _g3 = new Grid();
        private FontFamily _custom;                              // the font file, when one is chosen

        private ModeStyle _s = new ModeStyle();
        private OverlayRole _role = OverlayRole.Normal;
        private string _raw = "00:00";
        private string _shown = null;
        private Brush _fg = Brushes.White;
        private readonly Grid _stage = new Grid();

        // progress ring / bar, caption, glow pulse
        private readonly Grid _ring = new Grid();
        private readonly System.Windows.Shapes.Ellipse _ringTrack = new System.Windows.Shapes.Ellipse();
        private readonly System.Windows.Shapes.Path _ringArc = new System.Windows.Shapes.Path();
        private readonly Grid _bar = new Grid();
        private readonly Border _barTrack = new Border();
        private readonly Border _barFill = new Border();
        private double _progress = -1;       // -1 = none, otherwise 0..1 (how much of the ring / bar is filled)
        private string _caption = null;      // replaces "Text above" while set (chain step names)
        private bool _pulse;

        // Which overlay this is (the analog face only makes sense for the clock).
        public OverlayMode Kind = OverlayMode.Clock;

        private static readonly Dictionary<string, BitmapImage> ImageCache = new Dictionary<string, BitmapImage>();

        public OverlayView()
        {
            _text.Text = "00:00";
            _text.HorizontalAlignment = HorizontalAlignment.Center;
            Typography.SetNumeralAlignment(_text, FontNumeralAlignment.Tabular);

            _label.HorizontalAlignment = HorizontalAlignment.Center;
            _label.Visibility = Visibility.Collapsed;
            _above.HorizontalAlignment = HorizontalAlignment.Center;
            _below.HorizontalAlignment = HorizontalAlignment.Center;
            _date.HorizontalAlignment = HorizontalAlignment.Center;
            _info.HorizontalAlignment = HorizontalAlignment.Center;
            _info.TextAlignment = TextAlignment.Center;
            _info.Visibility = Visibility.Collapsed;
            _above.Visibility = Visibility.Collapsed;
            _below.Visibility = Visibility.Collapsed;
            _date.Visibility = Visibility.Collapsed;

            _iconGlyph.FontFamily = new FontFamily("Segoe UI Emoji, Segoe UI Symbol");
            _iconGlyph.VerticalAlignment = VerticalAlignment.Center;
            _iconImage.VerticalAlignment = VerticalAlignment.Center;
            _iconImage.Stretch = Stretch.Uniform;

            // the number displays share one spot; only one is visible
            _stage.VerticalAlignment = VerticalAlignment.Center;
            _fx.Visibility = Visibility.Collapsed;
            _numHost.Children.Add(_text);
            _numHost.Children.Add(_fx);
            _g3.Children.Add(_numHost);
            _g3.Children.Add(_flip);
            _g3.Children.Add(_seg);
            _g3.Children.Add(_analog);
            _g2.Children.Add(_g3);
            _stage.Children.Add(_g2);

            _ringTrack.Fill = null;
            _ringArc.Fill = null;
            _ringArc.StrokeStartLineCap = PenLineCap.Round;
            _ringArc.StrokeEndLineCap = PenLineCap.Round;
            _ring.Children.Add(_ringTrack);
            _ring.Children.Add(_ringArc);
            _ring.VerticalAlignment = VerticalAlignment.Center;
            _ring.Visibility = Visibility.Collapsed;

            _barFill.HorizontalAlignment = HorizontalAlignment.Stretch;
            _barFill.RenderTransformOrigin = new Point(0, 0.5);
            _barFill.RenderTransform = new ScaleTransform(1, 1);
            _bar.Children.Add(_barTrack);
            _bar.Children.Add(_barFill);
            _bar.Visibility = Visibility.Collapsed;

            _row.Orientation = Orientation.Horizontal;
            _row.HorizontalAlignment = HorizontalAlignment.Center;
            _row.Children.Add(_iconGlyph);
            _row.Children.Add(_iconImage);
            _row.Children.Add(_stage);
            _row.Children.Add(_ring);

            _content.VerticalAlignment = VerticalAlignment.Center;
            var root = new Grid();
            root.Children.Add(_imgLayer);
            root.Children.Add(_content);
            _moveOutline.IsHitTestVisible = false;
            _moveOutline.Visibility = Visibility.Collapsed;
            root.Children.Add(_moveOutline);
            Child = root;
        }

        // How far the window must extend beyond the pill so the glow isn't cut off (in DIPs).
        public double OuterMargin
        {
            get
            {
                int glow = _s.ShadowEnabled ? ColorUtil.Clamp(_s.ShadowBlur, 0, 60) : 0;
                int numberGlow = ColorUtil.Clamp(_s.NumberGlowSize, 0, 60);
                if (numberGlow > glow) glow = numberGlow;
                if (Kind == OverlayMode.Countdown && _s.PulseSeconds > 0 && glow < 40) glow = 40;   // room for the pulse
                return 12 + glow;
            }
        }

        private DigitStyle Effective
        {
            get
            {
                if (_s.DigitStyle == DigitStyle.Analog && Kind != OverlayMode.Clock) return DigitStyle.Normal;
                return _s.DigitStyle;
            }
        }

        public void SetText(string text)
        {
            _raw = text;
            Refresh();
        }

        public void SetRole(OverlayRole role)
        {
            if (role == _role) return;
            _role = role;
            _fg = new SolidColorBrush(RoleColor());
            Refresh();
            UpdateProgress();
            ApplyNumberGlow();
        }

        // 0..1 = fill the ring / bar that far, -1 = hide it.
        public void SetProgress(double fraction)
        {
            if (fraction > 1) fraction = 1;
            if (fraction < 0 && fraction != -1) fraction = 0;
            if (Math.Abs(fraction - _progress) < 0.002) return;
            _progress = fraction;
            UpdateProgress();
        }

        // Replaces "Text above" (null = go back to the normal text).
        public void SetCaption(string text)
        {
            if (text == _caption) return;
            _caption = text;
            UpdateAbove();
        }

        // Pulsing glow (last seconds of a countdown).
        public void SetPulse(bool on)
        {
            if (on == _pulse) return;
            _pulse = on;
            ApplyEffect();
        }

        // Extra lines under the numbers (one per line). Empty hides them.
        public void SetInfo(string text)
        {
            _infoText = text ?? "";
            ApplyInfoText();
        }

        private string _infoText = "";

        private void ApplyInfoText()
        {
            string t = _s.ShowInfo ? Cased(_infoText) : "";
            if (_info.Text != t) _info.Text = t;
            _info.Visibility = t.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateAbove()
        {
            string t = string.IsNullOrEmpty(_caption) ? (_s.TextAbove ?? "") : _caption;
            if (!_s.ShowAbove) t = "";
            t = Cased(t);
            if (_above.Text != t) _above.Text = t;
            _above.Visibility = t.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateProgress()
        {
            ProgressStyle ps = _s.ProgressStyle;
            bool show = _progress >= 0 && ps != ProgressStyle.None && Kind != OverlayMode.Clock;
            double thick = ColorUtil.Clamp(_s.ProgressThickness, 2, 30);
            Brush track = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));

            if (show && ps == ProgressStyle.Bar)
            {
                _ring.Visibility = Visibility.Collapsed;
                _bar.Visibility = Visibility.Visible;
                _bar.Height = thick;
                _bar.Opacity = ColorUtil.Clamp(_s.ProgressOpacity, 0, 100) / 100.0;
                _barTrack.Background = track;
                _barTrack.CornerRadius = new CornerRadius(thick / 2);
                _barFill.Background = ProgressBrush();
                _barFill.CornerRadius = new CornerRadius(thick / 2);
                ((ScaleTransform)_barFill.RenderTransform).ScaleX = _progress;
                _barFill.Visibility = _progress < 0.003 ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (show && ps == ProgressStyle.Ring)
            {
                _bar.Visibility = Visibility.Collapsed;
                _ring.Visibility = Visibility.Visible;
                double d = ColorUtil.Clamp(_s.FontSize * 0.95 * ColorUtil.Clamp(_s.ProgressSize, 30, 400) / 100.0, 20, 500);
                _ring.Width = d;
                _ring.Height = d;
                _ring.Opacity = ColorUtil.Clamp(_s.ProgressOpacity, 0, 100) / 100.0;
                _ring.Margin = new Thickness(14, 0, 0, 0);
                _ringTrack.Stroke = track;
                _ringTrack.StrokeThickness = thick;
                _ringArc.Stroke = ProgressBrush();
                _ringArc.StrokeThickness = thick;

                if (_progress < 0.003)
                {
                    _ringArc.Data = null;
                }
                else
                {
                    double c = d / 2;
                    double r = (d - thick) / 2;
                    double a = Math.Min(_progress, 0.9999) * 2 * Math.PI;
                    var fig = new PathFigure();
                    fig.StartPoint = new Point(c, c - r);
                    fig.IsClosed = false;
                    fig.Segments.Add(new ArcSegment(new Point(c + r * Math.Sin(a), c - r * Math.Cos(a)),
                        new Size(r, r), 0, a > Math.PI, SweepDirection.Clockwise, true));
                    var geo = new PathGeometry();
                    geo.Figures.Add(fig);
                    _ringArc.Data = geo;
                }
            }
            else
            {
                _bar.Visibility = Visibility.Collapsed;
                _ring.Visibility = Visibility.Collapsed;
            }
        }

        // The colour of the ring / bar: its own colour if you chose one, otherwise the state colour.
        private Brush ProgressBrush()
        {
            if (string.IsNullOrEmpty(_s.ProgressColor)) return _fg;
            return new SolidColorBrush(ColorUtil.Parse(_s.ProgressColor, Colors.White));
        }

        // The glow / shadow around the pill. While pulsing it breathes in the pulse colour.
        private void ApplyEffect()
        {
            if (_pulse && Kind == OverlayMode.Countdown && _s.PulseSeconds > 0)
            {
                var sh = new DropShadowEffect();
                sh.ShadowDepth = 0;
                sh.Opacity = 1;
                sh.Color = ColorUtil.Parse(_s.PulseColor, Colors.Red);
                sh.BlurRadius = 8;
                Effect = sh;
                var a = new DoubleAnimation(8, 40, TimeSpan.FromMilliseconds(450));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                sh.BeginAnimation(DropShadowEffect.BlurRadiusProperty, a);
            }
            else if (_s.ShadowEnabled)
            {
                var sh = new DropShadowEffect();
                sh.BlurRadius = ColorUtil.Clamp(_s.ShadowBlur, 0, 60);
                sh.ShadowDepth = 0;
                sh.Opacity = 0.7;
                sh.Color = Colors.Black;
                Effect = sh;
            }
            else
            {
                Effect = null;
            }
        }

        // A dashed outline so you can see what you are dragging.
        private readonly Border _moveOutline = new Border();

        public void SetMoveOutline(bool on)
        {
            if (on)
            {
                _moveOutline.BorderBrush = Brushes.DeepSkyBlue;
                _moveOutline.Background = new SolidColorBrush(Color.FromArgb(46, 0, 150, 255));
                _moveOutline.IsHitTestVisible = true;
                _moveOutline.BorderThickness = new Thickness(2);
                _moveOutline.CornerRadius = CornerRadius;
                _moveOutline.Margin = new Thickness(-Padding.Left, -Padding.Top, -Padding.Right, -Padding.Bottom);
                _moveOutline.Visibility = Visibility.Visible;
            }
            else
            {
                _moveOutline.Visibility = Visibility.Collapsed;
                _moveOutline.IsHitTestVisible = false;
            }
        }

        // Pops the pill in with the chosen show animation (Fade is handled by the window opacity).
        public void PlayShow(ShowAnimation anim, OverlayPosition pos)
        {
            RenderTransformOrigin = new Point(0.5, 0.5);
            if (anim == ShowAnimation.Slide)
            {
                double dy = (int)pos / 3 == 2 ? 24 : -24;
                var tt = new TranslateTransform(0, dy);
                RenderTransform = tt;
                var a = new DoubleAnimation(dy, 0, TimeSpan.FromMilliseconds(260));
                a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                tt.BeginAnimation(TranslateTransform.YProperty, a);
            }
            else if (anim == ShowAnimation.Pop)
            {
                var st = new ScaleTransform(0.8, 0.8);
                RenderTransform = st;
                var a = new DoubleAnimation(0.8, 1, TimeSpan.FromMilliseconds(280));
                a.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.7 };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else
            {
                RenderTransform = Transform.Identity;
            }
        }

        private Color RoleColor()
        {
            switch (_role)
            {
                case OverlayRole.Running: return ColorUtil.Parse(_s.RunningColor, Colors.LightGreen);
                case OverlayRole.Paused: return ColorUtil.Parse(_s.PausedColor, Colors.Orange);
                case OverlayRole.Finished: return ColorUtil.Parse(_s.FinishedColor, Colors.Red);
                case OverlayRole.Warning: return ColorUtil.Parse(_s.WarnColor, Colors.Orange);
                case OverlayRole.Danger: return ColorUtil.Parse(_s.DangerColor, Colors.Red);
                case OverlayRole.Target: return ColorUtil.Parse(_s.TargetColor, Colors.LimeGreen);
                default: return ColorUtil.Parse(_s.TextColor, Colors.White);
            }
        }

        // ------------------------------------------------------------ drawing the numbers

        private static bool HalfSecondOn()
        {
            return (DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond * 500)) % 2 == 0;
        }

        // Draws the current text in the chosen style. Cheap when nothing changed.
        private void Refresh()
        {
            DigitStyle style = Effective;
            bool blink = _s.BlinkColon && !HalfSecondOn();   // true = colon hidden right now
            double size = ColorUtil.Clamp(_s.FontSize, 8, 300);
            string digits = FormatDigits(_raw);
            char sep = SepChar;
            bool textStyle = style == DigitStyle.Normal || style == DigitStyle.Terminal;
            bool fx = textStyle && UseFx;

            _text.Visibility = (textStyle && !fx) ? Visibility.Visible : Visibility.Collapsed;
            _fx.Visibility = fx ? Visibility.Visible : Visibility.Collapsed;
            _flip.Visibility = style == DigitStyle.Flip ? Visibility.Visible : Visibility.Collapsed;
            _seg.Visibility = style == DigitStyle.Led ? Visibility.Visible : Visibility.Collapsed;
            _analog.Visibility = style == DigitStyle.Analog ? Visibility.Visible : Visibility.Collapsed;

            if (style == DigitStyle.Flip)
            {
                _flip.SepChar = sep;
                _flip.Set(digits, size, _fg, blink);
            }
            else if (style == DigitStyle.Led)
            {
                _seg.SepChar = sep;
                _seg.Set(digits, _fg, size * 0.95, blink);
            }
            else if (style == DigitStyle.Analog)
            {
                Brush accent = new SolidColorBrush(ColorUtil.Parse(_s.RunningColor, Colors.LightGreen));
                _analog.Set(DateTime.Now, size * 2.6, _fg, accent, AppHost.Settings.ClockShowSeconds);
            }
            else
            {
                string shown = digits;
                if (style == DigitStyle.Terminal) shown = "> " + shown + (HalfSecondOn() ? "_" : " ");
                if (fx)
                {
                    _fx.SetText(shown, (blink && sep != '\0') ? sep.ToString() : "");
                    _fx.SetFill(RoleColor(), _s.NumberGradient, ColorUtil.Parse(_s.NumberColor2, Colors.HotPink), _s.NumberGradientAngle);
                }
                else
                {
                    string key = shown + (blink ? "|b" : "|n") + sep;
                    if (key != _shown)
                    {
                        _shown = key;
                        if (!blink)
                        {
                            _text.Inlines.Clear();
                            _text.Text = shown;
                        }
                        else
                        {
                            _text.Text = "";
                            _text.Inlines.Clear();
                            foreach (char c in shown)
                            {
                                var r = new Run(c.ToString());
                                if (c == sep) r.Foreground = Brushes.Transparent;
                                _text.Inlines.Add(r);
                            }
                        }
                    }
                    _text.Foreground = _fg;
                }
            }

            // date line (clock only)
            bool dateOn = Kind == OverlayMode.Clock && (_s.ShowDayName || _s.ShowDate);
            _date.Visibility = dateOn ? Visibility.Visible : Visibility.Collapsed;
            if (dateOn)
            {
                string fmt = _s.ShowDayName && _s.ShowDate ? "dddd, d MMM yyyy" : (_s.ShowDayName ? "dddd" : "d MMM yyyy");
                string d = Cased(DateTime.Now.ToString(fmt));
                if (_date.Text != d) _date.Text = d;
            }
        }

        // ------------------------------------------------------------ pictures

        private static BitmapImage LoadImage(string path, int decodeWidth)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                string key = path + "|" + File.GetLastWriteTimeUtc(path).Ticks + "|" + decodeWidth;
                BitmapImage bi;
                if (ImageCache.TryGetValue(key, out bi)) return bi;

                bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(path);
                if (decodeWidth > 0) bi.DecodePixelWidth = decodeWidth;
                bi.EndInit();
                bi.Freeze();
                ImageCache[key] = bi;
                return bi;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GlyphFor(OverlayIcon icon)
        {
            switch (icon)
            {
                case OverlayIcon.Clock: return "\u23F0";
                case OverlayIcon.Hourglass: return "\u231B";
                case OverlayIcon.Stopwatch: return "\u23F1";
                case OverlayIcon.Bell: return "\U0001F514";
                case OverlayIcon.Star: return "\u2B50";
                case OverlayIcon.Bolt: return "\u26A1";
                case OverlayIcon.Flag: return "\U0001F3C1";
                case OverlayIcon.Trophy: return "\U0001F3C6";
                case OverlayIcon.Skull: return "\U0001F480";
                case OverlayIcon.Heart: return "\u2764";
                case OverlayIcon.Gamepad: return "\U0001F3AE";
                default: return "";
            }
        }

        // ------------------------------------------------------------ helpers for the parts

        private static FontFamily SafeFamily(string name, string fallback)
        {
            try { return new FontFamily(string.IsNullOrEmpty(name) ? fallback : name); }
            catch (Exception) { return new FontFamily(fallback); }
        }

        // A small move (and optional turn) for a part. Null when there is nothing to do.
        private static Transform Nudge(int x, int y, int rotation)
        {
            if (x == 0 && y == 0 && rotation == 0) return null;
            var g = new TransformGroup();
            if (rotation != 0) g.Children.Add(new RotateTransform(rotation));
            g.Children.Add(new TranslateTransform(x, y));
            return g;
        }

        // Font, size, colour, boldness, opacity and position for one of the small text lines.
        private void TextPart(TextBlock tb, string font, int size, string color, bool bold, int opacity, int offX, int offY)
        {
            tb.FontFamily = PartFamily(font);
            tb.FontSize = size > 0 ? ColorUtil.Clamp(size, 6, 160) : ColorUtil.Clamp(_s.ExtraTextSize, 6, 120);
            string c = string.IsNullOrEmpty(color) ? _s.ExtraTextColor : color;
            tb.Foreground = new SolidColorBrush(ColorUtil.Parse(c, Colors.LightGray));
            tb.FontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
            tb.Opacity = ColorUtil.Clamp(opacity, 0, 100) / 100.0;
            tb.RenderTransform = Nudge(offX, offY, 0);
        }

        // Puts the parts in the order you chose, with the space you chose between them.
        private void ApplyOrder()
        {
            var map = new Dictionary<string, UIElement>();
            map["label"] = _label;
            map["above"] = _above;
            map["main"] = _row;
            map["bar"] = _bar;
            map["date"] = _date;
            map["info"] = _info;
            map["below"] = _below;
            string[] fallback = { "label", "above", "main", "bar", "date", "info", "below" };

            var order = new List<string>();
            foreach (string raw in (_s.PartOrder ?? "").Split(','))
            {
                string key = raw.Trim().ToLowerInvariant();
                if (map.ContainsKey(key) && !order.Contains(key)) order.Add(key);
            }
            foreach (string key in fallback) if (!order.Contains(key)) order.Add(key);

            _content.Children.Clear();
            double space = ColorUtil.Clamp(_s.PartSpacing, 0, 60);
            for (int i = 0; i < order.Count; i++)
            {
                UIElement part = map[order[i]];
                double top = i == 0 ? 0 : space;
                if (order[i] == "bar") top += 4;
                ((FrameworkElement)part).Margin = new Thickness(0, top, 0, 0);
                _content.Children.Add(part);
            }
        }

        // ------------------------------------------------------------ text effects (1.4.5)

        // True when the numbers need the effect drawing (outline, gradient, spacing, line height).
        private bool UseFx
        {
            get { return _s.OutlineSize > 0 || _s.NumberGradient || _s.LetterSpacing > 0 || _s.LineHeight != 100; }
        }

        // The character that separates the groups of numbers (nothing = '\0').
        private char SepChar
        {
            get
            {
                switch (_s.Separator)
                {
                    case DigitSeparator.Dot: return '.';
                    case DigitSeparator.Dash: return '-';
                    case DigitSeparator.Space: return ' ';
                    case DigitSeparator.None: return '\0';
                    default: return ':';
                }
            }
        }

        // Applies the chosen separator and the leading-zero choice to the time text.
        private string FormatDigits(string raw)
        {
            string t = raw ?? "";
            if (!_s.LeadingZero && t.Length > 1 && t[0] == '0' && char.IsDigit(t[1])) t = t.Substring(1);
            switch (_s.Separator)
            {
                case DigitSeparator.Dot: t = t.Replace(':', '.'); break;
                case DigitSeparator.Dash: t = t.Replace(':', '-'); break;
                case DigitSeparator.Space: t = t.Replace(':', ' '); break;
                case DigitSeparator.None: t = t.Replace(":", ""); break;
            }
            return t;
        }

        // UPPERCASE / lowercase for the small texts.
        private string Cased(string t)
        {
            if (t == null) return "";
            if (_s.TextCase == LetterCase.UPPERCASE) return t.ToUpperInvariant();
            if (_s.TextCase == LetterCase.lowercase) return t.ToLowerInvariant();
            return t;
        }

        private FontFamily PartFamily(string name)
        {
            if (string.IsNullOrEmpty(name) && _s.FontFileForAll && _custom != null) return _custom;
            return SafeFamily(name, "Segoe UI");
        }

        // The glow around the numbers. Strength 2 and 3 stack more glow layers.
        private void ApplyNumberGlow()
        {
            int size = ColorUtil.Clamp(_s.NumberGlowSize, 0, 60);
            int strength = ColorUtil.Clamp(_s.NumberGlowStrength, 1, 3);
            _stage.Effect = size > 0 ? MakeGlow(size) : null;
            _g2.Effect = (size > 0 && strength >= 2) ? MakeGlow(size) : null;
            _g3.Effect = (size > 0 && strength >= 3) ? MakeGlow(size) : null;
        }

        private DropShadowEffect MakeGlow(int size)
        {
            var g = new DropShadowEffect();
            g.ShadowDepth = 0;
            g.Opacity = 1;
            g.BlurRadius = size;
            g.Color = string.IsNullOrEmpty(_s.NumberGlowColor) ? RoleColor() : ColorUtil.Parse(_s.NumberGlowColor, Colors.White);
            return g;
        }

        // Re-reads every appearance setting.
        public void Apply(ModeStyle s)
        {
            _s = s;
            _shown = null;
            _fg = new SolidColorBrush(RoleColor());

            // ---- text ----
            FontFamily family;
            try { family = new FontFamily(s.FontFamily); }
            catch (Exception) { family = new FontFamily("Segoe UI Semibold"); }
            _custom = FontFiles.Load(s.FontFile);
            if (_custom != null) family = _custom;
            if (Effective == DigitStyle.Terminal) family = new FontFamily("Consolas");
            double fontSize = ColorUtil.Clamp(s.FontSize, 8, 300);
            _text.FontFamily = family;
            _text.FontSize = fontSize;
            _text.FontWeight = s.FontBold ? FontWeights.Bold : FontWeights.Normal;
            _text.FontStyle = s.FontItalic ? FontStyles.Italic : FontStyles.Normal;
            _text.Effect = null;

            _fx.SetFont(family, s.FontBold, s.FontItalic, fontSize);
            _fx.SetSpacing(ColorUtil.Clamp(s.LetterSpacing, 0, 60), ColorUtil.Clamp(s.LineHeight, 30, 300),
                ColorUtil.Clamp(s.OutlineSize, 0, 20), ColorUtil.Parse(s.OutlineColor, Colors.Black));

            // shadow behind the numbers
            if (s.TextShadow)
            {
                var ts = new DropShadowEffect();
                ts.BlurRadius = ColorUtil.Clamp(s.TextShadowBlur, 0, 40);
                ts.ShadowDepth = ColorUtil.Clamp(s.TextShadowDistance, 0, 40);
                ts.Direction = ColorUtil.Clamp(s.TextShadowAngle, 0, 360);
                ts.Opacity = ColorUtil.Clamp(s.TextShadowOpacity, 0, 100) / 100.0;
                ts.Color = ColorUtil.Parse(s.TextShadowColor, Colors.Black);
                _numHost.Effect = ts;
            }
            else
            {
                _numHost.Effect = null;
            }
            ApplyNumberGlow();

            // ---- numbers (own settings) ----
            _stage.Visibility = s.ShowNumbers ? Visibility.Visible : Visibility.Collapsed;
            _stage.Opacity = ColorUtil.Clamp(s.NumbersOpacity, 0, 100) / 100.0;
            _stage.RenderTransform = Nudge(s.NumbersOffsetX, s.NumbersOffsetY, 0);

            // ---- label ----
            _label.Text = Cased(s.LabelText);
            _label.Visibility = s.ShowLabel ? Visibility.Visible : Visibility.Collapsed;
            _label.FontSize = ColorUtil.Clamp(s.LabelSize, 6, 80);
            _label.FontFamily = PartFamily(s.LabelFont);
            _label.FontWeight = s.LabelBold ? FontWeights.SemiBold : FontWeights.Normal;
            _label.Foreground = new SolidColorBrush(ColorUtil.Parse(s.LabelColor, Colors.Gray));
            _label.Opacity = ColorUtil.Clamp(s.LabelOpacity, 0, 100) / 100.0;
            _label.RenderTransform = Nudge(s.LabelOffsetX, s.LabelOffsetY, 0);

            // ---- extra text: above, below, date and extra lines each have their own look ----
            TextPart(_above, s.AboveFont, s.AboveSize, s.AboveColor, s.AboveBold, s.AboveOpacity, s.AboveOffsetX, s.AboveOffsetY);
            TextPart(_below, s.BelowFont, s.BelowSize, s.BelowColor, s.BelowBold, s.BelowOpacity, s.BelowOffsetX, s.BelowOffsetY);
            TextPart(_date, s.DateFont, s.DateSize, s.DateColor, s.DateBold, s.DateOpacity, s.DateOffsetX, s.DateOffsetY);
            TextPart(_info, s.InfoFont, s.InfoSize, s.InfoColor, s.InfoBold, s.InfoOpacity, s.InfoOffsetX, s.InfoOffsetY);
            UpdateAbove();
            _below.Text = Cased(s.TextBelow ?? "");
            _below.Visibility = (!s.ShowBelow || string.IsNullOrEmpty(s.TextBelow)) ? Visibility.Collapsed : Visibility.Visible;
            ApplyInfoText();

            // ---- icon ----
            _iconGlyph.Visibility = Visibility.Collapsed;
            _iconImage.Visibility = Visibility.Collapsed;
            double iconSize = ColorUtil.Clamp(s.IconSize, 8, 200);
            Thickness iconMargin = s.IconOnRight ? new Thickness(12, 0, 0, 0) : new Thickness(0, 0, 12, 0);
            double iconOpacity = ColorUtil.Clamp(s.IconOpacity, 0, 100) / 100.0;
            if (s.ShowIcon && s.Icon == OverlayIcon.Custom)
            {
                BitmapImage bi = LoadImage(s.IconPath, 256);
                if (bi != null)
                {
                    _iconImage.Source = bi;
                    _iconImage.Width = iconSize;
                    _iconImage.Height = iconSize;
                    _iconImage.Margin = iconMargin;
                    _iconImage.Opacity = iconOpacity;
                    _iconImage.RenderTransformOrigin = new Point(0.5, 0.5);
                    _iconImage.RenderTransform = Nudge(s.IconOffsetX, s.IconOffsetY, s.IconRotation);
                    _iconImage.Visibility = Visibility.Visible;
                }
            }
            else if (s.ShowIcon && s.Icon != OverlayIcon.None)
            {
                _iconGlyph.Text = GlyphFor(s.Icon);
                _iconGlyph.FontSize = iconSize * 0.85;
                string iconColor = string.IsNullOrEmpty(s.IconColor) ? s.TextColor : s.IconColor;
                _iconGlyph.Foreground = new SolidColorBrush(ColorUtil.Parse(iconColor, Colors.White));
                _iconGlyph.Margin = iconMargin;
                _iconGlyph.Opacity = iconOpacity;
                _iconGlyph.RenderTransformOrigin = new Point(0.5, 0.5);
                _iconGlyph.RenderTransform = Nudge(s.IconOffsetX, s.IconOffsetY, s.IconRotation);
                _iconGlyph.Visibility = Visibility.Visible;
            }
            _row.Children.Clear();
            if (s.IconOnRight)
            {
                _row.Children.Add(_stage);
                _row.Children.Add(_iconGlyph);
                _row.Children.Add(_iconImage);
                _row.Children.Add(_ring);
            }
            else
            {
                _row.Children.Add(_iconGlyph);
                _row.Children.Add(_iconImage);
                _row.Children.Add(_stage);
                _row.Children.Add(_ring);
            }

            ApplyOrder();

            // ---- background ----
            Color bg1 = ColorUtil.WithAlpha(ColorUtil.Parse(s.BackgroundColor, Color.FromRgb(0x10, 0x10, 0x18)), s.BackgroundOpacity);
            Color bg2 = ColorUtil.WithAlpha(ColorUtil.Parse(s.BackgroundColor2, Color.FromRgb(0x2A, 0x16, 0x50)), s.BackgroundOpacity);
            if (s.BackgroundStyle == BackgroundStyle.None)
            {
                Background = Brushes.Transparent;
            }
            else if (s.BackgroundStyle == BackgroundStyle.Gradient)
            {
                Background = new LinearGradientBrush(bg1, bg2, s.GradientAngle);
            }
            else
            {
                Background = new SolidColorBrush(bg1);
            }

            // ---- shape ----
            double rad = ColorUtil.Clamp(s.CornerRadius, 0, 200);
            double padH = ColorUtil.Clamp(s.PaddingH, 0, 200);
            double padV = ColorUtil.Clamp(s.PaddingV, 0, 200);
            double bt = ColorUtil.Clamp(s.BorderThickness, 0, 20);
            CornerRadius = new CornerRadius(rad);
            Padding = new Thickness(padH, padV, padH, padV);
            BorderThickness = new Thickness(bt);
            BorderBrush = new SolidColorBrush(ColorUtil.Parse(s.BorderColor, Colors.White));

            // ---- picture behind the numbers ----
            BitmapImage back = LoadImage(s.BackgroundImagePath, 900);
            if (back != null)
            {
                var ib = new ImageBrush(back);
                ib.Opacity = ColorUtil.Clamp(s.BackgroundImageOpacity, 0, 100) / 100.0;
                ib.Stretch = s.BackgroundImageFit == ImageFit.Fit ? Stretch.Uniform
                           : (s.BackgroundImageFit == ImageFit.Stretch ? Stretch.Fill : Stretch.UniformToFill);
                _imgLayer.Background = ib;
                _imgLayer.Margin = new Thickness(-padH, -padV, -padH, -padV);
                double inner = Math.Max(0, rad - bt);
                _imgLayer.CornerRadius = new CornerRadius(inner);
                _imgLayer.Visibility = Visibility.Visible;
            }
            else
            {
                _imgLayer.Background = null;
                _imgLayer.Visibility = Visibility.Collapsed;
            }

            // ---- shadow / glow around the pill ----
            ApplyEffect();

            // ---- size ----
            double k = ColorUtil.Clamp(s.Scale, 25, 500) / 100.0;
            LayoutTransform = new ScaleTransform(k, k);
            Margin = new Thickness(OuterMargin);

            if (_moveOutline.Visibility == Visibility.Visible) SetMoveOutline(true);
            Refresh();
            UpdateProgress();
        }
    }
}
