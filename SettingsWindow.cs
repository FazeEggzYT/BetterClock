// The settings window (part 1): window, theme, animations, live preview and building blocks.
// The tabs themselves are in SettingsTabs.cs.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BetterClock
{
    public partial class SettingsWindow : Window
    {
        private enum PreviewMode { Clock, TimerRunning, TimerPaused, Countdown, Finished }

        private static SettingsWindow _instance;

        private readonly TabControl _tabs = new TabControl();
        private readonly Grid _root = new Grid();
        private readonly Rectangle _accentBar = new Rectangle();
        private readonly DispatcherTimer _saveTimer = new DispatcherTimer();
        private readonly DispatcherTimer _previewTimer = new DispatcherTimer();
        private readonly Dictionary<OverlayMode, OverlayView> _mapViews = new Dictionary<OverlayMode, OverlayView>();
        private readonly Dictionary<OverlayMode, StackPanel> _modeHosts = new Dictionary<OverlayMode, StackPanel>();
        private readonly Dictionary<string, Button> _hotkeyButtons = new Dictionary<string, Button>();

        private readonly Dictionary<OverlayMode, CheckBox> _customChecks = new Dictionary<OverlayMode, CheckBox>();
        private StackPanel _gamesPage;
        private TextBlock _moveBanner;
        private Action _onMoveMode;
        private Action<OverlayMode> _onMoved;
        private Action _onProfile;

        private OverlayView _zoomView;
        private TextBlock _hotkeyStatus;
        private HotkeyDef _capturing;
        private PreviewMode _previewMode = PreviewMode.Clock;
        private bool _loading = true;

        private static readonly string[] Presets =
        {
            "#FFFFFF", "#000000", "#7CFFB2", "#FFB35C", "#FF5C5C", "#4D7CFF", "#A85CFF", "#FFD84D", "#FF7AD9", "#10101C"
        };

        private AppSettings S { get { return AppHost.Settings; } }

        public static void ShowSingleton()
        {
            ShowSingleton(null);
        }

        public static void ShowSingleton(string tab)
        {
            if (_instance != null)
            {
                if (_instance.WindowState == WindowState.Minimized) _instance.WindowState = WindowState.Normal;
                _instance.Activate();
                if (tab != null) _instance.SelectTab(tab);
                return;
            }
            _instance = new SettingsWindow();
            _instance.Show();
            _instance.Activate();
            if (tab != null) _instance.SelectTab(tab);
        }

        public SettingsWindow()
        {
            Title = Program.AppName + " - Settings";
            Width = 1120;
            Height = 800;
            MinWidth = 980;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;
            UseLayoutRounding = true;

            var icon = IconFactory.GetImageSource();
            if (icon != null) Icon = icon;

            // ---- theme: brushes first, then the control styles that use them ----
            ThemeManager.Apply(Resources, S, false);
            Resources.MergedDictionaries.Add(ControlStyles.Create());
            SetResourceReference(BackgroundProperty, "BgBrush");

            // ---- layout: thin animated bar on top, tabs on the left, preview on the right ----
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _accentBar.Height = 3;
            Grid.SetRow(_accentBar, 0);
            _root.Children.Add(_accentBar);

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
            Grid.SetRow(body, 1);
            _root.Children.Add(body);

            _tabs.Margin = new Thickness(12, 12, 0, 12);
            Grid.SetColumn(_tabs, 0);
            body.Children.Add(_tabs);

            UIElement preview = BuildPreviewPanel();
            Grid.SetColumn(preview, 1);
            body.Children.Add(preview);

            Content = _root;

            BuildAllTabs();
            _tabs.SelectionChanged += OnTabChanged;

            ApplyUiScale();
            ApplyAccentBar();

            _saveTimer.Interval = TimeSpan.FromMilliseconds(600);
            _saveTimer.Tick += delegate { _saveTimer.Stop(); AppHost.SaveSettings(); };

            _previewTimer.Interval = TimeSpan.FromMilliseconds(500);
            _previewTimer.Tick += delegate { UpdatePreview(); };
            _previewTimer.Start();

            PreviewKeyDown += OnCaptureKey;

            Closing += delegate
            {
                if (_capturing != null) EndCapture();
                _saveTimer.Stop();
                AppHost.SaveSettings();
            };
            Closed += delegate
            {
                _previewTimer.Stop();
                AppHost.MoveModeChanged -= _onMoveMode;
                AppHost.OverlayMoved -= _onMoved;
                if (AppHost.Games != null) AppHost.Games.ProfileChanged -= _onProfile;
                _instance = null;
            };

            // live updates from dragging and from game switching
            _onMoveMode = delegate { UpdateMoveBanner(); };
            _onMoved = delegate(OverlayMode m) { RefillMode(m); UpdatePreview(); };
            _onProfile = delegate
            {
                RefillMode(OverlayMode.Clock);
                RefillMode(OverlayMode.Timer);
                RefillMode(OverlayMode.Countdown);
                RefillGames();
                UpdatePreview();
            };
            AppHost.MoveModeChanged += _onMoveMode;
            AppHost.OverlayMoved += _onMoved;
            if (AppHost.Games != null) AppHost.Games.ProfileChanged += _onProfile;
            UpdateMoveBanner();

            _loading = false;
            UpdatePreview();

            // Window opening animation.
            if (UiAnim.Enabled)
            {
                Opacity = 0;
                Loaded += delegate
                {
                    var fade = new DoubleAnimation(0, 1, UiAnim.Dur(260));
                    BeginAnimation(OpacityProperty, fade);
                    UiAnim.FadeSlideIn(_root, 0, 18, 340);
                };
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ThemeManager.ApplyTitleBar(new WindowInteropHelper(this).Handle, ThemeManager.IsDark(S));
        }

        // ======================================================================== theme and look of the window

        // Called when theme, accent, animation or scale settings change.
        private void ReapplyTheme()
        {
            UiAnim.Enabled = S.AnimationsEnabled;
            UiAnim.SpeedPercent = S.AnimationSpeed;
            ThemeManager.Apply(Resources, S, true);
            ThemeManager.ApplyTitleBar(new WindowInteropHelper(this).Handle, ThemeManager.IsDark(S));
            ApplyAccentBar();
            ApplyUiScale();
        }

        private void ApplyUiScale()
        {
            double k = ColorUtil.Clamp(S.UiScale, 60, 200) / 100.0;
            _root.LayoutTransform = new ScaleTransform(k, k);
        }

        private void ApplyAccentBar()
        {
            Color a = ColorUtil.Parse(S.AccentColor, Color.FromRgb(0x4D, 0x7C, 0xFF));
            Color b = Color.FromRgb((byte)(255 - a.R / 2), (byte)(a.G / 2 + 40), 255);   // a friendly second colour
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 0);
            brush.SpreadMethod = GradientSpreadMethod.Repeat;
            brush.GradientStops.Add(new GradientStop(a, 0));
            brush.GradientStops.Add(new GradientStop(b, 0.5));
            brush.GradientStops.Add(new GradientStop(a, 1));
            var move = new TranslateTransform(0, 0);
            brush.RelativeTransform = move;
            _accentBar.Fill = brush;

            if (UiAnim.Enabled)
            {
                var anim = new DoubleAnimation(0, 1, UiAnim.Dur(4500));
                anim.RepeatBehavior = RepeatBehavior.Forever;
                move.BeginAnimation(TranslateTransform.XProperty, anim);
            }
        }

        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != _tabs) return;   // ignore combo boxes inside the tabs
            var item = _tabs.SelectedItem as TabItem;
            if (item == null) return;

            string name = item.Header as string;
            if (name == "Clock") SetPreviewDefault(OverlayMode.Clock);
            else if (name == "Timer") SetPreviewDefault(OverlayMode.Timer);
            else if (name == "Countdown") SetPreviewDefault(OverlayMode.Countdown);

            var content = item.Content as FrameworkElement;
            if (content != null) UiAnim.FadeSlideIn(content, 0, 14, 240);
        }

        private void SetPreviewDefault(OverlayMode mode)
        {
            if (mode == OverlayMode.Clock) _previewMode = PreviewMode.Clock;
            else if (mode == OverlayMode.Timer && ModeOf(_previewMode) != OverlayMode.Timer) _previewMode = PreviewMode.TimerRunning;
            else if (mode == OverlayMode.Countdown && ModeOf(_previewMode) != OverlayMode.Countdown) _previewMode = PreviewMode.Countdown;
            UpdatePreview();
        }

        public void SelectTab(string header)
        {
            foreach (object o in _tabs.Items)
            {
                var t = o as TabItem;
                if (t != null && (string)t.Header == header) { _tabs.SelectedItem = t; return; }
            }
        }

        // Throws away every tab and builds them again (after import / reset).
        private void RebuildAll()
        {
            string selected = null;
            var cur = _tabs.SelectedItem as TabItem;
            if (cur != null) selected = cur.Header as string;

            bool was = _loading;
            _loading = true;
            _tabs.Items.Clear();
            _hotkeyButtons.Clear();
            _modeHosts.Clear();
            _gamesPage = null;
            BuildAllTabs();
            _loading = was;

            if (selected != null) SelectTab(selected);
            ReapplyTheme();
            UpdatePreview();
        }

        // ======================================================================== live preview

        private static OverlayMode ModeOf(PreviewMode m)
        {
            if (m == PreviewMode.Clock) return OverlayMode.Clock;
            if (m == PreviewMode.TimerRunning || m == PreviewMode.TimerPaused) return OverlayMode.Timer;
            return OverlayMode.Countdown;
        }

        private UIElement BuildPreviewPanel()
        {
            var panel = new Border();
            panel.SetResourceReference(Border.BackgroundProperty, "PanelBrush");
            panel.CornerRadius = new CornerRadius(14);
            panel.Margin = new Thickness(10, 12, 12, 12);
            panel.Padding = new Thickness(14);

            var sp = new StackPanel();
            panel.Child = sp;

            _moveBanner = new TextBlock();
            _moveBanner.Text = "MOVE MODE IS ON - drag the overlays on your screen, then press Lock (or Ctrl+Alt+M).";
            _moveBanner.TextWrapping = TextWrapping.Wrap;
            _moveBanner.FontWeight = FontWeights.SemiBold;
            _moveBanner.Foreground = Brushes.DeepSkyBlue;
            _moveBanner.Margin = new Thickness(0, 0, 0, 8);
            _moveBanner.Visibility = Visibility.Collapsed;
            sp.Children.Add(_moveBanner);

            sp.Children.Add(Heading("Live preview", 17));
            sp.Children.Add(Hint("Changes show here instantly. The mini screen shows where each overlay sits."));

            // ---- mini screen (1920 x 1080 scaled down) ----
            var screen = new Grid();
            screen.Width = 1920;
            screen.Height = 1080;
            screen.Background = new LinearGradientBrush(Color.FromRgb(0x1B, 0x2A, 0x4A), Color.FromRgb(0x0B, 0x0E, 0x1A), 60);
            foreach (OverlayMode m in new OverlayMode[] { OverlayMode.Clock, OverlayMode.Timer, OverlayMode.Countdown })
            {
                var v = new OverlayView();
                v.Kind = m;
                _mapViews[m] = v;
                screen.Children.Add(v);
            }

            var frame = new Border();
            frame.CornerRadius = new CornerRadius(8);
            frame.BorderThickness = new Thickness(3);
            frame.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3F, 0x66));
            frame.ClipToBounds = true;
            frame.Margin = new Thickness(0, 8, 0, 0);
            var box = new Viewbox();
            box.Stretch = Stretch.Uniform;
            box.Width = 356;
            box.Height = 200;
            box.Child = screen;
            frame.Child = box;
            sp.Children.Add(frame);

            // ---- state buttons ----
            var states = new WrapPanel();
            states.Margin = new Thickness(0, 10, 0, 0);
            states.Children.Add(StateButton("Clock", PreviewMode.Clock));
            states.Children.Add(StateButton("Timer", PreviewMode.TimerRunning));
            states.Children.Add(StateButton("Paused", PreviewMode.TimerPaused));
            states.Children.Add(StateButton("Countdown", PreviewMode.Countdown));
            states.Children.Add(StateButton("At zero", PreviewMode.Finished));
            sp.Children.Add(states);

            // ---- close-up ----
            sp.Children.Add(Heading("Close-up", 14));
            var zoomHost = new Border();
            zoomHost.CornerRadius = new CornerRadius(10);
            zoomHost.Margin = new Thickness(0, 6, 0, 0);
            zoomHost.Height = 170;
            zoomHost.Background = new LinearGradientBrush(Color.FromRgb(0x24, 0x35, 0x5E), Color.FromRgb(0x0E, 0x12, 0x22), 70);
            _zoomView = new OverlayView();
            _zoomView.HorizontalAlignment = HorizontalAlignment.Center;
            _zoomView.VerticalAlignment = VerticalAlignment.Center;
            var zbox = new Viewbox();
            zbox.StretchDirection = StretchDirection.DownOnly;
            zbox.Stretch = Stretch.Uniform;
            zbox.Margin = new Thickness(8);
            zbox.Child = _zoomView;
            zoomHost.Child = zbox;
            sp.Children.Add(zoomHost);

            var tip = Hint("Tip: the real overlay is click-through, so it never blocks your game.");
            tip.Margin = new Thickness(0, 10, 0, 0);
            sp.Children.Add(tip);
            return panel;
        }

        private void UpdateMoveBanner()
        {
            if (_moveBanner != null) _moveBanner.Visibility = AppHost.MoveMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private Button StateButton(string text, PreviewMode mode)
        {
            var b = new Button();
            b.Content = text;
            b.Padding = new Thickness(10, 4, 10, 4);
            b.Margin = new Thickness(0, 0, 6, 6);
            b.Click += delegate { _previewMode = mode; UpdatePreview(); };
            return b;
        }

        // The extra clock lines (world clocks, dates, session) as they will look.
        private string PreviewInfo(OverlayMode m)
        {
            if (m != OverlayMode.Clock) return "";
            return OverlayController.BuildInfo(S, DateTime.Now, DateTime.Now.AddMinutes(-83));
        }

        // How full the progress ring / bar looks in the preview.
        private double PreviewProgress(OverlayMode m)
        {
            if (m == OverlayMode.Countdown) return _previewMode == PreviewMode.Finished ? 0 : 0.65;
            if (m == OverlayMode.Timer && S.TimerTargetEnabled) return 0.4;
            return -1;
        }

        private void UpdatePreview()
        {
            if (_zoomView == null || _mapViews.Count == 0) return;

            OverlayMode focus = ModeOf(_previewMode);
            string clockText = OverlayController.FormatClock(S, DateTime.Now);
            string timerText = "12:34";
            string countText = OverlayController.FormatTime(S.CountdownTotalSeconds);

            // close-up
            ModeStyle zs = S.StyleFor(focus);
            _zoomView.Kind = focus;
            _zoomView.Apply(zs);
            _zoomView.Margin = new Thickness(_zoomView.OuterMargin);
            _zoomView.SetProgress(PreviewProgress(focus));
            _zoomView.SetInfo(PreviewInfo(focus));
            switch (_previewMode)
            {
                case PreviewMode.Clock: _zoomView.SetText(clockText); _zoomView.SetRole(OverlayRole.Normal); break;
                case PreviewMode.TimerRunning: _zoomView.SetText(timerText); _zoomView.SetRole(OverlayRole.Running); break;
                case PreviewMode.TimerPaused: _zoomView.SetText(timerText); _zoomView.SetRole(OverlayRole.Paused); break;
                case PreviewMode.Countdown: _zoomView.SetText(countText); _zoomView.SetRole(OverlayRole.Normal); break;
                default: _zoomView.SetText("00:00"); _zoomView.SetRole(OverlayRole.Finished); break;
            }

            // mini screen
            foreach (KeyValuePair<OverlayMode, OverlayView> kv in _mapViews)
            {
                ModeStyle st = S.StyleFor(kv.Key);
                OverlayView v = kv.Value;
                v.Apply(st);
                int pos = (int)st.Position;
                int col = pos % 3;
                int row = pos / 3;
                v.HorizontalAlignment = col == 0 ? HorizontalAlignment.Left : (col == 1 ? HorizontalAlignment.Center : HorizontalAlignment.Right);
                v.VerticalAlignment = row == 0 ? VerticalAlignment.Top : (row == 1 ? VerticalAlignment.Center : VerticalAlignment.Bottom);
                v.Margin = new Thickness(ColorUtil.Clamp(st.EdgeMargin, 0, 1000));
                if (st.UseCustomPosition)
                {
                    v.HorizontalAlignment = HorizontalAlignment.Left;
                    v.VerticalAlignment = VerticalAlignment.Top;
                    v.Margin = new Thickness(ColorUtil.Clamp(st.CustomX, 0, 8000), ColorUtil.Clamp(st.CustomY, 0, 8000), 0, 0);
                }
                v.Opacity = kv.Key == focus ? 1.0 : 0.35;
                v.SetProgress(PreviewProgress(kv.Key));
                v.SetInfo(PreviewInfo(kv.Key));

                if (kv.Key == OverlayMode.Clock) { v.SetText(clockText); v.SetRole(OverlayRole.Normal); }
                else if (kv.Key == OverlayMode.Timer)
                {
                    v.SetText(timerText);
                    v.SetRole(_previewMode == PreviewMode.TimerPaused ? OverlayRole.Paused : OverlayRole.Running);
                }
                else
                {
                    bool zero = _previewMode == PreviewMode.Finished;
                    v.SetText(zero ? "00:00" : countText);
                    v.SetRole(zero ? OverlayRole.Finished : OverlayRole.Normal);
                }
            }
        }

        // ======================================================================== saving

        private void Changed()
        {
            if (_loading) return;
            AppHost.ApplySettings();
            UpdatePreview();
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // ======================================================================== building blocks

        private TextBlock Heading(string text, double size)
        {
            var t = new TextBlock();
            t.Text = text;
            t.FontSize = size;
            t.FontWeight = FontWeights.SemiBold;
            t.SetResourceReference(TextBlock.ForegroundProperty, "InkBrush");
            return t;
        }

        private TextBlock Hint(string text)
        {
            var t = new TextBlock();
            t.Text = text;
            t.TextWrapping = TextWrapping.Wrap;
            t.FontSize = 12;
            t.Margin = new Thickness(0, 2, 0, 6);
            t.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            return t;
        }

        private TextBlock Label(string text)
        {
            var t = new TextBlock();
            t.Text = text;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.TextWrapping = TextWrapping.Wrap;
            t.SetResourceReference(TextBlock.ForegroundProperty, "InkBrush");
            return t;
        }

        // A new tab with a scrolling page. Returns the stack you add things to.
        private StackPanel NewTab(string header, string title, string subtitle)
        {
            var page = new StackPanel();
            page.Margin = new Thickness(26, 22, 20, 24);
            page.Children.Add(Heading(title, 22));
            var sub = Hint(subtitle);
            sub.Margin = new Thickness(0, 2, 0, 16);
            page.Children.Add(sub);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll.Content = page;

            var tab = new TabItem();
            tab.Header = header;
            tab.Content = scroll;
            _tabs.Items.Add(tab);
            return page;
        }

        private Grid AddRow(StackPanel p, string label)
        {
            var g = new Grid();
            g.Margin = new Thickness(0, 4, 0, 4);
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (label != null)
            {
                TextBlock l = Label(label);
                Grid.SetColumn(l, 0);
                g.Children.Add(l);
            }
            p.Children.Add(g);
            return g;
        }

        private static void Put(Grid row, UIElement el)
        {
            Grid.SetColumn(el, 1);
            row.Children.Add(el);
        }

        private Button MakeButton(string text, Action onClick)
        {
            var b = new Button();
            b.Content = text;
            b.Margin = new Thickness(0, 0, 8, 0);
            b.Click += delegate { onClick(); };
            return b;
        }

        private CheckBox AddCheck(StackPanel p, object owner, string label, string field, Action<bool> extra)
        {
            var c = new CheckBox();
            c.Content = label;
            c.Margin = new Thickness(0, 6, 0, 6);
            c.IsChecked = (bool)Reflect.Get(owner, field);
            c.Click += delegate
            {
                if (_loading) return;
                bool v = c.IsChecked == true;
                Reflect.Set(owner, field, v);
                if (extra != null) extra(v);
                Changed();
            };
            p.Children.Add(c);
            return c;
        }

        private void AddText(StackPanel p, object owner, string label, string field, double width)
        {
            Grid row = AddRow(p, label);
            var tb = new TextBox();
            tb.Width = width;
            tb.HorizontalAlignment = HorizontalAlignment.Left;
            tb.Text = (string)Reflect.Get(owner, field);
            tb.TextChanged += delegate
            {
                if (_loading) return;
                Reflect.Set(owner, field, tb.Text);
                Changed();
            };
            Put(row, tb);
        }

        private void AddNumberBox(StackPanel p, object owner, string label, string field, int min, int max, string unit)
        {
            Grid row = AddRow(p, label);
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var tb = new TextBox();
            tb.Width = 90;
            tb.Text = Reflect.Get(owner, field).ToString();
            tb.TextChanged += delegate
            {
                if (_loading) return;
                int v;
                if (!int.TryParse(tb.Text.Trim(), out v)) return;
                if (v < min) v = min;
                if (v > max) v = max;
                Reflect.Set(owner, field, v);
                Changed();
            };
            sp.Children.Add(tb);
            var u = Label("  " + unit);
            sp.Children.Add(u);
            Put(row, sp);
        }

        private void AddSlider(StackPanel p, object owner, string label, string field, double min, double max, double step, string unit, Action extra)
        {
            Grid row = AddRow(p, label);
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            bool isInt = Reflect.Field(owner, field).FieldType == typeof(int);
            var slider = new Slider();
            slider.Width = 230;
            slider.Minimum = min;
            slider.Maximum = max;
            slider.SmallChange = step;
            slider.LargeChange = step * 5;
            slider.TickFrequency = step;
            slider.IsSnapToTickEnabled = true;
            slider.VerticalAlignment = VerticalAlignment.Center;
            slider.Value = Convert.ToDouble(Reflect.Get(owner, field));

            var value = Label("");
            value.Margin = new Thickness(12, 0, 0, 0);
            value.Width = 70;
            Action show = delegate { value.Text = slider.Value.ToString(step < 1 ? "0.0" : "0") + unit; };
            show();

            slider.ValueChanged += delegate
            {
                show();
                if (_loading) return;
                if (isInt) Reflect.Set(owner, field, (int)Math.Round(slider.Value));
                else Reflect.Set(owner, field, slider.Value);
                if (extra != null) extra();
                Changed();
            };
            sp.Children.Add(slider);
            sp.Children.Add(value);
            Put(row, sp);
        }

        private void AddEnumCombo(StackPanel p, object owner, string label, string field, Type enumType, Action extra)
        {
            AddEnumCombo(p, owner, label, field, enumType, extra, null);
        }

        // "skip" lists enum names that should not be offered here.
        private void AddEnumCombo(StackPanel p, object owner, string label, string field, Type enumType, Action extra, string[] skip)
        {
            Grid row = AddRow(p, label);
            var combo = new ComboBox();
            combo.Width = 230;
            combo.HorizontalAlignment = HorizontalAlignment.Left;
            foreach (string n in Enum.GetNames(enumType))
            {
                if (skip != null && Array.IndexOf(skip, n) >= 0) continue;
                combo.Items.Add(n);
            }
            combo.SelectedItem = Reflect.Get(owner, field).ToString();
            combo.SelectionChanged += delegate
            {
                if (_loading || combo.SelectedItem == null) return;
                Reflect.Set(owner, field, Enum.Parse(enumType, (string)combo.SelectedItem));
                if (extra != null) extra();
                Changed();
            };
            Put(row, combo);
        }

        // A picture / sound file chooser: shows the file name, "Choose..." and "Remove".
        private void AddFilePicker(StackPanel p, object owner, string label, string field, string filter)
        {
            Grid row = AddRow(p, label);
            var sp = new WrapPanel();
            var name = Label("");
            name.Margin = new Thickness(0, 0, 10, 0);
            Action show = delegate
            {
                string path = (string)Reflect.Get(owner, field);
                name.Text = string.IsNullOrEmpty(path) ? "None" : System.IO.Path.GetFileName(path);
            };
            show();
            sp.Children.Add(name);
            sp.Children.Add(MakeButton("Choose...", delegate
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = filter;
                if (dlg.ShowDialog(this) == true)
                {
                    Reflect.Set(owner, field, dlg.FileName);
                    show();
                    Changed();
                }
            }));
            sp.Children.Add(MakeButton("Remove", delegate
            {
                Reflect.Set(owner, field, "");
                show();
                Changed();
            }));
            Put(row, sp);
        }

        private void AddFontCombo(StackPanel p, object owner, string label, string field)
        {
            Grid row = AddRow(p, label);
            var combo = new ComboBox();
            combo.Width = 260;
            combo.HorizontalAlignment = HorizontalAlignment.Left;
            combo.MaxDropDownHeight = 320;

            var names = new List<string>();
            foreach (FontFamily f in Fonts.SystemFontFamilies) names.Add(f.Source);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            string current = (string)Reflect.Get(owner, field);
            if (!names.Contains(current)) names.Insert(0, current);
            foreach (string n in names) combo.Items.Add(n);
            combo.SelectedItem = current;

            combo.SelectionChanged += delegate
            {
                if (_loading || combo.SelectedItem == null) return;
                Reflect.Set(owner, field, (string)combo.SelectedItem);
                Changed();
            };
            Put(row, combo);
        }

        private void AddColor(StackPanel p, object owner, string label, string field, Action extra)
        {
            Grid row = AddRow(p, label);
            var sp = new WrapPanel();

            var swatch = new Border();
            swatch.Width = 28;
            swatch.Height = 28;
            swatch.CornerRadius = new CornerRadius(7);
            swatch.BorderThickness = new Thickness(1);
            swatch.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
            swatch.Margin = new Thickness(0, 0, 8, 4);

            var hex = new TextBox();
            hex.Width = 96;
            hex.FontFamily = new FontFamily("Consolas");
            hex.Margin = new Thickness(0, 0, 10, 4);

            string initial = (string)Reflect.Get(owner, field);
            hex.Text = initial;
            Color c0;
            if (ColorUtil.TryParse(initial, out c0)) swatch.Background = new SolidColorBrush(c0);

            hex.TextChanged += delegate
            {
                Color c;
                if (ColorUtil.TryParse(hex.Text, out c))
                {
                    hex.Foreground = (Brush)FindResource("InkBrush");
                    swatch.Background = new SolidColorBrush(c);
                    if (_loading) return;
                    string t = hex.Text.Trim();
                    Reflect.Set(owner, field, t.StartsWith("#") ? t : "#" + t);
                    if (extra != null) extra();
                    Changed();
                }
                else
                {
                    hex.Foreground = Brushes.IndianRed;
                }
            };

            sp.Children.Add(swatch);
            sp.Children.Add(hex);
            foreach (string preset in Presets)
            {
                string hexValue = preset;
                var dot = new Border();
                dot.Width = 20;
                dot.Height = 20;
                dot.CornerRadius = new CornerRadius(10);
                dot.Margin = new Thickness(0, 4, 4, 4);
                dot.BorderThickness = new Thickness(1);
                dot.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
                dot.Cursor = Cursors.Hand;
                dot.Background = new SolidColorBrush(ColorUtil.Parse(hexValue, Colors.White));
                dot.MouseLeftButtonUp += delegate { hex.Text = hexValue; };
                sp.Children.Add(dot);
            }
            Put(row, sp);
        }
    }
}
