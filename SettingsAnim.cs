// Settings sections for animations and for the look of each state (v1.4.7).
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BetterClock
{
    public partial class SettingsWindow
    {
        private void BuildAnimSections(StackPanel page, ModeStyle st, OverlayMode mode)
        {
            // ---- show, hide and speed ----
            var size = new AnimatedSection("Size and animation", false);
            AddSlider(size.Body, st, "Overall opacity", "OverallOpacity", 10, 100, 5, " %", null);
            AddCheck(size.Body, st, "Animations on for this overlay", "AnimationsOn", null);
            AddEnumCombo(size.Body, st, "When it appears", "ShowAnimation", typeof(ShowAnimation), null);
            AddEnumCombo(size.Body, st, "When it disappears", "HideAnimation", typeof(HideAnim), null);
            AddSlider(size.Body, st, "Fade time", "FadeMs", 0, 1000, 20, " ms", null);
            AddSlider(size.Body, st, "Animation speed", "AnimSpeed", 25, 300, 5, " %", null);
            AddEnumCombo(size.Body, st, "Easing (how it speeds up and slows down)", "AnimEasing", typeof(EaseKind), null);
            size.Body.Children.Add(Hint("Auto for the disappear animation follows the appear animation. Turning animations off here also needs Settings > Animations to be on."));
            AddCopySection(size, mode);
            page.Children.Add(size);

            // ---- while it runs ----
            var run = new AnimatedSection("Animations while it runs", false);
            AddEnumCombo(run.Body, st, "When the numbers change", "DigitAnimation", typeof(DigitAnim), null);
            AddEnumCombo(run.Body, st, "Idle breathing", "Breathe", typeof(BreatheKind), null);
            AddColor(run.Body, st, "Breathing glow colour", "BreatheColor", null);
            AddSlider(run.Body, st, "Breathing glow size", "BreatheAmount", 3, 60, 1, " px", null);
            AddSlider(run.Body, st, "Breathing growth of the numbers", "BreatheSize", 1, 30, 1, " %", null);
            AddSlider(run.Body, st, "Seconds per breath", "BreatheSeconds", 1, 20, 1, " s", null);
            if (mode == OverlayMode.Countdown)
                AddEnumCombo(run.Body, st, "When it is almost out", "WarnAnimation", typeof(WarnAnim), null);
            AddCheck(run.Body, st, "Fade the colour between states", "ColorFade", null);
            AddSlider(run.Body, st, "Colour fade time", "ColorFadeMs", 50, 2000, 50, " ms", null);
            AddCheck(run.Body, st, "Animated gradient background", "AnimatedGradient", null);
            AddSlider(run.Body, st, "Seconds per turn", "GradientSeconds", 1, 60, 1, " s", null);
            run.Body.Children.Add(Hint("Idle breathing: Glow breathes the glow around the overlay, Size breathes the numbers. The animated gradient needs the Gradient background style."));
            AddCopySection(run, mode);
            page.Children.Add(run);
        }

        // ======================================================================== a look for each state

        private static string StateTitle(string key)
        {
            switch (key)
            {
                case "Running": return "Look while running";
                case "Paused": return "Look while paused";
                case "Low": return "Look when time is running low";
                case "Danger": return "Look when it is almost out";
                case "Finished": return "Look at zero";
                case "Overtime": return "Look in overtime";
                case "Target": return "Look when the target is reached";
                default: return "Look at night";
            }
        }

        private void BuildStateSections(StackPanel page, ModeStyle st, OverlayMode mode)
        {
            var keys = new List<string>();
            if (mode != OverlayMode.Clock)
            {
                keys.Add("Running");
                keys.Add("Paused");
            }
            if (mode == OverlayMode.Countdown)
            {
                keys.Add("Low");
                keys.Add("Danger");
                keys.Add("Finished");
                keys.Add("Overtime");
            }
            if (mode == OverlayMode.Timer) keys.Add("Target");
            keys.Add("Night");

            foreach (string key in keys) page.Children.Add(BuildStateSection(st, mode, key, keys));
        }

        private AnimatedSection BuildStateSection(ModeStyle st, OverlayMode mode, string key, List<string> all)
        {
            var sec = new AnimatedSection(StateTitle(key), false);
            StackPanel b = sec.Body;
            if (key == "Night")
            {
                AddCheck(b, st, "Use a night look", "NightOn", null);
                AddSlider(b, st, "Night starts at", "NightFrom", 0, 23, 1, " h", null);
                AddSlider(b, st, "Night ends at", "NightTo", 0, 23, 1, " h", null);
            }
            AddColor(b, st, "Background colour", key + "Background", null);
            AddColor(b, st, "Border colour", key + "BorderColor", null);
            AddOptSlider(b, st, "Border thickness", key + "BorderWidth", -1, 20, 1, " px");
            AddColor(b, st, "Glow colour", key + "GlowColor", null);
            AddOptSlider(b, st, "Glow size", key + "GlowSize", -1, 60, 1, " px");
            AddOptSlider(b, st, "Size of the overlay", key + "Scale", 0, 500, 5, " %");
            b.Children.Add(Hint("Empty colours and 'unchanged' keep the normal look. The colour of the numbers for each state is in the Text section."));

            string stateKey = key;
            var buttons = new WrapPanel();
            buttons.Children.Add(MakeButton("Preview this state", delegate { ShowPreviewState(mode, stateKey); }));
            buttons.Children.Add(MakeButton("Reset this state", delegate { ResetState(st, mode, stateKey); }));
            b.Children.Add(buttons);

            var copy = new WrapPanel();
            copy.Margin = new Thickness(0, 6, 0, 0);
            var combo = new ComboBox();
            combo.Width = 120;
            combo.Margin = new Thickness(0, 0, 8, 0);
            foreach (string other in all) if (other != key) combo.Items.Add(other);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            copy.Children.Add(Label("Copy the look of "));
            copy.Children.Add(combo);
            copy.Children.Add(MakeButton("Copy here", delegate
            {
                if (combo.SelectedItem != null) CopyState(st, mode, (string)combo.SelectedItem, stateKey);
            }));
            b.Children.Add(copy);
            return sec;
        }

        private void CopyState(ModeStyle st, OverlayMode mode, string from, string to)
        {
            foreach (string prop in StateLooks.Props) Reflect.Set(st, to + prop, Reflect.Get(st, from + prop));
            RefillMode(mode);
            Changed();
        }

        private void ResetState(ModeStyle st, OverlayMode mode, string key)
        {
            var fresh = new ModeStyle();
            foreach (string prop in StateLooks.Props) Reflect.Set(st, key + prop, Reflect.Get(fresh, key + prop));
            RefillMode(mode);
            Changed();
        }

        // A slider where the lowest value means "leave the normal look alone".
        private void AddOptSlider(StackPanel p, object owner, string label, string field, double min, double max, double step, string unit)
        {
            Track(p, owner, field);
            Grid row = AddRow(p, label);
            Tip(row, field);
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

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
            value.Width = 90;
            Action show = delegate { value.Text = slider.Value <= min ? "unchanged" : slider.Value.ToString("0") + unit; };
            show();

            slider.ValueChanged += delegate
            {
                show();
                if (_loading) return;
                Reflect.Set(owner, field, (int)Math.Round(slider.Value));
                Changed();
            };
            sp.Children.Add(slider);
            sp.Children.Add(value);
            Put(row, sp);
        }
    }
}
