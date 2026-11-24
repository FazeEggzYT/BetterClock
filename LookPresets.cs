// Ready-made looks (the theme gallery) and your own saved looks (presets).
// A preset holds the look of all three overlays. Positions and label texts are not changed when you apply one.
using System;
using System.Collections.Generic;
using System.IO;

namespace BetterClock
{
    public static class LookPresets
    {
        private static AppSettings S { get { return AppHost.Settings; } }

        // ------------------------------------------------------------ your own presets

        public static string Folder
        {
            get { return Path.Combine(AppSettings.FolderPath, "presets"); }
        }

        private static string PathFor(string name)
        {
            return Path.Combine(Folder, name + ".ini");
        }

        // Removes characters a file name can't have.
        public static string Clean(string name)
        {
            if (name == null) return "";
            var sb = new System.Text.StringBuilder();
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in name.Trim())
            {
                if (Array.IndexOf(bad, c) >= 0 || c == '.') continue;
                sb.Append(c);
            }
            string s = sb.ToString().Trim();
            if (s.Length > 40) s = s.Substring(0, 40).Trim();
            return s;
        }

        public static List<string> Names()
        {
            var list = new List<string>();
            try
            {
                if (Directory.Exists(Folder))
                {
                    foreach (string f in Directory.GetFiles(Folder, "*.ini"))
                        list.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch (Exception)
            {
            }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        // Returns the saved name, or null if it could not be saved.
        public static string Save(string name)
        {
            string clean = Clean(name);
            if (clean.Length == 0) return null;
            try
            {
                Directory.CreateDirectory(Folder);
                var holder = new AppSettings();
                holder.Clock.CopyFrom(S.Clock, false);
                holder.Timer.CopyFrom(S.Timer, false);
                holder.Countdown.CopyFrom(S.Countdown, false);
                holder.SaveTo(PathFor(clean));
                return clean;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool ApplySaved(string name)
        {
            try
            {
                if (!File.Exists(PathFor(name))) return false;
                AppSettings loaded = AppSettings.LoadFrom(PathFor(name));
                S.Clock.CopyFrom(loaded.Clock, true);
                S.Timer.CopyFrom(loaded.Timer, true);
                S.Countdown.CopyFrom(loaded.Countdown, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Delete(string name)
        {
            try { File.Delete(PathFor(name)); } catch (Exception) { }
        }

        // Applies the change on screen, saves it and refreshes the settings window if it is open.
        public static void Commit()
        {
            AppHost.ApplySettings();
            AppHost.SaveSettings();
            SettingsWindow.RefreshIfOpen();
        }

        // ------------------------------------------------------------ the theme gallery

        public static readonly string[] BuiltInNames =
        {
            "Default", "Neon", "Minimal", "Glass", "Retro green", "Scoreboard", "Gold", "Ice", "Sunset", "High contrast",
            "Cyberpunk", "Forest", "Ocean", "Lava", "Candy", "Midnight", "Mono", "Amber terminal",
            "Pastel", "Royal", "Steel", "Matrix", "Arcade", "Paper", "Aurora"
        };

        // Saves the look of all three overlays to a file anywhere (to send to a friend).
        public static bool ExportToFile(string path)
        {
            try
            {
                var holder = new AppSettings();
                holder.Clock.CopyFrom(S.Clock, false);
                holder.Timer.CopyFrom(S.Timer, false);
                holder.Countdown.CopyFrom(S.Countdown, false);
                holder.SaveTo(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Loads a look file from anywhere onto the three overlays.
        public static bool ImportFromFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                AppSettings loaded = AppSettings.LoadFrom(path);
                S.Clock.CopyFrom(loaded.Clock, true);
                S.Timer.CopyFrom(loaded.Timer, true);
                S.Countdown.CopyFrom(loaded.Countdown, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // A random look (colours, shape, font style, border) on all three overlays.
        public static void Randomise()
        {
            var r = new Random();
            var s = new ModeStyle();
            float hue = (float)r.NextDouble();
            s.BackgroundStyle = r.Next(3) == 0 ? BackgroundStyle.Solid : (r.Next(2) == 0 ? BackgroundStyle.Gradient : BackgroundStyle.Radial);
            s.BackgroundColor = Hsv(hue, 0.7, 0.18);
            s.BackgroundColor2 = Hsv((hue + 0.12f) % 1f, 0.7, 0.45);
            s.GradientAngle = r.Next(0, 360);
            s.BackgroundOpacity = 70 + r.Next(30);
            s.TextColor = Hsv((hue + 0.5f) % 1f, 0.15 + r.NextDouble() * 0.4, 1.0);
            s.RunningColor = Hsv((hue + 0.33f) % 1f, 0.6, 1.0);
            s.PausedColor = Hsv((hue + 0.15f) % 1f, 0.7, 1.0);
            s.FinishedColor = Hsv(0.0f, 0.8, 1.0);
            s.BorderColor = Hsv((hue + 0.5f) % 1f, 0.8, 1.0);
            s.BorderThickness = r.Next(0, 5);
            s.CornerRadius = r.Next(0, 40);
            PillShape[] shapes = { PillShape.Rounded, PillShape.Rounded, PillShape.Rounded, PillShape.Slanted, PillShape.Hexagon };
            s.Shape = shapes[r.Next(shapes.Length)];
            DigitStyle[] digits = { DigitStyle.Normal, DigitStyle.Normal, DigitStyle.Led, DigitStyle.Terminal };
            s.DigitStyle = digits[r.Next(digits.Length)];
            s.ShadowEnabled = r.Next(2) == 0;
            s.ShadowColor = s.BorderColor;
            s.ShadowBlur = 10 + r.Next(25);
            S.Clock.CopyFrom(s, true);
            S.Timer.CopyFrom(s, true);
            S.Countdown.CopyFrom(s, true);
        }

        private static string Hsv(float h, double sat, double val)
        {
            double hh = h * 6.0;
            int i = (int)Math.Floor(hh) % 6;
            double f = hh - Math.Floor(hh);
            double p = val * (1 - sat), q = val * (1 - f * sat), t = val * (1 - (1 - f) * sat);
            double rr, gg, bb;
            switch (i)
            {
                case 0: rr = val; gg = t; bb = p; break;
                case 1: rr = q; gg = val; bb = p; break;
                case 2: rr = p; gg = val; bb = t; break;
                case 3: rr = p; gg = q; bb = val; break;
                case 4: rr = t; gg = p; bb = val; break;
                default: rr = val; gg = p; bb = q; break;
            }
            return string.Format("#{0:X2}{1:X2}{2:X2}", (int)(rr * 255), (int)(gg * 255), (int)(bb * 255));
        }

        public static void ApplyBuiltIn(string name)
        {
            S.Clock.CopyFrom(BuiltIn(name), true);
            S.Timer.CopyFrom(BuiltIn(name), true);
            S.Countdown.CopyFrom(BuiltIn(name), true);
        }

        public static ModeStyle BuiltIn(string name)
        {
            ModeStyle more = MoreThemes.Build(name);
            if (more != null) return more;
            var s = new ModeStyle();   // starts from the default look
            switch (name)
            {
                case "Neon":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#0A0A14";
                    s.BackgroundOpacity = 85;
                    s.TextColor = "#00F0FF";
                    s.RunningColor = "#39FF14";
                    s.PausedColor = "#FFE600";
                    s.FinishedColor = "#FF2E63";
                    s.BorderColor = "#FF00E5";
                    s.BorderThickness = 2;
                    s.CornerRadius = 10;
                    s.DigitStyle = DigitStyle.Led;
                    s.ShadowEnabled = true;
                    s.ShadowBlur = 30;
                    break;

                case "Minimal":
                    s.BackgroundStyle = BackgroundStyle.None;
                    s.FontFamily = "Segoe UI Light";
                    s.FontSize = 48;
                    s.TextColor = "#FFFFFF";
                    s.BorderThickness = 0;
                    s.ShadowEnabled = false;
                    s.TextShadow = true;
                    break;

                case "Glass":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#FFFFFF";
                    s.BackgroundOpacity = 16;
                    s.TextColor = "#FFFFFF";
                    s.BorderColor = "#FFFFFF";
                    s.BorderThickness = 1;
                    s.CornerRadius = 22;
                    s.ShadowEnabled = true;
                    s.ShadowBlur = 20;
                    break;

                case "Retro green":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#000000";
                    s.BackgroundOpacity = 90;
                    s.DigitStyle = DigitStyle.Terminal;
                    s.TextColor = "#33FF33";
                    s.RunningColor = "#7CFF7C";
                    s.PausedColor = "#FFD633";
                    s.FinishedColor = "#FF4444";
                    s.BorderColor = "#33FF33";
                    s.BorderThickness = 2;
                    s.CornerRadius = 4;
                    s.ShadowEnabled = false;
                    break;

                case "Scoreboard":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#111111";
                    s.BackgroundOpacity = 95;
                    s.DigitStyle = DigitStyle.Led;
                    s.TextColor = "#FFB000";
                    s.RunningColor = "#FFB000";
                    s.PausedColor = "#FF7A00";
                    s.FinishedColor = "#FF2A2A";
                    s.BorderColor = "#444444";
                    s.BorderThickness = 3;
                    s.CornerRadius = 6;
                    break;

                case "Gold":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#1A1205";
                    s.BackgroundColor2 = "#3A2A08";
                    s.GradientAngle = 90;
                    s.BackgroundOpacity = 90;
                    s.TextColor = "#FFD84D";
                    s.RunningColor = "#FFF0A0";
                    s.BorderColor = "#FFD84D";
                    s.BorderThickness = 2;
                    s.CornerRadius = 16;
                    break;

                case "Ice":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#0B1B2E";
                    s.BackgroundColor2 = "#14456B";
                    s.GradientAngle = 60;
                    s.BackgroundOpacity = 88;
                    s.TextColor = "#D8F3FF";
                    s.RunningColor = "#7DF9FF";
                    s.BorderColor = "#7DF9FF";
                    s.BorderThickness = 1;
                    s.CornerRadius = 18;
                    break;

                case "Sunset":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#3A0F2E";
                    s.BackgroundColor2 = "#FF5E3A";
                    s.GradientAngle = 20;
                    s.BackgroundOpacity = 85;
                    s.TextColor = "#FFFFFF";
                    s.RunningColor = "#FFF3B0";
                    s.BorderThickness = 0;
                    s.CornerRadius = 24;
                    break;

                case "High contrast":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#000000";
                    s.BackgroundOpacity = 100;
                    s.TextColor = "#FFFFFF";
                    s.RunningColor = "#00FF00";
                    s.PausedColor = "#FFFF00";
                    s.FinishedColor = "#FF0000";
                    s.BorderColor = "#FFFFFF";
                    s.BorderThickness = 3;
                    s.CornerRadius = 0;
                    s.FontBold = true;
                    s.ShadowEnabled = false;
                    s.TextShadow = false;
                    break;

                default:
                    break;   // "Default" = the normal look
            }
            return s;
        }
    }
}
