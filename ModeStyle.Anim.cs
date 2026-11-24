// Animation settings and a separate look for each state (v1.4.7).
using System.Reflection;
using System.Text;

namespace BetterClock
{
    public enum DigitAnim { None, Fade, Slide, Flip, Pop }

    public enum HideAnim { Auto, Fade, Slide, Shrink, None }

    public enum EaseKind { Smooth, Linear, Snappy, Bouncy }

    public enum BreatheKind { None, Glow, Size }

    public enum WarnAnim { None, Shake, Flash }

    public partial class ModeStyle
    {
        // ---- animation ----
        public bool AnimationsOn = true;            // off = this overlay never animates
        public int AnimSpeed = 100;                 // percent: 200 = twice as fast
        public EaseKind AnimEasing = EaseKind.Smooth;
        public HideAnim HideAnimation = HideAnim.Auto;
        public DigitAnim DigitAnimation = DigitAnim.None;
        public BreatheKind Breathe = BreatheKind.None;
        public string BreatheColor = "#4D7CFF";
        public int BreatheAmount = 20;              // glow size (px)
        public int BreatheSize = 4;                 // how much the numbers grow (percent)
        public int BreatheSeconds = 3;
        public WarnAnim WarnAnimation = WarnAnim.None;
        public bool ColorFade = false;
        public int ColorFadeMs = 400;
        public bool AnimatedGradient = false;
        public int GradientSeconds = 8;
        public int PulseSpeed = 450;                // countdown pulse: ms per beat
        public int PulseSize = 40;                  // countdown pulse: biggest glow (px)

        // ---- a separate look for the night (time of day) ----
        public bool NightOn = false;
        public int NightFrom = 22;                  // hour
        public int NightTo = 7;                     // hour

        // ---- Running ----
        public string RunningBackground = "";          // empty = unchanged
        public string RunningBorderColor = "";
        public int RunningBorderWidth = -1;            // -1 = unchanged
        public string RunningGlowColor = "";
        public int RunningGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int RunningScale = 0;                   // 0 = unchanged (percent)

        // ---- Paused ----
        public string PausedBackground = "";          // empty = unchanged
        public string PausedBorderColor = "";
        public int PausedBorderWidth = -1;            // -1 = unchanged
        public string PausedGlowColor = "";
        public int PausedGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int PausedScale = 0;                   // 0 = unchanged (percent)

        // ---- Low ----
        public string LowBackground = "";          // empty = unchanged
        public string LowBorderColor = "";
        public int LowBorderWidth = -1;            // -1 = unchanged
        public string LowGlowColor = "";
        public int LowGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int LowScale = 0;                   // 0 = unchanged (percent)

        // ---- Danger ----
        public string DangerBackground = "";          // empty = unchanged
        public string DangerBorderColor = "";
        public int DangerBorderWidth = -1;            // -1 = unchanged
        public string DangerGlowColor = "";
        public int DangerGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int DangerScale = 0;                   // 0 = unchanged (percent)

        // ---- Finished ----
        public string FinishedBackground = "";          // empty = unchanged
        public string FinishedBorderColor = "";
        public int FinishedBorderWidth = -1;            // -1 = unchanged
        public string FinishedGlowColor = "";
        public int FinishedGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int FinishedScale = 0;                   // 0 = unchanged (percent)

        // ---- Overtime ----
        public string OvertimeBackground = "";          // empty = unchanged
        public string OvertimeBorderColor = "";
        public int OvertimeBorderWidth = -1;            // -1 = unchanged
        public string OvertimeGlowColor = "";
        public int OvertimeGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int OvertimeScale = 0;                   // 0 = unchanged (percent)

        // ---- Target ----
        public string TargetBackground = "";          // empty = unchanged
        public string TargetBorderColor = "";
        public int TargetBorderWidth = -1;            // -1 = unchanged
        public string TargetGlowColor = "";
        public int TargetGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int TargetScale = 0;                   // 0 = unchanged (percent)

        // ---- Night ----
        public string NightBackground = "";          // empty = unchanged
        public string NightBorderColor = "";
        public int NightBorderWidth = -1;            // -1 = unchanged
        public string NightGlowColor = "";
        public int NightGlowSize = -1;               // -1 = unchanged, 0 = no glow
        public int NightScale = 0;                   // 0 = unchanged (percent)

        // A text that changes whenever any setting changes (used to skip needless redraws).
        private static FieldInfo[] _sigFields;

        public string Signature()
        {
            if (_sigFields == null) _sigFields = typeof(ModeStyle).GetFields(BindingFlags.Public | BindingFlags.Instance);
            var sb = new StringBuilder();
            foreach (FieldInfo f in _sigFields)
            {
                object v = f.GetValue(this);
                sb.Append(v == null ? "" : v.ToString());
                sb.Append('|');
            }
            return sb.ToString();
        }
    }
}
