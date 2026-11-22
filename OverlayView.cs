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
            _info.Margin = new Thickness(0, 4, 0, 0);
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
            _stage.Children.Add(_text);
            _stage.Children.Add(_flip);
            _stage.Children.Add(_seg);
            _stage.Children.Add(_analog);

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

            var content = new StackPanel();
            content.Children.Add(_label);
            content.Children.Add(_above);
            content.Children.Add(_row);
            content.Children.Add(_bar);
            content.Children.Add(_date);
            content.Children.Add(_info);
            content.Children.Add(_below);

            var root = new Grid();
            root.Children.Add(_imgLayer);
            root.Children.Add(content);
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
            text = text ?? "";
            if (_info.Text == text) return;
            _info.Text = text;
            _info.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateAbove()
        {
            string t = string.IsNullOrEmpty(_caption) ? (_s.TextAbove ?? "") : _caption;
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
                _bar.Margin = new Thickness(0, 8, 0, 0);
                _barTrack.Background = track;
                _barTrack.CornerRadius = new CornerRadius(thick / 2);
                _barFill.Background = _fg;
                _barFill.CornerRadius = new CornerRadius(thick / 2);
                ((ScaleTransform)_barFill.RenderTransform).ScaleX = _progress;
                _barFill.Visibility = _progress < 0.003 ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (show && ps == ProgressStyle.Ring)
            {
                _bar.Visibility = Visibility.Collapsed;
                _ring.Visibility = Visibility.Visible;
                double d = ColorUtil.Clamp(_s.FontSize * 0.95, 24, 260);
                _ring.Width = d;
                _ring.Height = d;
                _ring.Margin = new Thickness(14, 0, 0, 0);
                _ringTrack.Stroke = track;
                _ringTrack.StrokeThickness = thick;
                _ringArc.Stroke = _fg;
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

            _text.Visibility = (style == DigitStyle.Normal || style == DigitStyle.Terminal) ? Visibility.Visible : Visibility.Collapsed;
            _flip.Visibility = style == DigitStyle.Flip ? Visibility.Visible : Visibility.Collapsed;
            _seg.Visibility = style == DigitStyle.Led ? Visibility.Visible : Visibility.Collapsed;
            _analog.Visibility = style == DigitStyle.Analog ? Visibility.Visible : Visibility.Collapsed;

            if (style == DigitStyle.Flip)
            {
                _flip.Set(_raw, size, _fg, blink);
            }
            else if (style == DigitStyle.Led)
            {
                _seg.Set(_raw, _fg, size * 0.95, blink);
            }
            else if (style == DigitStyle.Analog)
            {
                Brush accent = new SolidColorBrush(ColorUtil.Parse(_s.RunningColor, Colors.LightGreen));
                _analog.Set(DateTime.Now, size * 2.6, _fg, accent, AppHost.Settings.ClockShowSeconds);
            }
            else
            {
                string shown = _raw;
                if (style == DigitStyle.Terminal) shown = "> " + shown + (HalfSecondOn() ? "_" : " ");
                string key = shown + (blink ? "|b" : "|n");
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
                            if (c == ':') r.Foreground = Brushes.Transparent;
                            _text.Inlines.Add(r);
                        }
                    }
                }
                _text.Foreground = _fg;
            }

            // date line (clock only)
            bool dateOn = Kind == OverlayMode.Clock && (_s.ShowDayName || _s.ShowDate);
            _date.Visibility = dateOn ? Visibility.Visible : Visibility.Collapsed;
            if (dateOn)
            {
                string fmt = _s.ShowDayName && _s.ShowDate ? "dddd, d MMM yyyy" : (_s.ShowDayName ? "dddd" : "d MMM yyyy");
                string d = DateTime.Now.ToString(fmt);
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
            if (Effective == DigitStyle.Terminal) family = new FontFamily("Consolas");
            _text.FontFamily = family;
            _text.FontSize = ColorUtil.Clamp(s.FontSize, 8, 300);
            _text.FontWeight = s.FontBold ? FontWeights.Bold : FontWeights.Normal;
            _text.FontStyle = s.FontItalic ? FontStyles.Italic : FontStyles.Normal;

            if (s.TextShadow)
            {
                var ts = new DropShadowEffect();
                ts.BlurRadius = 6;
                ts.ShadowDepth = 2;
                ts.Opacity = 0.8;
                ts.Color = Colors.Black;
                _text.Effect = ts;
            }
            else
            {
                _text.Effect = null;
            }

            // ---- label and extra text ----
            _label.Text = s.LabelText;
            _label.Visibility = s.ShowLabel ? Visibility.Visible : Visibility.Collapsed;
            _label.FontSize = ColorUtil.Clamp(s.LabelSize, 6, 80);
            _label.FontFamily = new FontFamily("Segoe UI");
            _label.FontWeight = FontWeights.SemiBold;
            _label.Foreground = new SolidColorBrush(ColorUtil.Parse(s.LabelColor, Colors.Gray));

            Brush extraBrush = new SolidColorBrush(ColorUtil.Parse(s.ExtraTextColor, Colors.LightGray));
            double extraSize = ColorUtil.Clamp(s.ExtraTextSize, 6, 120);
            foreach (TextBlock tb in new TextBlock[] { _above, _below, _date, _info })
            {
                tb.FontFamily = new FontFamily("Segoe UI");
                tb.FontSize = extraSize;
                tb.Foreground = extraBrush;
            }
            UpdateAbove();
            _below.Text = s.TextBelow ?? "";
            _below.Visibility = string.IsNullOrEmpty(s.TextBelow) ? Visibility.Collapsed : Visibility.Visible;

            // ---- icon ----
            _iconGlyph.Visibility = Visibility.Collapsed;
            _iconImage.Visibility = Visibility.Collapsed;
            double iconSize = ColorUtil.Clamp(s.IconSize, 8, 200);
            Thickness iconMargin = s.IconOnRight ? new Thickness(12, 0, 0, 0) : new Thickness(0, 0, 12, 0);
            if (s.Icon == OverlayIcon.Custom)
            {
                BitmapImage bi = LoadImage(s.IconPath, 256);
                if (bi != null)
                {
                    _iconImage.Source = bi;
                    _iconImage.Width = iconSize;
                    _iconImage.Height = iconSize;
                    _iconImage.Margin = iconMargin;
                    _iconImage.Visibility = Visibility.Visible;
                }
            }
            else if (s.Icon != OverlayIcon.None)
            {
                _iconGlyph.Text = GlyphFor(s.Icon);
                _iconGlyph.FontSize = iconSize * 0.85;
                _iconGlyph.Foreground = new SolidColorBrush(ColorUtil.Parse(s.TextColor, Colors.White));
                _iconGlyph.Margin = iconMargin;
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
            double k = ColorUtil.Clamp(s.Scale, 25, 400) / 100.0;
            LayoutTransform = new ScaleTransform(k, k);
            Margin = new Thickness(OuterMargin);

            if (_moveOutline.Visibility == Visibility.Visible) SetMoveOutline(true);
            Refresh();
            UpdateProgress();
        }
    }
}
