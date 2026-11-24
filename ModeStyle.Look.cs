// Background, shape and border extras (v1.4.6).
namespace BetterClock
{
    public enum PillShape { Rounded, Circle, Hexagon, Slanted }

    public enum BorderLine { Solid, Dashed, Dotted, Double }

    public enum TextureStyle { None, Noise, Stripes, Dots, Grid }

    public partial class ModeStyle
    {
        // ---- background ----
        public string BackgroundColor3 = "";        // empty = a two-colour gradient

        // ---- shape and corners ----
        public PillShape Shape = PillShape.Rounded;
        public bool PerCorner = false;
        public int CornerTL = 16;
        public int CornerTR = 16;
        public int CornerBR = 16;
        public int CornerBL = 16;

        // ---- border ----
        public BorderLine BorderLineStyle = BorderLine.Solid;
        public int BorderOpacity = 100;
        public bool BorderGradient = false;
        public string BorderColor2 = "#A85CFF";
        public int BorderGradientAngle = 45;

        // ---- inner glow ----
        public int InnerGlowSize = 0;               // 0 = off
        public string InnerGlowColor = "#4D7CFF";
        public int InnerGlowOpacity = 60;

        // ---- glass look ----
        public bool Glass = false;
        public int GlassStrength = 50;

        // ---- texture ----
        public TextureStyle Texture = TextureStyle.None;
        public string TextureColor = "#FFFFFF";
        public int TextureOpacity = 15;
        public int TextureSize = 8;

        // ---- shadow of the whole pill (used when "Glow / shadow around it" is on) ----
        public string ShadowColor = "#000000";
        public int ShadowOffsetX = 0;
        public int ShadowOffsetY = 0;
        public int ShadowOpacity = 70;

        // ---- picture behind the numbers ----
        public int BackgroundImageZoom = 100;
        public int BackgroundImageX = 0;
        public int BackgroundImageY = 0;
        public string BackgroundImageTint = "";     // empty = no tint
        public int BackgroundImageTintAmount = 40;
    }
}
