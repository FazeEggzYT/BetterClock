// Layout options for the parts of the overlay (v1.4.8).
namespace BetterClock
{
    public enum LayoutDirection { Vertical, Horizontal }

    public enum PartAlignment { Center, Left, Right }

    public partial class ModeStyle
    {
        public LayoutDirection Layout = LayoutDirection.Vertical;   // parts stacked top to bottom, or side by side
        public PartAlignment PartAlign = PartAlignment.Center;      // left / centre / right (top / centre / bottom when side by side)
        public int FixedWidth = 0;                                  // 0 = automatic
        public int FixedHeight = 0;                                 // 0 = automatic
        public int MinWidthPx = 0;                                  // the overlay never gets narrower than this
    }
}
