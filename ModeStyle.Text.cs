// Text effects for the numbers and the small texts (v1.4.5).
namespace BetterClock
{
    public enum LetterCase { Normal, UPPERCASE, lowercase }

    public enum DigitSeparator { Colon, Dot, Dash, Space, None }

    public partial class ModeStyle
    {
        // ---- outline around the numbers ----
        public int OutlineSize = 0;                 // 0 = off
        public string OutlineColor = "#000000";

        // ---- gradient colour on the numbers ----
        public bool NumberGradient = false;
        public string NumberColor2 = "#FF5AA5";     // the end colour; the start is the normal number colour
        public int NumberGradientAngle = 90;        // 0 = left to right, 90 = top to bottom

        // ---- glow on the numbers ----
        public int NumberGlowSize = 0;              // 0 = off
        public string NumberGlowColor = "";         // empty = same as the numbers
        public int NumberGlowStrength = 1;          // 1 to 3

        // ---- shadow behind the numbers (switched on with TextShadow) ----
        public string TextShadowColor = "#000000";
        public int TextShadowBlur = 6;
        public int TextShadowDistance = 2;
        public int TextShadowAngle = 315;           // 0 = right, 90 = up, 180 = left, 270 = down
        public int TextShadowOpacity = 80;

        // ---- spacing ----
        public int LetterSpacing = 0;               // extra pixels between the characters
        public int LineHeight = 100;                // percent

        // ---- capital letters for the small texts ----
        public LetterCase TextCase = LetterCase.Normal;

        // ---- your own font file ----
        public string FontFile = "";
        public bool FontFileForAll = false;

        // ---- separator and leading zero ----
        public DigitSeparator Separator = DigitSeparator.Colon;
        public bool LeadingZero = true;
    }
}
