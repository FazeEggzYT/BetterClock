// Better Clock - startup splash screen (Stage 2).
// Dark card, big logo emoji, status text bottom-left, percentage bottom-right,
// thin progress bar along the bottom edge (Unreal-launcher style).
// NOTE: written for the C# 5 compiler that ships with Windows (no ?. or $"" syntax).
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace BetterClock
{
    public class SplashWindow : Window
    {
        private const double CardW = 720;
        private const double CardH = 400;
        private const double Pad = 26;

        private readonly TextBlock _percentText;
        private readonly TextBlock _statusText;
        private readonly Border _bar;
        private readonly DispatcherTimer _timer;
        private double _target;
        private double _shown;

        public SplashWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            Width = CardW + Pad * 2;
            Height = CardH + Pad * 2;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = "Better Clock";
            Opacity = 0;

            // ---- card ----
            var bg = new LinearGradientBrush();
            bg.StartPoint = new Point(0, 0);
            bg.EndPoint = new Point(1, 1);
            bg.GradientStops.Add(new GradientStop(Color.FromRgb(0x0D, 0x0E, 0x1B), 0.0));
            bg.GradientStops.Add(new GradientStop(Color.FromRgb(0x19, 0x12, 0x30), 1.0));

            var card = new Border();
            card.Width = CardW;
            card.Height = CardH;
            card.Margin = new Thickness(Pad);
            card.CornerRadius = new CornerRadius(14);
            card.BorderThickness = new Thickness(1);
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2B, 0x2F, 0x52));
            card.Background = bg;
            card.ClipToBounds = true;
            var shadow = new DropShadowEffect();
            shadow.BlurRadius = 26;
            shadow.ShadowDepth = 0;
            shadow.Opacity = 0.65;
            shadow.Color = Colors.Black;
            card.Effect = shadow;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ---- centre logo ----
            var centre = new StackPanel();
            centre.VerticalAlignment = VerticalAlignment.Center;
            centre.HorizontalAlignment = HorizontalAlignment.Center;

            var logo = new TextBlock();
            // U+1F171 (B button) + variation selector, then U+1F550 (one o'clock)
            logo.Text = "\U0001F171\uFE0F  \U0001F550";
            logo.FontFamily = new FontFamily("Segoe UI Emoji");
            logo.FontSize = 86;
            logo.Foreground = Brushes.White;
            logo.HorizontalAlignment = HorizontalAlignment.Center;
            logo.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = new ScaleTransform(1, 1);
            logo.RenderTransform = scale;
            var pulse = new DoubleAnimation(1.0, 1.07, TimeSpan.FromMilliseconds(1100));
            pulse.AutoReverse = true;
            pulse.RepeatBehavior = RepeatBehavior.Forever;
            pulse.EasingFunction = new SineEase();
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
            centre.Children.Add(logo);

            var title = new TextBlock();
            title.Text = "B E T T E R   C L O C K";
            title.FontFamily = new FontFamily("Segoe UI");
            title.FontSize = 30;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = Brushes.White;
            title.Margin = new Thickness(0, 18, 0, 0);
            title.HorizontalAlignment = HorizontalAlignment.Center;
            centre.Children.Add(title);

            var sub = new TextBlock();
            sub.Text = "Clock  -  Timer  -  Countdown overlay for your games";
            sub.FontFamily = new FontFamily("Segoe UI");
            sub.FontSize = 13;
            sub.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x94, 0xB0));
            sub.Margin = new Thickness(0, 8, 0, 0);
            sub.HorizontalAlignment = HorizontalAlignment.Center;
            centre.Children.Add(sub);

            Grid.SetRow(centre, 0);
            grid.Children.Add(centre);

            // ---- status + percent row ----
            var info = new Grid();
            info.Margin = new Thickness(26, 0, 26, 12);
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusText = new TextBlock();
            _statusText.Text = "Starting...";
            _statusText.FontFamily = new FontFamily("Segoe UI");
            _statusText.FontSize = 13;
            _statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xA9, 0xAF, 0xC9));
            _statusText.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetColumn(_statusText, 0);
            info.Children.Add(_statusText);

            _percentText = new TextBlock();
            _percentText.Text = "0%";
            _percentText.FontFamily = new FontFamily("Segoe UI Semibold");
            _percentText.FontSize = 28;
            _percentText.Foreground = Brushes.White;
            _percentText.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetColumn(_percentText, 1);
            info.Children.Add(_percentText);

            Grid.SetRow(info, 1);
            grid.Children.Add(info);

            // ---- progress bar along the bottom ----
            var track = new Border();
            track.Height = 5;
            track.Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x40));
            _bar = new Border();
            _bar.Height = 5;
            _bar.Width = 0;
            _bar.HorizontalAlignment = HorizontalAlignment.Left;
            var barBrush = new LinearGradientBrush();
            barBrush.StartPoint = new Point(0, 0);
            barBrush.EndPoint = new Point(1, 0);
            barBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x4D, 0x7C, 0xFF), 0.0));
            barBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0xA8, 0x5C, 0xFF), 1.0));
            _bar.Background = barBrush;
            var trackGrid = new Grid();
            trackGrid.Children.Add(track);
            trackGrid.Children.Add(_bar);
            Grid.SetRow(trackGrid, 2);
            grid.Children.Add(trackGrid);

            card.Child = grid;
            Content = card;

            // ---- smooth progress animation ----
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += OnTick;
            _timer.Start();

            Loaded += delegate
            {
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260));
                BeginAnimation(OpacityProperty, fadeIn);
            };
        }

        public void SetStatus(string text)
        {
            _statusText.Text = text;
        }

        // Moves the bar towards this percentage (0-100). It glides, it doesn't jump.
        public void SetTarget(double percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            _target = percent;
        }

        public bool IsFull
        {
            get { return _shown >= 99.99; }
        }

        public async Task WaitUntilFull()
        {
            while (!IsFull)
            {
                await Task.Delay(30);
            }
        }

        public void FadeOutAndClose()
        {
            _timer.Stop();
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(380));
            fade.Completed += delegate { Close(); };
            BeginAnimation(OpacityProperty, fade);
        }

        private void OnTick(object sender, EventArgs e)
        {
            double diff = _target - _shown;
            if (diff <= 0.01)
            {
                _shown = _target;
            }
            else
            {
                double step = diff * 0.08;
                if (step < 0.25) step = 0.25;
                _shown += step;
                if (_shown > _target) _shown = _target;
            }
            _percentText.Text = ((int)Math.Floor(_shown + 0.0001)).ToString() + "%";
            _bar.Width = (CardW - 2) * (_shown / 100.0);
        }
    }
}
