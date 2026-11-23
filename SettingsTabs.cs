// The settings window (part 2): the tabs.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace BetterClock
{
    public partial class SettingsWindow
    {
        private void BuildAllTabs()
        {
            BuildModeTab(OverlayMode.Clock);
            BuildModeTab(OverlayMode.Timer);
            BuildModeTab(OverlayMode.Countdown);
            BuildPresetsTab();
            BuildGamesTab();
            BuildKeysTab();
            BuildSettingsTab();
            BuildChangelogTab();
            BuildAboutTab();
            if (_tabs.Items.Count > 0 && _tabs.SelectedIndex < 0) _tabs.SelectedIndex = 0;
        }

        // ======================================================================== Clock / Timer / Countdown

        private void BuildModeTab(OverlayMode mode)
        {
            string name = mode.ToString();
            string sub;
            if (mode == OverlayMode.Clock) sub = "The clock overlay. Choose where it sits and exactly how it looks.";
            else if (mode == OverlayMode.Timer) sub = "A stopwatch that counts up in seconds. It has its own look and its own place on screen.";
            else sub = "Counts down to zero, then flashes and plays your sound. Own look, own place.";

            StackPanel page = NewTab(name, name, sub);
            _modeHosts[mode] = page;
            FillModeTab(mode, page);
        }

        private void FillModeTab(OverlayMode mode, StackPanel page)
        {
            ModeStyle st = S.StyleFor(mode);

            AddSearchBox(page);

            // ---- tools ----
            var tools = new WrapPanel();
            tools.Margin = new Thickness(0, 0, 0, 12);
            tools.Children.Add(MakeButton("Show it on screen now", delegate { AppHost.Controller.ShowMode(mode); }));
            tools.Children.Add(MakeButton("Copy this look to the other modes", delegate { CopyLook(mode); }));
            tools.Children.Add(MakeButton("Reset this look", delegate { ResetLook(mode); }));
            page.Children.Add(tools);
            AddSlider(page, st, "Overall size", "Scale", 25, 500, 5, " %", null);

            // ---- behaviour ----
            AnimatedSection beh = new AnimatedSection("Behaviour", true);
            if (mode == OverlayMode.Clock)
            {
                AddCheck(beh.Body, S, "24-hour time", "Clock24Hour", null);
                AddCheck(beh.Body, S, "Show seconds", "ClockShowSeconds", null);
                AddCheck(beh.Body, S, "Keep the clock on screen until I hide it", "ClockHoldOpen", null);
                AddSlider(beh.Body, S, "Show for", "ClockShowForSeconds", 1, 30, 0.5, " s", null);
                beh.Body.Children.Add(Hint("\"Show for\" is how long the clock stays when you press its key (ignored if kept on screen)."));
            }
            else if (mode == OverlayMode.Timer)
            {
                AddCheck(beh.Body, S, "Keep counting while the timer is hidden", "TimerKeepRunningHidden", null);
                AddCheck(beh.Body, S, "Target: turn green when this time is reached", "TimerTargetEnabled", null);
                AddNumberBox(beh.Body, S, "Target time", "TimerTargetSeconds", 1, 359999, "seconds");
                AddCheck(beh.Body, S, "Chime when the target is reached", "TimerTargetBeep", null);
                beh.Body.Children.Add(Hint("Target time is in seconds (300 = 5 minutes). With a progress ring or bar on, it fills up towards the target."));
                beh.Body.Children.Add(Hint("Start / stop and Reset keys are in the Keys tab. The timer counts up in seconds."));
            }
            else
            {
                BuildCountdownLength(beh.Body);
                AddCheck(beh.Body, S, "Keep counting while the countdown is hidden", "CountdownKeepRunningHidden", null);
                AddCheck(beh.Body, S, "Show it automatically when it reaches zero", "CountdownAutoShowAtZero", null);
                AddCheck(beh.Body, S, "Flash at zero", "CountdownFlash", null);
                AddCheck(beh.Body, S, "Play a sound at zero", "CountdownSoundEnabled", null);
                BuildSoundPicker(beh.Body);
                AddSlider(beh.Body, S, "Sound volume", "CountdownSoundVolume", 0, 100, 5, " %", null);
                AddSlider(beh.Body, S, "Repeat sound", "CountdownSoundRepeat", 1, 10, 1, " x", null);
            }
            page.Children.Add(beh);

            // ---- clock extras: alarm, world clocks, dates, session, breaks ----
            if (mode == OverlayMode.Clock)
            {
                AnimatedSection ex = new AnimatedSection("Alarm, world clocks, dates and breaks", false);
                ex.Body.Children.Add(Hint("These show on the clock overlay. The extra lines only appear while the clock is on screen, so tick \"Keep the clock on screen\" above to see them all the time."));

                AddCheck(ex.Body, S, "Alarm clock", "AlarmEnabled", null);
                AddText(ex.Body, S, "Alarm time (24-hour)", "AlarmTime", 90);
                AddText(ex.Body, S, "Alarm message", "AlarmLabel", 220);
                AddCheck(ex.Body, S, "Ring every day", "AlarmRepeatDaily", null);
                var alarmRow = new WrapPanel();
                alarmRow.Margin = new Thickness(0, 4, 0, 4);
                alarmRow.Children.Add(MakeButton("Test alarm", delegate { AppHost.Controller.TestAlarm(); }));
                alarmRow.Children.Add(MakeButton("Stop alarm", delegate { AppHost.Controller.StopAlarm(); }));
                ex.Body.Children.Add(alarmRow);
                ex.Body.Children.Add(Hint("Write the time like 07:30. The alarm shows the clock and plays the countdown sound (set it in the Countdown tab). The clock key or Stop alarm silences it."));

                Grid wrow = AddRow(ex.Body, "World clocks");
                var wbox = new TextBox();
                wbox.Width = 380;
                wbox.HorizontalAlignment = HorizontalAlignment.Left;
                wbox.Text = S.WorldClocks ?? "";
                wbox.TextChanged += delegate
                {
                    if (_loading) return;
                    S.WorldClocks = wbox.Text;
                    Changed();
                };
                Put(wrow, wbox);

                Grid arow = AddRow(ex.Body, "Add a city");
                var asp = new WrapPanel();
                var cityBox = new ComboBox();
                cityBox.Width = 300;
                var zones = new List<TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones());
                foreach (TimeZoneInfo z in zones) cityBox.Items.Add(z.DisplayName);
                asp.Children.Add(cityBox);
                asp.Children.Add(MakeButton("Add", delegate
                {
                    int i = cityBox.SelectedIndex;
                    if (i < 0) return;
                    string dn = zones[i].DisplayName;
                    int cp = dn.IndexOf(')');
                    string nm = cp >= 0 ? dn.Substring(cp + 1).Trim() : dn;
                    int cm = nm.IndexOf(',');
                    if (cm > 0) nm = nm.Substring(0, cm).Trim();
                    nm = nm.Replace(";", "").Replace("|", "");
                    string add = nm + "|" + zones[i].Id;
                    wbox.Text = string.IsNullOrEmpty(wbox.Text) ? add : wbox.Text + ";" + add;
                }));
                asp.Children.Add(MakeButton("Clear", delegate { wbox.Text = ""; }));
                Put(arow, asp);
                ex.Body.Children.Add(Hint("Pick a place and press Add. Each one shows as a line under the clock. You can rename a place by editing the text in the box (the part before the | is the name)."));

                AddCheck(ex.Body, S, "Countdown to a date and time", "DateCountdownEnabled", null);
                AddText(ex.Body, S, "  What it is", "DateCountdownName", 160);
                AddText(ex.Body, S, "  Date and time", "DateCountdownTarget", 160);
                ex.Body.Children.Add(Hint("Write the date like 2026-12-25 18:00 (year-month-day hours:minutes)."));

                AddCheck(ex.Body, S, "Days since a date", "DaysSinceEnabled", null);
                AddText(ex.Body, S, "  What it is", "DaysSinceName", 160);
                AddText(ex.Body, S, "  Date", "DaysSinceDate", 160);

                AddCheck(ex.Body, S, "Show how long you have been playing (session)", "SessionShow", null);
                var sessRow = new WrapPanel();
                sessRow.Margin = new Thickness(0, 4, 0, 4);
                sessRow.Children.Add(MakeButton("Restart session", delegate { AppHost.Controller.ResetSession(); }));
                ex.Body.Children.Add(sessRow);
                ex.Body.Children.Add(Hint("The session starts when Better Clock starts."));

                AddCheck(ex.Body, S, "Break reminder", "BreakReminderEnabled", null);
                AddNumberBox(ex.Body, S, "  Remind me every", "BreakEveryMinutes", 1, 600, "minutes");
                ex.Body.Children.Add(Hint("Shows \"Time for a break!\" on the clock with a chime. It counts from when you turn it on or restart the session."));
                page.Children.Add(ex);
            }

            // ---- timer results history ----
            if (mode == OverlayMode.Timer)
            {
                AnimatedSection hist = new AnimatedSection("Results history", false);
                var histText = Label("");
                histText.Margin = new Thickness(0, 4, 0, 6);
                Action fill = delegate
                {
                    string tx = TimerHistory.Text(15);
                    histText.Text = tx.Length == 0 ? "No results yet. When you press Reset on a timer that has run, its time is saved here." : tx;
                };
                fill();
                TimerHistory.Changed += fill;
                hist.Body.Children.Add(histText);
                var histRow = new WrapPanel();
                histRow.Children.Add(MakeButton("Refresh", delegate { fill(); }));
                histRow.Children.Add(MakeButton("Clear history", delegate { TimerHistory.Clear(); }));
                hist.Body.Children.Add(histRow);
                page.Children.Add(hist);
            }

            // ---- countdown extras ----
            if (mode == OverlayMode.Countdown)
            {
                AnimatedSection plan = new AnimatedSection("Chains, Pomodoro and warnings", false);
                AddEnumCombo(plan.Body, S, "Countdown type", "CountdownPlan", typeof(CountdownPlanKind), null);
                plan.Body.Children.Add(Hint("Single = the plain countdown above. Chain = the steps below, one after another. Pomodoro = work and break cycles."));
                AddText(plan.Body, S, "Chain steps", "CountdownChain", 380);
                plan.Body.Children.Add(Hint("Time, then a name, separated by ; for example: 25:00 Work;5:00 Break;90 Stretch. A plain number means minutes."));
                AddCheck(plan.Body, S, "Start the chain again when it ends", "CountdownChainLoop", null);
                AddNumberBox(plan.Body, S, "Pomodoro: work", "PomoWorkMin", 1, 180, "minutes");
                AddNumberBox(plan.Body, S, "Pomodoro: short break", "PomoShortMin", 1, 60, "minutes");
                AddNumberBox(plan.Body, S, "Pomodoro: long break", "PomoLongMin", 1, 120, "minutes");
                AddNumberBox(plan.Body, S, "Pomodoro: rounds", "PomoRounds", 1, 12, "rounds");
                plan.Body.Children.Add(Hint("Pomodoro repeats forever: work, short break, work... and a long break after the last round."));
                AddCheck(plan.Body, S, "Keep counting up after zero (overtime, +00:05)", "CountdownOvertime", null);
                AddCheck(plan.Body, S, "Colour changes as time runs low", "CountdownLowColors", null);
                AddNumberBox(plan.Body, S, "Turns amber at", "CountdownAmberAt", 1, 3599, "seconds left");
                AddNumberBox(plan.Body, S, "Turns red at", "CountdownRedAt", 1, 3599, "seconds left");
                AddCheck(plan.Body, S, "Beep as the end gets close", "CountdownWarnBeeps", null);
                AddText(plan.Body, S, "Beep at (seconds left)", "CountdownWarnList", 220);
                var testRow = new WrapPanel();
                testRow.Margin = new Thickness(0, 6, 0, 0);
                testRow.Children.Add(MakeButton("Test warning beep", delegate { SoundService.PlayWarning(); }));
                testRow.Children.Add(MakeButton("Test step chime", delegate { SoundService.PlayChime(); }));
                plan.Body.Children.Add(testRow);
                page.Children.Add(plan);
            }

            // ---- position ----
            AnimatedSection pos = new AnimatedSection("Position and monitor", true);
            AddPositionGrid(pos.Body, st, mode);
            AddSlider(pos.Body, st, "Distance from edge", "EdgeMargin", 0, 400, 2, " px", null);
            AddMonitorPicker(pos.Body, st);

            _customChecks[mode] = AddCheck(pos.Body, st, "Use an exact position instead of the grid", "UseCustomPosition", null);
            AddNumberBox(pos.Body, st, "X (from the left)", "CustomX", 0, 20000, "px");
            AddNumberBox(pos.Body, st, "Y (from the top)", "CustomY", 0, 20000, "px");
            var moveRow = new WrapPanel();
            moveRow.Margin = new Thickness(0, 6, 0, 0);
            moveRow.Children.Add(MakeButton("Move with the mouse", delegate { AppHost.SetMoveMode(true); }));
            moveRow.Children.Add(MakeButton("Lock (stop moving)", delegate { AppHost.SetMoveMode(false); }));
            pos.Body.Children.Add(moveRow);
            pos.Body.Children.Add(Hint("Move mode shows all three overlays and lets you drag them anywhere, even onto another monitor. Lock puts them back to click-through."));
            page.Children.Add(pos);

            // ---- text ----
            AnimatedSection txt = new AnimatedSection("Text", false);
            AddFontCombo(txt.Body, st, "Font", "FontFamily");
            AddSlider(txt.Body, st, "Size", "FontSize", 12, 200, 1, " px", null);
            AddCheck(txt.Body, st, "Bold", "FontBold", null);
            AddCheck(txt.Body, st, "Italic", "FontItalic", null);
            AddColor(txt.Body, st, "Text colour", "TextColor", null);
            if (mode != OverlayMode.Clock)
            {
                AddColor(txt.Body, st, "While running", "RunningColor", null);
                AddColor(txt.Body, st, "While paused", "PausedColor", null);
            }
            if (mode == OverlayMode.Countdown) AddColor(txt.Body, st, "At zero", "FinishedColor", null);
            if (mode == OverlayMode.Countdown) AddColor(txt.Body, st, "Running low (amber)", "WarnColor", null);
            if (mode == OverlayMode.Countdown) AddColor(txt.Body, st, "Almost out (red)", "DangerColor", null);
            if (mode == OverlayMode.Timer) AddColor(txt.Body, st, "Target reached", "TargetColor", null);
            page.Children.Add(txt);

            // ---- number style ----
            AnimatedSection digits = new AnimatedSection("Number style", false);
            AddEnumCombo(digits.Body, st, "Style", "DigitStyle", typeof(DigitStyle), null,
                mode == OverlayMode.Clock ? null : new string[] { "Analog" });
            digits.Body.Children.Add(Hint(mode == OverlayMode.Clock
                ? "Normal, Flip (cards), Led (seven-segment), Terminal (retro) or Analog (round clock face)."
                : "Normal, Flip (cards), Led (seven-segment) or Terminal (retro)."));
            AddCheck(digits.Body, st, "Blinking colon", "BlinkColon", null);
            page.Children.Add(digits);

            BuildEffectSections(page, st, mode);

            // ---- extra text, date, icon ----
            AnimatedSection extra = new AnimatedSection("Extra text, date and icon", false);
            AddText(extra.Body, st, "Text above", "TextAbove", 240);
            AddText(extra.Body, st, "Text below", "TextBelow", 240);
            AddSlider(extra.Body, st, "Extra text size", "ExtraTextSize", 8, 80, 1, " px", null);
            AddColor(extra.Body, st, "Extra text colour", "ExtraTextColor", null);
            if (mode == OverlayMode.Clock)
            {
                AddCheck(extra.Body, st, "Show the day name (Saturday)", "ShowDayName", null);
                AddCheck(extra.Body, st, "Show the date (21 Nov 2026)", "ShowDate", null);
            }
            AddEnumCombo(extra.Body, st, "Icon", "Icon", typeof(OverlayIcon), null);
            AddFilePicker(extra.Body, st, "Own icon picture", "IconPath", "Pictures|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*");
            extra.Body.Children.Add(Hint("To use your own picture as the icon, set Icon to Custom."));
            AddSlider(extra.Body, st, "Icon size", "IconSize", 12, 150, 2, " px", null);
            AddCheck(extra.Body, st, "Icon on the right side", "IconOnRight", null);
            page.Children.Add(extra);

            // ---- label ----
            AnimatedSection lab = new AnimatedSection("Label above the numbers", false);
            AddCheck(lab.Body, st, "Show a label", "ShowLabel", null);
            AddText(lab.Body, st, "Label text", "LabelText", 220);
            AddSlider(lab.Body, st, "Label size", "LabelSize", 6, 60, 1, " px", null);
            AddColor(lab.Body, st, "Label colour", "LabelColor", null);
            page.Children.Add(lab);

            BuildPartSections(page, st, mode);

            // ---- background ----
            AnimatedSection bg = new AnimatedSection("Background", false);
            AddEnumCombo(bg.Body, st, "Style", "BackgroundStyle", typeof(BackgroundStyle), null);
            AddColor(bg.Body, st, "Colour", "BackgroundColor", null);
            AddColor(bg.Body, st, "Second colour (gradient)", "BackgroundColor2", null);
            AddSlider(bg.Body, st, "Gradient angle", "GradientAngle", 0, 360, 5, " deg", null);
            AddSlider(bg.Body, st, "Opacity", "BackgroundOpacity", 0, 100, 5, " %", null);
            page.Children.Add(bg);

            // ---- picture behind ----
            AnimatedSection pic = new AnimatedSection("Picture behind the numbers", false);
            AddFilePicker(pic.Body, st, "Picture", "BackgroundImagePath", "Pictures|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*");
            AddSlider(pic.Body, st, "Picture opacity", "BackgroundImageOpacity", 0, 100, 5, " %", null);
            AddEnumCombo(pic.Body, st, "Picture fit", "BackgroundImageFit", typeof(ImageFit), null);
            pic.Body.Children.Add(Hint("Cover fills the whole pill, Fit shows all of it, Stretch squeezes it to the pill."));
            page.Children.Add(pic);

            // ---- shape ----
            AnimatedSection shape = new AnimatedSection("Shape, border and glow", false);
            AddSlider(shape.Body, st, "Corner roundness", "CornerRadius", 0, 100, 1, " px", null);
            AddSlider(shape.Body, st, "Padding left / right", "PaddingH", 0, 120, 1, " px", null);
            AddSlider(shape.Body, st, "Padding top / bottom", "PaddingV", 0, 80, 1, " px", null);
            AddColor(shape.Body, st, "Border colour", "BorderColor", null);
            AddSlider(shape.Body, st, "Border thickness", "BorderThickness", 0, 12, 1, " px", null);
            AddCheck(shape.Body, st, "Glow / shadow around it", "ShadowEnabled", null);
            AddSlider(shape.Body, st, "Glow size", "ShadowBlur", 0, 60, 1, " px", null);
            page.Children.Add(shape);

            // ---- size & animation ----
            AnimatedSection size = new AnimatedSection("Size and animation", false);
            AddSlider(size.Body, st, "Overall opacity", "OverallOpacity", 10, 100, 5, " %", null);
            AddEnumCombo(size.Body, st, "When it appears", "ShowAnimation", typeof(ShowAnimation), null);
            AddSlider(size.Body, st, "Fade time", "FadeMs", 0, 1000, 20, " ms", null);
            page.Children.Add(size);

            // ---- progress ring / bar and pulse ----
            if (mode != OverlayMode.Clock)
            {
                AnimatedSection prog = new AnimatedSection(mode == OverlayMode.Countdown ? "Progress ring / bar and pulse" : "Progress ring / bar", false);
                AddEnumCombo(prog.Body, st, "Show progress as", "ProgressStyle", typeof(ProgressStyle), null);
                AddSlider(prog.Body, st, "Thickness", "ProgressThickness", 2, 30, 1, " px", null);
                AddSlider(prog.Body, st, "Ring size", "ProgressSize", 40, 300, 5, " %", null);
                if (mode == OverlayMode.Countdown)
                {
                    prog.Body.Children.Add(Hint("Bar = a thin bar under the numbers. Ring = a circle beside them. Both empty as time runs out."));
                    AddSlider(prog.Body, st, "Glow pulses in the last", "PulseSeconds", 0, 60, 1, " s", null);
                    AddColor(prog.Body, st, "Pulse colour", "PulseColor", null);
                    prog.Body.Children.Add(Hint("0 turns the pulse off."));
                }
                else
                {
                    prog.Body.Children.Add(Hint("Needs a timer target (Behaviour section). The bar or ring fills up towards the target."));
                }
                page.Children.Add(prog);
            }

            AddResetButtons(page, mode);
        }

        // ======================================================================== presets and themes

        private void BuildPresetsTab()
        {
            StackPanel page = NewTab("Presets", "Presets and themes", "One-click looks for all three overlays, and your own saved looks. Positions and label texts are not changed.");

            // ---- theme gallery ----
            AnimatedSection gal = new AnimatedSection("Theme gallery", true);
            gal.Body.Children.Add(Hint("Click a theme to put it on the Clock, Timer and Countdown. Watch the preview on the right. \"Default\" goes back to the normal look."));
            var wrap = new WrapPanel();
            foreach (string themeName in LookPresets.BuiltInNames)
            {
                string chosen = themeName;
                Button b = MakeButton(chosen, delegate
                {
                    LookPresets.ApplyBuiltIn(chosen);
                    RefillAllModes();
                    Changed();
                });
                b.Padding = new Thickness(16, 7, 16, 7);
                b.Margin = new Thickness(0, 0, 8, 8);
                wrap.Children.Add(b);
            }
            gal.Body.Children.Add(wrap);
            page.Children.Add(gal);

            // ---- your presets ----
            AnimatedSection mine = new AnimatedSection("Your presets", true);
            mine.Body.Children.Add(Hint("Saves the current look of all three overlays under a name, so you can come back to it. They also show in the tray menu."));

            var combo = new ComboBox();
            combo.Width = 240;
            Action refresh = delegate
            {
                string keep = combo.SelectedItem as string;
                combo.Items.Clear();
                foreach (string n in LookPresets.Names()) combo.Items.Add(n);
                if (keep != null && combo.Items.Contains(keep)) combo.SelectedItem = keep;
            };
            refresh();

            Grid saveRow = AddRow(mine.Body, "Save the current look as");
            var saveSp = new WrapPanel();
            var nameBox = new TextBox();
            nameBox.Width = 200;
            nameBox.Margin = new Thickness(0, 0, 8, 0);
            saveSp.Children.Add(nameBox);
            saveSp.Children.Add(MakeButton("Save", delegate
            {
                string saved = LookPresets.Save(nameBox.Text);
                if (saved == null)
                {
                    MessageBox.Show(this, "Type a name first (letters and numbers).", Program.AppName);
                    return;
                }
                nameBox.Text = "";
                refresh();
                combo.SelectedItem = saved;
            }));
            Put(saveRow, saveSp);

            Grid pickRow = AddRow(mine.Body, "Saved presets");
            var pickSp = new WrapPanel();
            combo.Margin = new Thickness(0, 0, 8, 0);
            pickSp.Children.Add(combo);
            pickSp.Children.Add(MakeButton("Apply", delegate
            {
                string sel = combo.SelectedItem as string;
                if (sel == null) return;
                if (LookPresets.ApplySaved(sel))
                {
                    RefillAllModes();
                    Changed();
                }
            }));
            pickSp.Children.Add(MakeButton("Delete", delegate
            {
                string sel = combo.SelectedItem as string;
                if (sel == null) return;
                if (MessageBox.Show(this, "Delete the preset \"" + sel + "\"?", Program.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                LookPresets.Delete(sel);
                combo.SelectedItem = null;
                refresh();
            }));
            Put(pickRow, pickSp);
            mine.Body.Children.Add(Hint("Presets are saved as small files in the presets folder next to your settings, so you can also copy them to a friend."));
            var openRow = new WrapPanel();
            openRow.Children.Add(MakeButton("Open the presets folder", delegate
            {
                try
                {
                    Directory.CreateDirectory(LookPresets.Folder);
                    Process.Start(LookPresets.Folder);
                }
                catch (Exception) { }
            }));
            mine.Body.Children.Add(openRow);
            page.Children.Add(mine);
        }

        // ======================================================================== search and reset

        // A search box at the top of a page: hides every setting that does not match.
        private void AddSearchBox(StackPanel page)
        {
            var row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.Margin = new Thickness(0, 0, 0, 12);
            row.Children.Add(Label("Search settings:  "));

            var box = new TextBox();
            box.Width = 300;
            box.ToolTip = "Type a word, like colour, size, sound or shadow";
            box.TextChanged += delegate
            {
                if (_loading) return;
                FilterPage(page, box.Text);
            };
            row.Children.Add(box);
            row.Children.Add(MakeButton("Clear", delegate { box.Text = ""; }));
            ((Button)row.Children[2]).Margin = new Thickness(8, 0, 0, 0);
            page.Children.Add(row);
        }

        private void FilterPage(StackPanel page, string query)
        {
            string q = (query ?? "").Trim().ToLowerInvariant();
            foreach (UIElement child in page.Children)
            {
                AnimatedSection sec = child as AnimatedSection;
                if (sec == null) continue;

                if (q.Length == 0)
                {
                    sec.Visibility = Visibility.Visible;
                    foreach (UIElement item in sec.Body.Children) item.Visibility = Visibility.Visible;
                    continue;
                }

                bool titleHit = sec.Title.ToLowerInvariant().Contains(q);
                bool any = titleHit;
                foreach (UIElement item in sec.Body.Children)
                {
                    bool hit = titleHit || CollectText(item).ToLowerInvariant().Contains(q);
                    item.Visibility = hit ? Visibility.Visible : Visibility.Collapsed;
                    if (hit) any = true;
                }
                sec.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
                if (any) sec.SetOpen(true, false);
            }
        }

        private static string CollectText(DependencyObject d)
        {
            var sb = new StringBuilder();
            AppendText(d, sb);
            return sb.ToString();
        }

        private static void AppendText(DependencyObject d, StringBuilder sb)
        {
            TextBlock tb = d as TextBlock;
            if (tb != null) sb.Append(tb.Text).Append(' ');

            ContentControl cc = d as ContentControl;
            if (cc != null && cc.Content is string) sb.Append((string)cc.Content).Append(' ');

            FrameworkElement fe = d as FrameworkElement;
            if (fe != null && fe.ToolTip is string) sb.Append((string)fe.ToolTip).Append(' ');

            foreach (object child in LogicalTreeHelper.GetChildren(d))
            {
                DependencyObject c = child as DependencyObject;
                if (c != null) AppendText(c, sb);
            }
        }

        // Adds "Reset this section" at the bottom of every section that has settings.
        private void AddResetButtons(StackPanel page, OverlayMode mode)
        {
            foreach (UIElement child in page.Children)
            {
                AnimatedSection sec = child as AnimatedSection;
                if (sec == null) continue;
                List<KeyValuePair<object, string>> fields;
                if (!_secFields.TryGetValue(sec.Body, out fields) || fields.Count == 0) continue;

                List<KeyValuePair<object, string>> mine = fields;
                Button b = MakeButton("Reset this section", delegate { ResetFields(mine, mode); });
                b.Margin = new Thickness(0, 10, 0, 0);
                b.HorizontalAlignment = HorizontalAlignment.Left;
                b.ToolTip = "Put every setting in this section back to its normal value.";
                sec.Body.Children.Add(b);
            }
        }

        private void ResetFields(List<KeyValuePair<object, string>> fields, OverlayMode mode)
        {
            var fresh = new AppSettings();
            foreach (KeyValuePair<object, string> kv in fields)
            {
                object source = null;
                if (kv.Key is ModeStyle) source = fresh.StyleFor(mode);
                else if (kv.Key is AppSettings) source = fresh;
                if (source == null) continue;
                Reflect.Set(kv.Key, kv.Value, Reflect.Get(source, kv.Value));
            }
            RefillMode(mode);
            Changed();
        }

        private void RefillMode(OverlayMode mode)
        {
            StackPanel host;
            if (!_modeHosts.TryGetValue(mode, out host)) return;
            bool was = _loading;
            _loading = true;
            // keep the title + subtitle (first two children), rebuild the rest
            while (host.Children.Count > 2) host.Children.RemoveAt(2);
            FillModeTab(mode, host);
            _loading = was;
        }

        private void CopyLook(OverlayMode from)
        {
            ModeStyle src = S.StyleFor(from);
            foreach (OverlayMode m in new OverlayMode[] { OverlayMode.Clock, OverlayMode.Timer, OverlayMode.Countdown })
            {
                if (m == from) continue;
                S.StyleFor(m).CopyFrom(src, true);
                RefillMode(m);
            }
            Changed();
        }

        private void ResetLook(OverlayMode mode)
        {
            ModeStyle fresh = new AppSettings().StyleFor(mode);
            S.StyleFor(mode).CopyFrom(fresh, true);
            RefillMode(mode);
            Changed();
        }

        // ---- countdown length: hours / minutes / seconds ----
        private void BuildCountdownLength(StackPanel p)
        {
            Grid row = AddRow(p, "Countdown from");
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            int total = S.CountdownTotalSeconds;
            TextBox h = TimeBox(total / 3600);
            TextBox m = TimeBox((total % 3600) / 60);
            TextBox s = TimeBox(total % 60);
            Action update = delegate
            {
                if (_loading) return;
                int hv = ParseBox(h), mv = ParseBox(m), sv = ParseBox(s);
                int t = hv * 3600 + mv * 60 + sv;
                if (t < 1) t = 1;
                if (t > 359999) t = 359999;
                S.CountdownTotalSeconds = t;
                Changed();
            };
            h.TextChanged += delegate { update(); };
            m.TextChanged += delegate { update(); };
            s.TextChanged += delegate { update(); };
            sp.Children.Add(h); sp.Children.Add(Label(" h   "));
            sp.Children.Add(m); sp.Children.Add(Label(" m   "));
            sp.Children.Add(s); sp.Children.Add(Label(" s"));
            Put(row, sp);
        }

        private static TextBox TimeBox(int value)
        {
            var t = new TextBox();
            t.Width = 54;
            t.Text = value.ToString();
            t.HorizontalContentAlignment = HorizontalAlignment.Center;
            return t;
        }

        private static int ParseBox(TextBox t)
        {
            int v;
            if (!int.TryParse(t.Text.Trim(), out v)) return 0;
            return v < 0 ? 0 : (v > 99 ? 99 : v);
        }

        // ---- countdown sound ----
        private void BuildSoundPicker(StackPanel p)
        {
            Grid row = AddRow(p, "Alarm sound");
            var sp = new WrapPanel();
            var name = Label("");
            name.Margin = new Thickness(0, 0, 10, 0);
            Action show = delegate
            {
                string path = S.CountdownSoundPath;
                name.Text = string.IsNullOrEmpty(path) ? "Built-in beeps" : System.IO.Path.GetFileName(path);
            };
            show();

            sp.Children.Add(name);
            sp.Children.Add(MakeButton("Choose file...", delegate
            {
                var dlg = new OpenFileDialog();
                dlg.Title = "Choose a sound for the countdown";
                dlg.Filter = "Sound files|*.wav;*.mp3;*.wma;*.m4a;*.aac|All files|*.*";
                if (dlg.ShowDialog(this) == true)
                {
                    S.CountdownSoundPath = dlg.FileName;
                    show();
                    Changed();
                }
            }));
            sp.Children.Add(MakeButton("Use beeps", delegate { S.CountdownSoundPath = ""; show(); Changed(); }));
            sp.Children.Add(MakeButton("Test", delegate { SoundService.PlayCountdownAlarm(S, true); }));
            sp.Children.Add(MakeButton("Stop", delegate { SoundService.Stop(); }));
            Put(row, sp);
        }

        // ---- 3x3 position grid ----
        private void AddPositionGrid(StackPanel p, ModeStyle st, OverlayMode mode)
        {
            Grid row = AddRow(p, "Where on screen");
            var grid = new UniformGrid();
            grid.Rows = 3;
            grid.Columns = 3;
            grid.Width = 156;
            grid.Height = 96;
            grid.HorizontalAlignment = HorizontalAlignment.Left;

            var buttons = new ToggleButton[9];
            for (int i = 0; i < 9; i++)
            {
                int index = i;
                var b = new ToggleButton();
                b.Margin = new Thickness(2);
                b.IsChecked = (int)st.Position == i;
                b.ToolTip = ((OverlayPosition)i).ToString();
                b.Click += delegate
                {
                    if (_loading) return;
                    st.Position = (OverlayPosition)index;
                    st.UseCustomPosition = false;
                    CheckBox cc;
                    if (_customChecks.TryGetValue(mode, out cc)) cc.IsChecked = false;
                    for (int k = 0; k < 9; k++) buttons[k].IsChecked = (k == index);
                    Changed();
                };
                buttons[i] = b;
                grid.Children.Add(b);
            }
            Put(row, grid);
        }

        private void AddMonitorPicker(StackPanel p, ModeStyle st)
        {
            Grid row = AddRow(p, "Monitor");
            var combo = new ComboBox();
            combo.Width = 330;
            combo.HorizontalAlignment = HorizontalAlignment.Left;
            combo.Items.Add("The monitor the game is on (recommended)");
            combo.Items.Add("Primary monitor");
            combo.Items.Add("All monitors (one overlay on each)");
            List<ScreenInfo> screens = MonitorHelper.All();
            for (int i = 0; i < screens.Count; i++)
            {
                combo.Items.Add(string.Format("Monitor {0}  ({1} x {2}){3}", i + 1,
                    screens[i].Width, screens[i].Height, screens[i].IsPrimary ? "  - primary" : ""));
            }
            if (st.MonitorMode == MonitorMode.GameWindow) combo.SelectedIndex = 0;
            else if (st.MonitorMode == MonitorMode.Primary) combo.SelectedIndex = 1;
            else if (st.MonitorMode == MonitorMode.All) combo.SelectedIndex = 2;
            else combo.SelectedIndex = Math.Min(3 + st.MonitorIndex, combo.Items.Count - 1);

            combo.SelectionChanged += delegate
            {
                if (_loading) return;
                int sel = combo.SelectedIndex;
                if (sel == 0) st.MonitorMode = MonitorMode.GameWindow;
                else if (sel == 1) st.MonitorMode = MonitorMode.Primary;
                else if (sel == 2) st.MonitorMode = MonitorMode.All;
                else { st.MonitorMode = MonitorMode.Specific; st.MonitorIndex = sel - 3; }
                Changed();
            };
            Put(row, combo);
        }


        // ======================================================================== Games

        private void BuildGamesTab()
        {
            _gamesPage = NewTab("Games", "Games", "Give a game its own looks. They switch on by themselves while that program is running.");
            FillGames();
        }

        private void RefillGames()
        {
            if (_gamesPage == null) return;
            bool was = _loading;
            _loading = true;
            while (_gamesPage.Children.Count > 2) _gamesPage.Children.RemoveAt(2);
            FillGames();
            _loading = was;
        }

        private void FillGames()
        {
            StackPanel page = _gamesPage;
            GameWatcher gw = AppHost.Games;
            string active = gw == null ? null : gw.Active;

            // what is on screen right now
            var banner = new Border();
            banner.CornerRadius = new CornerRadius(10);
            banner.Padding = new Thickness(14, 10, 14, 10);
            banner.Margin = new Thickness(0, 0, 0, 12);
            banner.SetResourceReference(Border.BackgroundProperty, "AccentSoftBrush");
            var bt = Label(active == null
                ? "Right now: your normal looks."
                : "Right now: the looks for " + active + ". Changes you make in the Clock / Timer / Countdown tabs are saved for that game.");
            banner.Child = bt;
            page.Children.Add(banner);

            AddCheck(page, S, "Switch looks automatically when one of these programs is running", "AutoSwitchGames", delegate { if (AppHost.Games != null) AppHost.Games.Check(); });

            var list = new AnimatedSection("Your games", true);
            page.Children.Add(list);
            List<string> games = GameWatcher.Games(S);
            if (games.Count == 0) list.Body.Children.Add(Hint("No games yet. Add one below."));

            foreach (string g in games)
            {
                string game = g;
                var row = new Grid();
                row.Margin = new Thickness(0, 5, 0, 5);
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var info = new StackPanel();
                info.Children.Add(Label(game));
                string state;
                if (active == game) state = "running now - using its own looks";
                else if (gw != null && gw.HasProfile(game)) state = "has its own saved looks";
                else state = "no saved looks yet (it starts with your normal looks)";
                var st = Hint(state);
                st.Margin = new Thickness(0);
                info.Children.Add(st);
                Grid.SetColumn(info, 0);
                row.Children.Add(info);

                var btns = new StackPanel { Orientation = Orientation.Horizontal };
                var copy = MakeButton("Use my current looks", delegate
                {
                    if (AppHost.Games != null) AppHost.Games.CopyCurrentLooksTo(game);
                    RefillGames();
                });
                copy.IsEnabled = active == null;
                btns.Children.Add(copy);
                btns.Children.Add(MakeButton("Remove", delegate
                {
                    if (AppHost.Games != null) AppHost.Games.Remove(game);
                    Changed();
                    RefillGames();
                }));
                Grid.SetColumn(btns, 1);
                row.Children.Add(btns);
                list.Body.Children.Add(row);
            }

            // ---- add a game ----
            var add = new AnimatedSection("Add a game", true);
            page.Children.Add(add);

            Grid nameRow = AddRow(add.Body, "Program name");
            var nameBox = new TextBox();
            nameBox.Width = 240;
            nameBox.HorizontalAlignment = HorizontalAlignment.Left;
            Put(nameRow, nameBox);

            Grid pickRow = AddRow(add.Body, "Or pick a running one");
            var pick = new ComboBox();
            pick.Width = 240;
            pick.HorizontalAlignment = HorizontalAlignment.Left;
            pick.MaxDropDownHeight = 280;
            foreach (string n in RunningPrograms()) pick.Items.Add(n);
            pick.SelectionChanged += delegate { if (pick.SelectedItem != null) nameBox.Text = (string)pick.SelectedItem; };
            Put(pickRow, pick);

            var addBtn = MakeButton("Add game", delegate
            {
                if (AppHost.Games != null && AppHost.Games.Add(nameBox.Text))
                {
                    Changed();
                    RefillGames();
                }
            });
            addBtn.Margin = new Thickness(0, 8, 0, 0);
            addBtn.HorizontalAlignment = HorizontalAlignment.Left;
            add.Body.Children.Add(addBtn);
            add.Body.Children.Add(Hint("Tip: start the game first, then pick it from the list. The program name is the .exe name you see in Task Manager."));
        }

        private static List<string> RunningPrograms()
        {
            var names = new List<string>();
            string me = Process.GetCurrentProcess().ProcessName;
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle.Length > 0 &&
                            !string.Equals(p.ProcessName, me, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase))
                        {
                            string n = p.ProcessName.ToLowerInvariant() + ".exe";
                            if (!names.Contains(n)) names.Add(n);
                        }
                    }
                    catch (Exception) { }
                    finally { p.Dispose(); }
                }
            }
            catch (Exception) { }
            names.Sort();
            return names;
        }

        // ======================================================================== Keys

        private void BuildKeysTab()
        {
            _hotkeyButtons.Clear();
            StackPanel page = NewTab("Keys", "Keys", "Global keys that work even while a game is in front. Click a key box, then press the new combination.");

            page.Children.Add(Heading("Main keys", 15));
            foreach (HotkeyDef d in AppHost.HotkeyDefs)
            {
                if (!d.AllowEmpty || d.Field == "KeyHideAll" || d.Field == "KeyMoveMode") AddKeyRow(page, d);
            }

            var optional = new AnimatedSection("Optional keys (only the Timer, only the Countdown)", false);
            optional.Margin = new Thickness(0, 14, 0, 10);
            foreach (HotkeyDef d in AppHost.HotkeyDefs)
            {
                if (d.AllowEmpty && d.Field != "KeyHideAll" && d.Field != "KeyMoveMode") AddKeyRow(optional.Body, d);
            }
            page.Children.Add(optional);

            _hotkeyStatus = new TextBlock();
            _hotkeyStatus.TextWrapping = TextWrapping.Wrap;
            _hotkeyStatus.Margin = new Thickness(0, 8, 0, 0);
            _hotkeyStatus.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            page.Children.Add(_hotkeyStatus);
            ShowHotkeyProblems("");
        }

        private void AddKeyRow(StackPanel p, HotkeyDef d)
        {
            var row = new Grid();
            row.Margin = new Thickness(0, 5, 0, 5);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock l = Label(d.Label);
            Grid.SetColumn(l, 0);
            row.Children.Add(l);

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var keyButton = new Button();
            keyButton.MinWidth = 150;
            keyButton.Margin = new Thickness(0, 0, 6, 0);
            keyButton.Content = KeyText(d.Field);
            keyButton.Click += delegate { BeginCapture(d, keyButton); };
            _hotkeyButtons[d.Field] = keyButton;
            sp.Children.Add(keyButton);

            if (d.AllowEmpty)
            {
                sp.Children.Add(MakeButton("Clear", delegate { SetKey(d.Field, ""); }));
            }
            if (!string.IsNullOrEmpty(d.DefaultKey))
            {
                sp.Children.Add(MakeButton("Default", delegate { SetKey(d.Field, d.DefaultKey); }));
            }
            Grid.SetColumn(sp, 1);
            row.Children.Add(sp);
            p.Children.Add(row);
        }

        private string KeyText(string field)
        {
            string v = (string)Reflect.Get(S, field);
            return string.IsNullOrEmpty(v) ? "Not set" : v;
        }

        private void SetKey(string field, string text)
        {
            if (text.Length > 0)
            {
                foreach (HotkeyDef other in AppHost.HotkeyDefs)
                {
                    if (other.Field == field) continue;
                    if (string.Equals((string)Reflect.Get(S, other.Field), text, StringComparison.OrdinalIgnoreCase))
                    {
                        SetStatus(text + " is already used for \"" + other.Label + "\".", true);
                        return;
                    }
                }
            }
            Reflect.Set(S, field, text);
            AppHost.ReloadHotkeys();
            RefreshHotkeyButtons();
            ShowHotkeyProblems(text.Length > 0 ? "Saved: " + text : "Key cleared.");
            Changed();
        }

        private void BeginCapture(HotkeyDef d, Button b)
        {
            if (_capturing != null) EndCapture();
            _capturing = d;
            AppHost.SuspendHotkeys();   // so the key you press reaches this window
            b.Content = "Press the keys...";
            b.Focus();
            ShowHotkeyProblems("Press the new combination, or Esc to cancel.");
        }

        private void OnCaptureKey(object sender, KeyEventArgs e)
        {
            if (_capturing == null) return;
            e.Handled = true;

            Key k = (e.Key == Key.System) ? e.SystemKey : e.Key;
            if (k == Key.LeftCtrl || k == Key.RightCtrl || k == Key.LeftAlt || k == Key.RightAlt ||
                k == Key.LeftShift || k == Key.RightShift || k == Key.LWin || k == Key.RWin) return;

            if (k == Key.Escape)
            {
                EndCapture();
                return;
            }

            uint mods = 0;
            ModifierKeys mk = Keyboard.Modifiers;
            if ((mk & ModifierKeys.Control) != 0) mods |= NativeMethods.MOD_CONTROL;
            if ((mk & ModifierKeys.Alt) != 0) mods |= NativeMethods.MOD_ALT;
            if ((mk & ModifierKeys.Shift) != 0) mods |= NativeMethods.MOD_SHIFT;
            if ((mk & ModifierKeys.Windows) != 0) mods |= NativeMethods.MOD_WIN;

            var bind = new HotkeyBinding();
            bind.Modifiers = mods;
            bind.Key = (uint)KeyInterop.VirtualKeyFromKey(k);
            string text = bind.ToString();

            if (HotkeyBinding.Parse(text) == null)
            {
                SetStatus("That key can't be used. Try a letter, a number or an F-key.", true);
                return;
            }
            bool isFKey = bind.Key >= 0x70 && bind.Key <= 0x87;
            if (mods == 0 && !isFKey)
            {
                SetStatus("Add Ctrl, Alt or Shift (only F-keys can be used on their own).", true);
                return;
            }
            foreach (HotkeyDef other in AppHost.HotkeyDefs)
            {
                if (other.Field == _capturing.Field) continue;
                HotkeyBinding ob = HotkeyBinding.Parse((string)Reflect.Get(S, other.Field));
                if (ob != null && ob.ToString() == text)
                {
                    SetStatus(text + " is already used for \"" + other.Label + "\".", true);
                    return;
                }
            }

            Reflect.Set(S, _capturing.Field, text);
            EndCapture();
            ShowHotkeyProblems("Saved: " + text);
            Changed();
        }

        private void EndCapture()
        {
            _capturing = null;
            AppHost.ReloadHotkeys();
            RefreshHotkeyButtons();
            ShowHotkeyProblems("");
        }

        private void RefreshHotkeyButtons()
        {
            foreach (KeyValuePair<string, Button> kv in _hotkeyButtons)
            {
                kv.Value.Content = KeyText(kv.Key);
            }
        }

        private void ShowHotkeyProblems(string message)
        {
            if (_hotkeyStatus == null) return;
            string text = message;
            if (AppHost.HotkeyProblems.Count > 0)
            {
                text = (text.Length > 0 ? text + "\n" : "") + string.Join("\n", AppHost.HotkeyProblems.ToArray());
                _hotkeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0x8A, 0x1E));
            }
            else
            {
                _hotkeyStatus.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            }
            _hotkeyStatus.Text = text;
        }

        private void SetStatus(string text, bool warning)
        {
            if (_hotkeyStatus == null) return;
            _hotkeyStatus.Text = text;
            if (warning) _hotkeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0x8A, 0x1E));
            else _hotkeyStatus.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        }

        // ======================================================================== Settings

        private void BuildSettingsTab()
        {
            StackPanel page = NewTab("Settings", "Settings", "How Better Clock itself looks and behaves.");
            AddSearchBox(page);

            // ---- look of the app ----
            var look = new AnimatedSection("Theme and colours", true);
            AddEnumCombo(look.Body, S, "Theme", "ColorTheme", typeof(ColorThemeMode), delegate { ReapplyTheme(); });
            look.Body.Children.Add(Hint("Dark, Light, or System (follows your Windows app mode)."));
            AddColor(look.Body, S, "Accent colour", "AccentColor", delegate { ReapplyTheme(); });
            page.Children.Add(look);

            // ---- animation ----
            var anim = new AnimatedSection("Animations and size", true);
            AddCheck(anim.Body, S, "Animations", "AnimationsEnabled", delegate { ReapplyTheme(); });
            AddSlider(anim.Body, S, "Animation speed", "AnimationSpeed", 50, 250, 10, " %", delegate { ReapplyTheme(); });
            AddSlider(anim.Body, S, "Window size (UI scale)", "UiScale", 80, 150, 5, " %", delegate { ReapplyTheme(); });
            page.Children.Add(anim);

            // ---- startup ----
            var start = new AnimatedSection("Startup", true);
            AddCheck(start.Body, S, "Start with Windows", "StartWithWindows", delegate(bool on)
            {
                if (!AutoStart.Set(on))
                {
                    S.StartWithWindows = AutoStart.IsEnabled();
                    SetStatus("Could not change the Windows startup entry.", true);
                }
            });
            AddCheck(start.Body, S, "Show the loading screen", "ShowSplash", null);
            AddCheck(start.Body, S, "Show the \"Better Clock is running\" message", "ShowStartupMessage", null);
            AddCheck(start.Body, S, "Open this window when Better Clock starts", "OpenSettingsOnLaunch", null);
            page.Children.Add(start);

            // ---- updates ----
            var upd = new AnimatedSection("Updates", true);
            AddCheck(upd.Body, S, "Check GitHub for a new version when Better Clock starts", "CheckForUpdates", null);
            AddCheck(upd.Body, S, "Install new versions automatically (Better Clock restarts itself)", "AutoInstallUpdates", null);
            var updResult = Hint(UpdateChecker.Configured
                ? "Updates come from " + AppInfo.Repo + " on GitHub. Your settings are kept."
                : "No GitHub repository is set yet. Put yourname/BetterClock into repo.txt and build again (a GitHub Actions build does this for you).");
            var updButtons = new WrapPanel();
            var installBtn = MakeButton("Update now", delegate { });
            installBtn.Visibility = Visibility.Collapsed;
            UpdateResult found = null;
            installBtn.Click += delegate
            {
                if (found == null) return;
                installBtn.IsEnabled = false;
                Updater.Install(found,
                    delegate(string s) { updResult.Text = s; },
                    delegate(string err) { updResult.Text = err; installBtn.IsEnabled = true; });
            };
            updButtons.Children.Add(MakeButton("Check now", delegate
            {
                if (!UpdateChecker.Configured) return;
                updResult.Text = "Checking...";
                installBtn.Visibility = Visibility.Collapsed;
                UpdateChecker.CheckAsync(delegate(UpdateResult r)
                {
                    if (!r.Ok) { updResult.Text = r.Error; return; }
                    if (r.UpdateAvailable)
                    {
                        found = r;
                        updResult.Text = "Version " + r.Latest + " is available (you have " + AppInfo.Version + ").";
                        installBtn.Visibility = string.IsNullOrEmpty(r.DownloadUrl) ? Visibility.Collapsed : Visibility.Visible;
                    }
                    else updResult.Text = "You are up to date (v" + AppInfo.Version + ").";
                });
            }));
            updButtons.Children.Add(installBtn);
            updButtons.Children.Add(MakeButton("Open releases page", delegate
            {
                if (!UpdateChecker.Configured) return;
                try { Process.Start(UpdateChecker.ReleasesUrl); } catch (Exception) { }
            }));
            upd.Body.Children.Add(updButtons);
            upd.Body.Children.Add(updResult);
            page.Children.Add(upd);

            // ---- data ----
            var data = new AnimatedSection("Your settings file", false);
            var buttons = new WrapPanel();
            buttons.Children.Add(MakeButton("Export...", ExportSettings));
            buttons.Children.Add(MakeButton("Import...", ImportSettings));
            buttons.Children.Add(MakeButton("Open settings folder", delegate
            {
                try { Process.Start("explorer.exe", AppSettings.FolderPath); } catch (Exception) { }
            }));
            buttons.Children.Add(MakeButton("Reset everything", ResetEverything));
            data.Body.Children.Add(buttons);
            data.Body.Children.Add(Hint("Settings are saved automatically in " + AppSettings.FilePath));
            page.Children.Add(data);
        }

        private void ExportSettings()
        {
            var dlg = new SaveFileDialog();
            dlg.Title = "Export Better Clock settings";
            dlg.Filter = "Settings file|*.ini";
            dlg.FileName = "BetterClock-settings.ini";
            if (dlg.ShowDialog(this) == true)
            {
                try { S.SaveTo(dlg.FileName); }
                catch (Exception ex) { MessageBox.Show(this, "Could not export:\n" + ex.Message, Program.AppName); }
            }
        }

        private void ImportSettings()
        {
            var dlg = new OpenFileDialog();
            dlg.Title = "Import Better Clock settings";
            dlg.Filter = "Settings file|*.ini|All files|*.*";
            if (dlg.ShowDialog(this) != true) return;
            try
            {
                AppSettings loaded = AppSettings.LoadFrom(dlg.FileName);
                S.CopyFrom(loaded);
                AfterBulkChange();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not import:\n" + ex.Message, Program.AppName);
            }
        }

        private void ResetEverything()
        {
            MessageBoxResult r = MessageBox.Show(this, "Put every setting back to its default?", Program.AppName,
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;
            S.CopyFrom(new AppSettings());
            AfterBulkChange();
        }

        private void AfterBulkChange()
        {
            if (AppHost.Games != null) AppHost.Games.Forget();
            S.LastSeenVersion = AppInfo.Version;
            S.StartWithWindows = AutoStart.IsEnabled();
            AppHost.ReloadHotkeys();
            AppHost.ApplySettings();
            RebuildAll();
            AppHost.SaveSettings();
        }

        // ======================================================================== Changelog

        private void BuildChangelogTab()
        {
            StackPanel page = NewTab("Changelog", "What's new", "You are running version " + AppInfo.Version + ".");
            foreach (ChangelogEntry entry in Changelog.Entries())
            {
                var card = new Border();
                card.SetResourceReference(Border.BackgroundProperty, "Panel2Brush");
                card.CornerRadius = new CornerRadius(12);
                card.Padding = new Thickness(18, 14, 18, 14);
                card.Margin = new Thickness(0, 0, 0, 12);

                var sp = new StackPanel();
                card.Child = sp;

                var head = new StackPanel { Orientation = Orientation.Horizontal };
                var badge = new Border();
                badge.CornerRadius = new CornerRadius(8);
                badge.Padding = new Thickness(9, 2, 9, 3);
                badge.SetResourceReference(Border.BackgroundProperty, "AccentBrush");
                var badgeText = new TextBlock();
                badgeText.Text = "v" + entry.Version;
                badgeText.Foreground = Brushes.White;
                badgeText.FontWeight = FontWeights.SemiBold;
                badge.Child = badgeText;
                head.Children.Add(badge);

                var date = Label("   " + entry.Date + (entry.Version == AppInfo.Version ? "   (installed)" : ""));
                date.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
                head.Children.Add(date);
                sp.Children.Add(head);

                var title = Heading(entry.Title, 16);
                title.Margin = new Thickness(0, 8, 0, 6);
                sp.Children.Add(title);

                foreach (string item in entry.Items)
                {
                    var line = Label("\u2022  " + item);
                    line.Margin = new Thickness(0, 2, 0, 2);
                    sp.Children.Add(line);
                }
                page.Children.Add(card);
            }
        }

        // ======================================================================== About

        private void BuildAboutTab()
        {
            StackPanel page = NewTab("About", "About", "Better Clock - a tiny always-on-top clock, timer and countdown for gamers.");

            var logo = new Image();
            logo.Source = IconFactory.GetImageSource();
            logo.Width = 96;
            logo.Height = 96;
            logo.HorizontalAlignment = HorizontalAlignment.Left;
            logo.Margin = new Thickness(0, 6, 0, 12);
            logo.RenderTransformOrigin = new Point(0.5, 0.5);
            var pulse = new ScaleTransform(1, 1);
            logo.RenderTransform = pulse;
            if (UiAnim.Enabled)
            {
                var a = new DoubleAnimation(1.0, 1.08, TimeSpan.FromMilliseconds(1200));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                a.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
                pulse.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                pulse.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            page.Children.Add(logo);

            page.Children.Add(Heading(Program.AppName + "  v" + AppInfo.Version, 18));
            var t = Hint("Shows the time over your game without alt-tabbing. Click-through, never steals focus, " +
                "works over borderless and windowed fullscreen games.");
            page.Children.Add(t);

            var note = Hint("Note: a game in true exclusive fullscreen can hide any overlay - switch it to borderless/windowed. " +
                "Online games with anti-cheat can dislike overlays; use at your own risk.");
            note.Margin = new Thickness(0, 10, 0, 0);
            page.Children.Add(note);

            var mit = Hint("Free and open source (MIT licence).");
            mit.Margin = new Thickness(0, 10, 0, 0);
            page.Children.Add(mit);
        }
    }
}
