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
        Target,    // timer reached its target
        Overtime   // countdown counting up after zero
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
        private readonly ShapeLayer _shape = new ShapeLayer();        // custom shapes and border styles
        private readonly ShapeLayer _picLayer = new ShapeLayer();     // picture behind the numbers
        private readonly ShapeLayer _tintLayer = new ShapeLayer();    // colour over the picture
        private readonly ShapeLayer _texLayer = new ShapeLayer();     // noise / stripes / dots / grid
        private readonly ShapeLayer _innerLayer = new ShapeLayer();   // inner glow
        private readonly ShapeLayer _glassLayer = new ShapeLayer();   // glass shine
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
        private readonly SolidColorBrush _fgBrush = new SolidColorBrush(Colors.White);   // lives for ever, so its colour can fade
        private Brush _fg = Brushes.White;
        private ModeStyle _base = new ModeStyle();    // the settings as chosen (before any state look)
        private string _sig;                          // what was drawn last (to skip needless redraws)
        private string _lastDigits;
        public bool ForceNight;                    // the settings preview can show the night look
        private int _nightMinute = -1;
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
            root.Children.Add(_shape);
            root.Children.Add(_picLayer);
            root.Children.Add(_tintLayer);
            root.Children.Add(_texLayer);
            root.Children.Add(_innerLayer);
            root.Children.Add(_glassLayer);
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
                if (_s.ShadowEnabled) glow += Math.Max(Math.Abs(ColorUtil.Clamp(_s.ShadowOffsetX, -60, 60)), Math.Abs(ColorUtil.Clamp(_s.ShadowOffsetY, -60, 60)));
                if (_s.Breathe == BreatheKind.Glow && glow < _s.BreatheAmount) glow = ColorUtil.Clamp(_s.BreatheAmount, 0, 60);
                int pulseRoom = ColorUtil.Clamp(_s.PulseSize, 10, 80);
                if (Kind == OverlayMode.Countdown && _s.PulseSeconds > 0 && glow < pulseRoom) glow = pulseRoom;   // room for the pulse
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
            CheckNight();
            Refresh();
        }

        public void SetRole(OverlayRole role)
        {
            if (role == _role)
            {
                CheckNight();
                return;
            }
            _role = role;
            Rebuild(false, true);     // a state can have its own look
            SetFg(true);
            Refresh();
            UpdateProgress();
            ApplyNumberGlow();
            ApplyWarn();
        }

        // The night look switches on and off by the clock.
        private void CheckNight()
        {
            if (!_base.NightOn) return;
            int m = DateTime.Now.Minute;
            if (m == _nightMinute) return;
            _nightMinute = m;
            Rebuild(false, true);
        }

        private bool AnimOn { get { return _s.AnimationsOn && UiAnim.Enabled; } }

        private double SpeedFactor { get { return 100.0 / ColorUtil.Clamp(_s.AnimSpeed, 25, 400); } }

        private Duration Ms(double ms)
        {
            return new Duration(TimeSpan.FromMilliseconds(Math.Max(1, ms * SpeedFactor)));
        }

        private IEasingFunction MakeEase()
        {
            switch (_s.AnimEasing)
            {
                case EaseKind.Linear: return null;
                case EaseKind.Snappy: return new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 };
                case EaseKind.Bouncy: return new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };
                default: return new CubicEase { EasingMode = EasingMode.EaseOut };
            }
        }

        // The colour of the numbers. With "colour fade" it glides to the new colour.
        private void SetFg(bool animate)
        {
            Color target = RoleColor();
            if (animate && AnimOn && _s.ColorFade)
            {
                var ca = new ColorAnimation(_fgBrush.Color, target, Ms(ColorUtil.Clamp(_s.ColorFadeMs, 50, 3000)));
                _fgBrush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            }
            else
            {
                _fgBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
                _fgBrush.Color = target;
            }
            _fg = _fgBrush;
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
                var a = new DoubleAnimation(8, ColorUtil.Clamp(_s.PulseSize, 10, 80), new Duration(TimeSpan.FromMilliseconds(ColorUtil.Clamp(_s.PulseSpeed, 100, 3000))));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                sh.BeginAnimation(DropShadowEffect.BlurRadiusProperty, a);
            }
            else if (AnimOn && _s.Breathe == BreatheKind.Glow)
            {
                // an idle glow that slowly breathes in and out
                var sh = new DropShadowEffect();
                sh.ShadowDepth = 0;
                sh.Opacity = 1;
                sh.Color = ColorUtil.Parse(_s.BreatheColor, Colors.DeepSkyBlue);
                sh.BlurRadius = 2;
                Effect = sh;
                var a = new DoubleAnimation(2, ColorUtil.Clamp(_s.BreatheAmount, 3, 60), Ms(ColorUtil.Clamp(_s.BreatheSeconds, 1, 30) * 500.0));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                a.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
                Timeline.SetDesiredFrameRate(a, 30);
                sh.BeginAnimation(DropShadowEffect.BlurRadiusProperty, a);
            }
            else if (_s.ShadowEnabled)
            {
                var sh = new DropShadowEffect();
                sh.BlurRadius = ColorUtil.Clamp(_s.ShadowBlur, 0, 60);
                double dx = ColorUtil.Clamp(_s.ShadowOffsetX, -60, 60);
                double dy = ColorUtil.Clamp(_s.ShadowOffsetY, -60, 60);
                sh.ShadowDepth = Math.Sqrt(dx * dx + dy * dy);
                double dir = sh.ShadowDepth > 0 ? Math.Atan2(-dy, dx) * 180.0 / Math.PI : 0;
                if (dir < 0) dir += 360;
                sh.Direction = dir;
                sh.Opacity = ColorUtil.Clamp(_s.ShadowOpacity, 0, 100) / 100.0;
                sh.Color = ColorUtil.Parse(_s.ShadowColor, Colors.Black);
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
            if (!_s.AnimationsOn)
            {
                RenderTransform = Transform.Identity;
                return;
            }
            IEasingFunction ease = MakeEase();
            if (anim == ShowAnimation.Slide)
            {
                double dy = (int)pos / 3 == 2 ? 24 : -24;
                var tt = new TranslateTransform(0, dy);
                RenderTransform = tt;
                var a = new DoubleAnimation(dy, 0, Ms(260));
                a.EasingFunction = ease;
                tt.BeginAnimation(TranslateTransform.YProperty, a);
            }
            else if (anim == ShowAnimation.Pop)
            {
                var st = new ScaleTransform(0.8, 0.8);
                RenderTransform = st;
                var a = new DoubleAnimation(0.8, 1, Ms(280));
                a.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.7 };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else if (anim == ShowAnimation.Zoom)
            {
                var st = new ScaleTransform(0.3, 0.3);
                RenderTransform = st;
                var a = new DoubleAnimation(0.3, 1, Ms(320));
                a.EasingFunction = ease;
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else if (anim == ShowAnimation.Drop)
            {
                var tt = new TranslateTransform(0, -80);
                RenderTransform = tt;
                var a = new DoubleAnimation(-80, 0, Ms(520));
                a.EasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 3 };
                tt.BeginAnimation(TranslateTransform.YProperty, a);
            }
            else if (anim == ShowAnimation.Bounce)
            {
                var st = new ScaleTransform(0.5, 0.5);
                RenderTransform = st;
                var a = new DoubleAnimation(0.5, 1, Ms(600));
                a.EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 4 };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else
            {
                RenderTransform = Transform.Identity;
            }
        }

        // The hide animation (the window fades at the same time).
        public void PlayHide(HideAnim anim, OverlayPosition pos, TimeSpan time)
        {
            if (time.TotalMilliseconds < 1 || !_s.AnimationsOn) return;
            RenderTransformOrigin = new Point(0.5, 0.5);
            var d = new Duration(time);
            if (anim == HideAnim.Slide)
            {
                double dy = (int)pos / 3 == 2 ? 24 : -24;
                var tt = new TranslateTransform(0, 0);
                RenderTransform = tt;
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, dy, d));
            }
            else if (anim == HideAnim.Shrink)
            {
                var st = new ScaleTransform(1, 1);
                RenderTransform = st;
                var a = new DoubleAnimation(1, 0.6, d);
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
        }

        private Color RoleColor()
        {
            switch (_role)
            {
                case OverlayRole.Running: return ColorUtil.Parse(_s.RunningColor, Colors.LightGreen);
                case OverlayRole.Paused: return ColorUtil.Parse(_s.PausedColor, Colors.Orange);
                case OverlayRole.Finished: return ColorUtil.Parse(_s.FinishedColor, Colors.Red);
                case OverlayRole.Overtime: return ColorUtil.Parse(_s.FinishedColor, Colors.Red);
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
            if (style != DigitStyle.Analog && style != DigitStyle.Flip)
            {
                if (_lastDigits != null && digits != _lastDigits) PlayDigitChange();
                _lastDigits = digits;
            }
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
                    _fx.SetFill(_fgBrush.Color, _s.NumberGradient, ColorUtil.Parse(_s.NumberColor2, Colors.HotPink), _s.NumberGradientAngle);
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
            bool side = _s.Layout == LayoutDirection.Horizontal;
            _content.Orientation = side ? Orientation.Horizontal : Orientation.Vertical;
            HorizontalAlignment ha = HorizontalAlignment.Center;
            if (_s.PartAlign == PartAlignment.Left) ha = HorizontalAlignment.Left;
            else if (_s.PartAlign == PartAlignment.Right) ha = HorizontalAlignment.Right;
            VerticalAlignment va = VerticalAlignment.Center;
            if (_s.PartAlign == PartAlignment.Left) va = VerticalAlignment.Top;
            else if (_s.PartAlign == PartAlignment.Right) va = VerticalAlignment.Bottom;
            _info.TextAlignment = _s.PartAlign == PartAlignment.Left ? TextAlignment.Left
                : (_s.PartAlign == PartAlignment.Right ? TextAlignment.Right : TextAlignment.Center);
            for (int i = 0; i < order.Count; i++)
            {
                var part = (FrameworkElement)map[order[i]];
                double gap = i == 0 ? 0 : space;
                bool isBar = order[i] == "bar";
                if (side)
                {
                    part.Margin = new Thickness(gap, 0, 0, 0);
                    part.VerticalAlignment = va;
                    part.HorizontalAlignment = HorizontalAlignment.Left;
                    if (isBar) part.Width = 90;
                }
                else
                {
                    part.Margin = new Thickness(0, gap + (isBar ? 4 : 0), 0, 0);
                    part.VerticalAlignment = VerticalAlignment.Stretch;
                    part.HorizontalAlignment = isBar ? HorizontalAlignment.Stretch : ha;
                    if (isBar) part.Width = double.NaN;
                }
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

        // ------------------------------------------------------------ background and shape (1.4.6)

        // A circle is always as wide as it is tall.
        protected override Size MeasureOverride(Size constraint)
        {
            Size size = base.MeasureOverride(constraint);
            if (_s.Shape == PillShape.Circle)
            {
                double m = Math.Max(size.Width, size.Height);
                return new Size(m, m);
            }
            return size;
        }

        private static void Lay(ShapeLayer layer, Brush fill, PillShape shape, double[] r, double inset, Thickness over)
        {
            if (fill == null)
            {
                layer.Visibility = Visibility.Collapsed;
                return;
            }
            layer.Margin = over;
            layer.Set(shape, r[0], r[1], r[2], r[3], inset, fill, null, 0, BorderLine.Solid);
            layer.Visibility = Visibility.Visible;
        }

        // The fill of the pill: solid, gradient (two or three colours) or radial. Null = none.
        private static Brush MakeFill(ModeStyle s, bool animate)
        {
            if (s.BackgroundStyle == BackgroundStyle.None) return null;
            Color c1 = ColorUtil.WithAlpha(ColorUtil.Parse(s.BackgroundColor, Color.FromRgb(0x10, 0x10, 0x18)), s.BackgroundOpacity);
            if (s.BackgroundStyle == BackgroundStyle.Solid) return new SolidColorBrush(c1);

            Color c2 = ColorUtil.WithAlpha(ColorUtil.Parse(s.BackgroundColor2, Color.FromRgb(0x2A, 0x16, 0x50)), s.BackgroundOpacity);
            var stops = new GradientStopCollection();
            stops.Add(new GradientStop(c1, 0));
            if (!string.IsNullOrEmpty(s.BackgroundColor3))
                stops.Add(new GradientStop(ColorUtil.WithAlpha(ColorUtil.Parse(s.BackgroundColor3, Colors.Purple), s.BackgroundOpacity), 0.5));
            stops.Add(new GradientStop(c2, 1));
            if (s.BackgroundStyle == BackgroundStyle.Radial) return new RadialGradientBrush(stops);
            var lg = new LinearGradientBrush(stops, s.GradientAngle);
            if (animate && s.AnimatedGradient)
            {
                // the gradient slowly turns round
                var rt = new RotateTransform(0, 0.5, 0.5);
                lg.RelativeTransform = rt;
                var a = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(ColorUtil.Clamp(s.GradientSeconds, 1, 120) * 100.0 / ColorUtil.Clamp(s.AnimSpeed, 25, 400))));
                a.RepeatBehavior = RepeatBehavior.Forever;
                Timeline.SetDesiredFrameRate(a, 20);
                rt.BeginAnimation(RotateTransform.AngleProperty, a);
            }
            return lg;
        }

        private static Brush MakeBorderBrush(ModeStyle s)
        {
            Color b1 = ColorUtil.WithAlpha(ColorUtil.Parse(s.BorderColor, Colors.White), s.BorderOpacity);
            if (!s.BorderGradient) return new SolidColorBrush(b1);
            Color b2 = ColorUtil.WithAlpha(ColorUtil.Parse(s.BorderColor2, Colors.White), s.BorderOpacity);
            return new LinearGradientBrush(b1, b2, s.BorderGradientAngle);
        }

        // A soft glow along the inside edge of the pill.
        private static Brush MakeInnerGlow(ModeStyle s)
        {
            if (s.InnerGlowSize <= 0) return null;
            Color gc = ColorUtil.Parse(s.InnerGlowColor, Colors.DeepSkyBlue);
            double size = ColorUtil.Clamp(s.InnerGlowSize, 1, 100) / 100.0;
            var rb = new RadialGradientBrush();
            rb.GradientStops.Add(new GradientStop(Color.FromArgb(0, gc.R, gc.G, gc.B), Math.Max(0.0, 1.0 - size)));
            rb.GradientStops.Add(new GradientStop(ColorUtil.WithAlpha(gc, s.InnerGlowOpacity), 1.0));
            return rb;
        }

        // A shine on the top half, like glass.
        private static Brush MakeGlass(ModeStyle s)
        {
            if (!s.Glass) return null;
            double a = ColorUtil.Clamp(s.GlassStrength, 0, 100) / 100.0;
            var lg = new LinearGradientBrush();
            lg.StartPoint = new Point(0, 0);
            lg.EndPoint = new Point(0, 1);
            lg.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(255 * 0.55 * a), 255, 255, 255), 0));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(255 * 0.12 * a), 255, 255, 255), 0.5));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.5));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            return lg;
        }

        // Noise, stripes, dots or a grid, tiled over the pill.
        private static Brush MakeTexture(ModeStyle s)
        {
            if (s.Texture == TextureStyle.None) return null;
            double tile = ColorUtil.Clamp(s.TextureSize, 3, 60);
            Color c = ColorUtil.Parse(s.TextureColor, Colors.White);
            Brush ink = new SolidColorBrush(c);
            var group = new DrawingGroup();
            double tileSize = tile;

            if (s.Texture == TextureStyle.Stripes)
            {
                group.Children.Add(new GeometryDrawing(ink, null, new RectangleGeometry(new Rect(0, 0, tile, tile / 2))));
            }
            else if (s.Texture == TextureStyle.Dots)
            {
                group.Children.Add(new GeometryDrawing(ink, null, new EllipseGeometry(new Point(tile / 2, tile / 2), tile * 0.18, tile * 0.18)));
            }
            else if (s.Texture == TextureStyle.Grid)
            {
                group.Children.Add(new GeometryDrawing(ink, null, new RectangleGeometry(new Rect(0, 0, tile, 1))));
                group.Children.Add(new GeometryDrawing(ink, null, new RectangleGeometry(new Rect(0, 0, 1, tile))));
            }
            else
            {
                tileSize = 64;
                var rnd = new Random(7);
                double dot = Math.Max(1, tile / 5);
                for (int i = 0; i < 160; i++)
                {
                    Brush b = new SolidColorBrush(Color.FromArgb((byte)rnd.Next(60, 255), c.R, c.G, c.B));
                    group.Children.Add(new GeometryDrawing(b, null, new RectangleGeometry(new Rect(rnd.Next(0, 64), rnd.Next(0, 64), dot, dot))));
                }
            }

            var db = new DrawingBrush(group);
            db.TileMode = TileMode.Tile;
            db.ViewboxUnits = BrushMappingMode.Absolute;
            db.ViewportUnits = BrushMappingMode.Absolute;
            db.Viewbox = new Rect(0, 0, tileSize, tileSize);
            db.Viewport = new Rect(0, 0, tileSize, tileSize);
            db.Opacity = ColorUtil.Clamp(s.TextureOpacity, 0, 100) / 100.0;
            if (s.Texture == TextureStyle.Stripes) db.Transform = new RotateTransform(45);
            return db;
        }

        // ------------------------------------------------------------ animations (1.4.7)

        // When the numbers change: fade, slide, flip or pop.
        private void PlayDigitChange()
        {
            if (!AnimOn || _s.DigitAnimation == DigitAnim.None) return;
            Duration d = Ms(200);
            IEasingFunction ease = MakeEase();
            _g3.RenderTransformOrigin = new Point(0.5, 0.5);
            if (_s.DigitAnimation == DigitAnim.Fade)
            {
                var a = new DoubleAnimation(0.2, 1, d);
                _g3.BeginAnimation(UIElement.OpacityProperty, a);
            }
            else if (_s.DigitAnimation == DigitAnim.Slide)
            {
                var tt = new TranslateTransform(0, 12);
                _g3.RenderTransform = tt;
                var a = new DoubleAnimation(12, 0, d);
                a.EasingFunction = ease;
                tt.BeginAnimation(TranslateTransform.YProperty, a);
            }
            else if (_s.DigitAnimation == DigitAnim.Pop)
            {
                var st = new ScaleTransform(0.8, 0.8);
                _g3.RenderTransform = st;
                var a = new DoubleAnimation(0.8, 1, d);
                a.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else
            {
                var st = new ScaleTransform(1, 0.1);
                _g3.RenderTransform = st;
                var a = new DoubleAnimation(0.1, 1, d);
                a.EasingFunction = ease;
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
        }

        // The numbers slowly grow and shrink while idle.
        private void ApplyBreatheSize()
        {
            _g2.RenderTransformOrigin = new Point(0.5, 0.5);
            if (AnimOn && _s.Breathe == BreatheKind.Size)
            {
                double to = 1 + ColorUtil.Clamp(_s.BreatheSize, 1, 30) / 100.0;
                var st = new ScaleTransform(1, 1);
                _g2.RenderTransform = st;
                var a = new DoubleAnimation(1, to, Ms(ColorUtil.Clamp(_s.BreatheSeconds, 1, 30) * 500.0));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                a.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
                Timeline.SetDesiredFrameRate(a, 30);
                st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else
            {
                _g2.RenderTransform = null;
            }
        }

        // Shake or flash while a countdown is almost out.
        private void ApplyWarn()
        {
            _content.BeginAnimation(UIElement.OpacityProperty, null);
            _content.Opacity = 1;
            _content.RenderTransform = null;
            if (!AnimOn || _role != OverlayRole.Danger || _s.WarnAnimation == WarnAnim.None) return;

            if (_s.WarnAnimation == WarnAnim.Shake)
            {
                var tt = new TranslateTransform(0, 0);
                _content.RenderTransform = tt;
                var a = new DoubleAnimationUsingKeyFrames();
                a.Duration = new Duration(TimeSpan.FromMilliseconds(420 * SpeedFactor));
                a.RepeatBehavior = RepeatBehavior.Forever;
                double[] xs = { 0, -4, 4, -3, 3, 0 };
                for (int i = 0; i < xs.Length; i++)
                    a.KeyFrames.Add(new LinearDoubleKeyFrame(xs[i], KeyTime.FromPercent(i / (double)(xs.Length - 1))));
                tt.BeginAnimation(TranslateTransform.XProperty, a);
            }
            else
            {
                var a = new DoubleAnimation(1, 0.25, Ms(350));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                _content.BeginAnimation(UIElement.OpacityProperty, a);
            }
        }

        // Re-reads every appearance setting.
        public void Apply(ModeStyle s)
        {
            _base = s;
            Rebuild(false, false);
        }

        // Draws the look. A state (low time, finished, night...) can change part of it.
        // Nothing is redrawn when nothing changed.
        private void Rebuild(bool force, bool keepColour)
        {
            ModeStyle eff = StateLooks.Resolve(_base, _role, DateTime.Now, ForceNight);
            string sig = Kind + "|" + eff.Signature();
            if (!force && sig == _sig) return;
            _sig = sig;
            ApplyStyle(eff, !keepColour);
        }

        private void ApplyStyle(ModeStyle s, bool resetColour)
        {
            _s = s;
            _shown = null;
            _lastDigits = null;
            if (resetColour) SetFg(false);
            else _fg = _fgBrush;

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

            // fixed size (0 = automatic)
            Width = s.FixedWidth > 0 ? ColorUtil.Clamp(s.FixedWidth, 20, 3000) : double.NaN;
            Height = s.FixedHeight > 0 ? ColorUtil.Clamp(s.FixedHeight, 20, 3000) : double.NaN;
            MinWidth = s.MinWidthPx > 0 ? ColorUtil.Clamp(s.MinWidthPx, 0, 3000) : 0;

            // ---- background, shape, border, picture and the extras on the background ----
            double padH = ColorUtil.Clamp(s.PaddingH, 0, 200);
            double padV = ColorUtil.Clamp(s.PaddingV, 0, 200);
            double bt = ColorUtil.Clamp(s.BorderThickness, 0, 20);
            double tl, tr, br, bl;
            if (s.PerCorner)
            {
                tl = ColorUtil.Clamp(s.CornerTL, 0, 200);
                tr = ColorUtil.Clamp(s.CornerTR, 0, 200);
                br = ColorUtil.Clamp(s.CornerBR, 0, 200);
                bl = ColorUtil.Clamp(s.CornerBL, 0, 200);
            }
            else
            {
                tl = ColorUtil.Clamp(s.CornerRadius, 0, 200);
                tr = tl; br = tl; bl = tl;
            }
            PillShape shape = s.Shape;
            bool custom = shape != PillShape.Rounded || s.BorderLineStyle != BorderLine.Solid;
            Brush fill = MakeFill(s, AnimOn);
            Brush edge = MakeBorderBrush(s);
            Thickness over;

            if (!custom)
            {
                // a plain rounded box: the Border draws everything itself
                Background = fill ?? Brushes.Transparent;
                CornerRadius = new CornerRadius(tl, tr, br, bl);
                BorderThickness = new Thickness(bt);
                BorderBrush = edge;
                Padding = new Thickness(padH, padV, padH, padV);
                over = new Thickness(-padH, -padV, -padH, -padV);
                _shape.Visibility = Visibility.Collapsed;
            }
            else
            {
                // another shape or border style: a shape layer draws the fill and the border
                Background = Brushes.Transparent;
                CornerRadius = new CornerRadius(0);
                BorderThickness = new Thickness(0);
                BorderBrush = null;
                Padding = new Thickness(padH + bt, padV + bt, padH + bt, padV + bt);
                over = new Thickness(-(padH + bt), -(padV + bt), -(padH + bt), -(padV + bt));
                _shape.Margin = over;
                _shape.Set(shape, tl, tr, br, bl, 0, fill, bt > 0 ? edge : null, bt, s.BorderLineStyle);
                _shape.Visibility = Visibility.Visible;
            }

            // the layers on top of the fill sit inside the border
            double inset = custom ? bt : 0;
            double[] inner = { Math.Max(0, tl - bt), Math.Max(0, tr - bt), Math.Max(0, br - bt), Math.Max(0, bl - bt) };

            // picture behind the numbers (and an optional colour over it)
            Brush picFill = null;
            Brush tintFill = null;
            BitmapImage back = LoadImage(s.BackgroundImagePath, 900);
            if (back != null)
            {
                var ib = new ImageBrush(back);
                ib.Opacity = ColorUtil.Clamp(s.BackgroundImageOpacity, 0, 100) / 100.0;
                ib.Stretch = s.BackgroundImageFit == ImageFit.Fit ? Stretch.Uniform
                           : (s.BackgroundImageFit == ImageFit.Stretch ? Stretch.Fill : Stretch.UniformToFill);
                double zoom = ColorUtil.Clamp(s.BackgroundImageZoom, 50, 500) / 100.0;
                if (Math.Abs(zoom - 1) > 0.001 || s.BackgroundImageX != 0 || s.BackgroundImageY != 0)
                {
                    var tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(zoom, zoom, 0.5, 0.5));
                    tg.Children.Add(new TranslateTransform(ColorUtil.Clamp(s.BackgroundImageX, -100, 100) / 100.0,
                                                           ColorUtil.Clamp(s.BackgroundImageY, -100, 100) / 100.0));
                    ib.RelativeTransform = tg;
                }
                picFill = ib;
                if (!string.IsNullOrEmpty(s.BackgroundImageTint) && s.BackgroundImageTintAmount > 0)
                    tintFill = new SolidColorBrush(ColorUtil.WithAlpha(ColorUtil.Parse(s.BackgroundImageTint, Colors.Black), s.BackgroundImageTintAmount));
            }
            Lay(_picLayer, picFill, shape, inner, inset, over);
            Lay(_tintLayer, tintFill, shape, inner, inset, over);

            // texture, inner glow and glass shine
            Lay(_texLayer, MakeTexture(s), shape, inner, inset, over);
            Lay(_innerLayer, MakeInnerGlow(s), shape, inner, inset, over);
            Lay(_glassLayer, MakeGlass(s), shape, inner, inset, over);

            // ---- shadow / glow around the pill ----
            ApplyEffect();

            // ---- size ----
            double k = ColorUtil.Clamp(s.Scale, 25, 500) / 100.0;
            LayoutTransform = new ScaleTransform(k, k);
            Margin = new Thickness(OuterMargin);

            if (_moveOutline.Visibility == Visibility.Visible) SetMoveOutline(true);
            ApplyBreatheSize();
            ApplyWarn();
            Refresh();
            UpdateProgress();
        }
    }
}
