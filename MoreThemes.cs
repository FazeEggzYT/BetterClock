// Fifteen more ready-made looks for the theme gallery (v1.4.8).
using System;

namespace BetterClock
{
    public static class MoreThemes
    {
        public static readonly string[] Names =
        {
            "Cyberpunk", "Forest", "Ocean", "Lava", "Candy", "Midnight", "Mono", "Amber terminal",
            "Pastel", "Royal", "Steel", "Matrix", "Arcade", "Paper", "Aurora"
        };

        // Returns the look, or null when the name is not one of these.
        public static ModeStyle Build(string name)
        {
            var s = new ModeStyle();
            switch (name)
            {
                case "Cyberpunk":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#12002B"; s.BackgroundColor2 = "#3A0A5E"; s.GradientAngle = 45;
                    s.BackgroundOpacity = 92;
                    s.TextColor = "#FFEE00"; s.RunningColor = "#00F5FF"; s.PausedColor = "#FF2BD6"; s.FinishedColor = "#FF2E63";
                    s.Shape = PillShape.Slanted;
                    s.BorderColor = "#00F5FF"; s.BorderThickness = 2;
                    s.OutlineSize = 1; s.OutlineColor = "#FF2BD6";
                    s.ShadowEnabled = true; s.ShadowBlur = 26; s.ShadowColor = "#FF2BD6";
                    break;
                case "Forest":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#0B2416"; s.BackgroundColor2 = "#1E4D2B"; s.GradientAngle = 90;
                    s.BackgroundOpacity = 90;
                    s.TextColor = "#E8F5D0"; s.RunningColor = "#9BE564"; s.PausedColor = "#F2C14E"; s.FinishedColor = "#E4572E";
                    s.BorderColor = "#6BBF59"; s.BorderThickness = 2; s.CornerRadius = 18;
                    s.Texture = TextureStyle.Noise; s.TextureOpacity = 12;
                    break;
                case "Ocean":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#02223A"; s.BackgroundColor2 = "#0A7FA8"; s.GradientAngle = 70;
                    s.BackgroundOpacity = 88;
                    s.TextColor = "#E6FBFF"; s.RunningColor = "#7DF9FF"; s.PausedColor = "#FFD166"; s.FinishedColor = "#FF6B6B";
                    s.BorderColor = "#7DF9FF"; s.BorderThickness = 1; s.CornerRadius = 26;
                    s.AnimatedGradient = true;
                    break;
                case "Lava":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#2A0500"; s.BackgroundColor2 = "#C93A00"; s.GradientAngle = 100;
                    s.BackgroundOpacity = 92;
                    s.TextColor = "#FFE2B0"; s.RunningColor = "#FFD166"; s.PausedColor = "#FFFFFF"; s.FinishedColor = "#FF1E1E";
                    s.BorderColor = "#FF8A00"; s.BorderThickness = 2; s.CornerRadius = 12;
                    s.ShadowEnabled = true; s.ShadowBlur = 28; s.ShadowColor = "#FF4500";
                    break;
                case "Candy":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#FF9ACB"; s.BackgroundColor2 = "#9A7BFF"; s.GradientAngle = 30;
                    s.BackgroundOpacity = 95;
                    s.TextColor = "#FFFFFF"; s.RunningColor = "#FFFDE0"; s.PausedColor = "#FFF176"; s.FinishedColor = "#D50057";
                    s.BorderColor = "#FFFFFF"; s.BorderThickness = 3; s.CornerRadius = 30;
                    s.TextShadow = true;
                    break;
                case "Midnight":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#05060F"; s.BackgroundOpacity = 90;
                    s.TextColor = "#B8C4FF"; s.RunningColor = "#8AA2FF"; s.PausedColor = "#FFD479"; s.FinishedColor = "#FF6B8B";
                    s.BorderColor = "#2A3270"; s.BorderThickness = 1; s.CornerRadius = 14;
                    s.InnerGlowSize = 10; s.InnerGlowColor = "#4D5DFF"; s.InnerGlowOpacity = 40;
                    break;
                case "Mono":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#FFFFFF"; s.BackgroundOpacity = 92;
                    s.TextColor = "#111111"; s.RunningColor = "#111111"; s.PausedColor = "#666666"; s.FinishedColor = "#000000";
                    s.BorderColor = "#111111"; s.BorderThickness = 2; s.CornerRadius = 0;
                    s.ShadowEnabled = false;
                    break;
                case "Amber terminal":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#140A00"; s.BackgroundOpacity = 92;
                    s.DigitStyle = DigitStyle.Terminal;
                    s.TextColor = "#FFB000"; s.RunningColor = "#FFC933"; s.PausedColor = "#FF8A00"; s.FinishedColor = "#FF3B1E";
                    s.BorderColor = "#FFB000"; s.BorderThickness = 2; s.CornerRadius = 4;
                    s.Texture = TextureStyle.Stripes; s.TextureOpacity = 8;
                    s.ShadowEnabled = false;
                    break;
                case "Pastel":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#FDF2F8"; s.BackgroundOpacity = 92;
                    s.TextColor = "#6B5B95"; s.RunningColor = "#4CAF93"; s.PausedColor = "#E6A23C"; s.FinishedColor = "#E06C75";
                    s.BorderColor = "#C9B6E4"; s.BorderThickness = 3; s.CornerRadius = 28;
                    s.ShadowEnabled = false;
                    break;
                case "Royal":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#1B0B3F"; s.BackgroundColor2 = "#3D1C8A"; s.GradientAngle = 90;
                    s.BackgroundOpacity = 94;
                    s.TextColor = "#FFD84D"; s.RunningColor = "#FFF0A0"; s.PausedColor = "#FFFFFF"; s.FinishedColor = "#FF5C7A";
                    s.BorderColor = "#FFD84D"; s.BorderLineStyle = BorderLine.Double; s.BorderThickness = 5; s.CornerRadius = 14;
                    break;
                case "Steel":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#2B2F36"; s.BackgroundColor2 = "#5A6270"; s.GradientAngle = 90;
                    s.BackgroundOpacity = 96;
                    s.TextColor = "#F2F5F8"; s.RunningColor = "#9FE870"; s.PausedColor = "#FFD166"; s.FinishedColor = "#FF5A5A";
                    s.BorderColor = "#9AA3B0"; s.BorderThickness = 3; s.CornerRadius = 8;
                    s.Texture = TextureStyle.Grid; s.TextureOpacity = 10;
                    break;
                case "Matrix":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#000A00"; s.BackgroundOpacity = 88;
                    s.DigitStyle = DigitStyle.Terminal;
                    s.TextColor = "#00FF41"; s.RunningColor = "#00FF41"; s.PausedColor = "#B6FF00"; s.FinishedColor = "#FF2E2E";
                    s.BorderColor = "#008F11"; s.BorderThickness = 1; s.CornerRadius = 2;
                    s.Texture = TextureStyle.Dots; s.TextureColor = "#00FF41"; s.TextureOpacity = 10;
                    s.ShadowEnabled = true; s.ShadowBlur = 24; s.ShadowColor = "#00FF41";
                    break;
                case "Arcade":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#0A0A2A"; s.BackgroundOpacity = 95;
                    s.DigitStyle = DigitStyle.Led;
                    s.TextColor = "#FF3EA5"; s.RunningColor = "#2EF2FF"; s.PausedColor = "#FFE600"; s.FinishedColor = "#FF3B30";
                    s.BorderColor = "#FFE600"; s.BorderLineStyle = BorderLine.Dashed; s.BorderThickness = 3; s.CornerRadius = 6;
                    break;
                case "Paper":
                    s.BackgroundStyle = BackgroundStyle.Solid;
                    s.BackgroundColor = "#F4ECD8"; s.BackgroundOpacity = 96;
                    s.FontFamily = "Georgia";
                    s.TextColor = "#3B2F1E"; s.RunningColor = "#2F6B3A"; s.PausedColor = "#A8641F"; s.FinishedColor = "#A8281F";
                    s.BorderColor = "#8C7A5B"; s.BorderThickness = 2; s.CornerRadius = 4;
                    s.Texture = TextureStyle.Noise; s.TextureColor = "#6B5A3A"; s.TextureOpacity = 14;
                    s.ShadowEnabled = false;
                    break;
                case "Aurora":
                    s.BackgroundStyle = BackgroundStyle.Gradient;
                    s.BackgroundColor = "#06232B"; s.BackgroundColor2 = "#2A0E5C"; s.BackgroundColor3 = "#0E7C66"; s.GradientAngle = 60;
                    s.BackgroundOpacity = 90;
                    s.TextColor = "#E9FFF7"; s.RunningColor = "#7CFFCB"; s.PausedColor = "#FFE29A"; s.FinishedColor = "#FF7AA8";
                    s.BorderColor = "#7CFFCB"; s.BorderGradient = true; s.BorderColor2 = "#B28CFF"; s.BorderThickness = 2; s.CornerRadius = 24;
                    s.AnimatedGradient = true; s.Glass = true; s.GlassStrength = 30;
                    break;
                default:
                    return null;
            }
            return s;
        }
    }
}
