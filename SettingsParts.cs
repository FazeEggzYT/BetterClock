// Settings sections for "each part on its own" (v1.4.4).
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BetterClock
{
    public partial class SettingsWindow
    {
        private static readonly string[] AllParts = { "label", "above", "main", "bar", "date", "info", "below" };

        private static string PartTitle(string key)
        {
            switch (key)
            {
                case "label": return "Label";
                case "above": return "Text above";
                case "main": return "Numbers and icon";
                case "bar": return "Progress bar";
                case "date": return "Date line";
                case "info": return "Extra lines";
                default: return "Text below";
            }
        }

        // Copies every setting of one section to the other two overlays.
        private void AddCopySection(AnimatedSection sec, OverlayMode mode)
        {
            StackPanel body = sec.Body;
            sec.Body.Children.Add(MakeButton("Copy this section to the other overlays", delegate
            {
                List<KeyValuePair<object, string>> list;
                if (!_secFields.TryGetValue(body, out list)) return;
                foreach (OverlayMode other in new OverlayMode[] { OverlayMode.Clock, OverlayMode.Timer, OverlayMode.Countdown })
                {
                    if (other == mode) continue;
                    ModeStyle target = S.StyleFor(other);
                    foreach (KeyValuePair<object, string> kv in list)
                        Reflect.Set(target, kv.Value, Reflect.Get(kv.Key, kv.Value));
                }
                AppHost.ApplySettings();
                AppHost.SaveSettings();
                RefillAllModes();
            }));
        }

        private void AddPartText(AnimatedSection sec, ModeStyle st, string prefix, bool withDefaultsNote)
        {
            AddFontCombo(sec.Body, st, "Font", prefix + "Font");
            AddSlider(sec.Body, st, "Size", prefix + "Size", 0, 120, 1, " px", null);
            if (withDefaultsNote) sec.Body.Children.Add(Hint("Size 0 and an empty colour use the Extra text size and colour from the section above."));
            AddColor(sec.Body, st, "Colour", prefix + "Color", null);
            AddCheck(sec.Body, st, "Bold", prefix + "Bold", null);
            AddSlider(sec.Body, st, "Opacity", prefix + "Opacity", 0, 100, 5, " %", null);
            AddSlider(sec.Body, st, "Move left / right", prefix + "OffsetX", -200, 200, 1, " px", null);
            AddSlider(sec.Body, st, "Move up / down", prefix + "OffsetY", -200, 200, 1, " px", null);
        }

        private void BuildPartSections(StackPanel page, ModeStyle st, OverlayMode mode)
        {
            // ---- numbers ----
            var nums = new AnimatedSection("Numbers (own settings)", false);
            AddCheck(nums.Body, st, "Show the numbers", "ShowNumbers", null);
            AddSlider(nums.Body, st, "Opacity", "NumbersOpacity", 0, 100, 5, " %", null);
            AddSlider(nums.Body, st, "Move left / right", "NumbersOffsetX", -200, 200, 1, " px", null);
            AddSlider(nums.Body, st, "Move up / down", "NumbersOffsetY", -200, 200, 1, " px", null);
            AddCopySection(nums, mode);
            page.Children.Add(nums);

            // ---- label (extra settings) ----
            var lab = new AnimatedSection("Label (own look)", false);
            AddFontCombo(lab.Body, st, "Font", "LabelFont");
            AddCheck(lab.Body, st, "Bold", "LabelBold", null);
            AddSlider(lab.Body, st, "Opacity", "LabelOpacity", 0, 100, 5, " %", null);
            AddSlider(lab.Body, st, "Move left / right", "LabelOffsetX", -200, 200, 1, " px", null);
            AddSlider(lab.Body, st, "Move up / down", "LabelOffsetY", -200, 200, 1, " px", null);
            lab.Body.Children.Add(Hint("Text, size and colour of the label are in the section above."));
            AddCopySection(lab, mode);
            page.Children.Add(lab);

            // ---- above ----
            var above = new AnimatedSection("Text above (own look)", false);
            AddCheck(above.Body, st, "Show it", "ShowAbove", null);
            AddPartText(above, st, "Above", true);
            AddCopySection(above, mode);
            page.Children.Add(above);

            // ---- below ----
            var below = new AnimatedSection("Text below (own look)", false);
            AddCheck(below.Body, st, "Show it", "ShowBelow", null);
            AddPartText(below, st, "Below", true);
            AddCopySection(below, mode);
            page.Children.Add(below);

            // ---- date ----
            if (mode == OverlayMode.Clock)
            {
                var date = new AnimatedSection("Date line (own look)", false);
                AddPartText(date, st, "Date", true);
                AddCopySection(date, mode);
                page.Children.Add(date);
            }

            // ---- extra lines ----
            var info = new AnimatedSection("Extra lines (own look)", false);
            AddCheck(info.Body, st, "Show them", "ShowInfo", null);
            AddPartText(info, st, "Info", true);
            info.Body.Children.Add(Hint("Extra lines are the world clocks, date countdown, days since and session time."));
            AddCopySection(info, mode);
            page.Children.Add(info);

            // ---- icon ----
            var icon = new AnimatedSection("Icon (own look)", false);
            AddCheck(icon.Body, st, "Show the icon", "ShowIcon", null);
            AddColor(icon.Body, st, "Icon colour", "IconColor", null);
            icon.Body.Children.Add(Hint("An empty colour uses the number colour."));
            AddSlider(icon.Body, st, "Opacity", "IconOpacity", 0, 100, 5, " %", null);
            AddSlider(icon.Body, st, "Move left / right", "IconOffsetX", -200, 200, 1, " px", null);
            AddSlider(icon.Body, st, "Move up / down", "IconOffsetY", -200, 200, 1, " px", null);
            AddSlider(icon.Body, st, "Turn", "IconRotation", -180, 180, 5, " deg", null);
            AddCopySection(icon, mode);
            page.Children.Add(icon);

            // ---- order and spacing ----
            var order = new AnimatedSection("Order and spacing of the parts", false);
            AddSlider(order.Body, st, "Space between parts", "PartSpacing", 0, 40, 1, " px", null);
            var list = new StackPanel();
            order.Body.Children.Add(list);
            FillOrderList(list, st, mode);
            order.Body.Children.Add(MakeButton("Put the order back to normal", delegate
            {
                st.PartOrder = "label,above,main,bar,date,info,below";
                FillOrderList(list, st, mode);
                Changed();
            }));
            order.Body.Children.Add(Hint("Use the arrows to move a part up or down. Parts that are switched off simply stay hidden."));
            Track(order.Body, st, "PartOrder");
            page.Children.Add(order);
        }

        private static List<string> ReadOrder(string text)
        {
            var order = new List<string>();
            foreach (string raw in (text ?? "").Split(','))
            {
                string key = raw.Trim().ToLowerInvariant();
                if (Array.IndexOf(AllParts, key) >= 0 && !order.Contains(key)) order.Add(key);
            }
            foreach (string key in AllParts) if (!order.Contains(key)) order.Add(key);
            return order;
        }

        private void FillOrderList(StackPanel list, ModeStyle st, OverlayMode mode)
        {
            list.Children.Clear();
            List<string> order = ReadOrder(st.PartOrder);
            for (int i = 0; i < order.Count; i++)
            {
                int index = i;
                var row = new StackPanel();
                row.Orientation = Orientation.Horizontal;
                row.Margin = new Thickness(0, 2, 0, 2);
                var name = Label((index + 1) + ".  " + PartTitle(order[index]));
                name.Width = 190;
                row.Children.Add(name);
                Button up = MakeButton("\u25B2", delegate { MovePart(list, st, mode, index, -1); });
                Button down = MakeButton("\u25BC", delegate { MovePart(list, st, mode, index, 1); });
                up.IsEnabled = index > 0;
                down.IsEnabled = index < order.Count - 1;
                row.Children.Add(up);
                row.Children.Add(down);
                list.Children.Add(row);
            }
        }

        private void MovePart(StackPanel list, ModeStyle st, OverlayMode mode, int index, int delta)
        {
            List<string> order = ReadOrder(st.PartOrder);
            int to = index + delta;
            if (to < 0 || to >= order.Count) return;
            string tmp = order[index];
            order[index] = order[to];
            order[to] = tmp;
            st.PartOrder = string.Join(",", order.ToArray());
            FillOrderList(list, st, mode);
            Changed();
        }
    }
}
