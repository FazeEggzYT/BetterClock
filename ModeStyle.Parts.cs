// More settings for each part of the overlay (v1.4.4): every part has its own look.
namespace BetterClock
{
    public partial class ModeStyle
    {
        // ---- numbers ----
        public bool ShowNumbers = true;
        public int NumbersOpacity = 100;
        public int NumbersOffsetX = 0;
        public int NumbersOffsetY = 0;

        // ---- label (the small word above everything) ----
        public string LabelFont = "";            // empty = Segoe UI
        public bool LabelBold = true;
        public int LabelOpacity = 100;
        public int LabelOffsetX = 0;
        public int LabelOffsetY = 0;

        // ---- text above the numbers ----
        public bool ShowAbove = true;
        public string AboveFont = "";           // empty = Segoe UI
        public int AboveSize = 0;               // 0 = same as "extra text size"
        public string AboveColor = "";          // empty = same as "extra text colour"
        public bool AboveBold = false;
        public int AboveOpacity = 100;
        public int AboveOffsetX = 0;
        public int AboveOffsetY = 0;

        // ---- text below the numbers ----
        public bool ShowBelow = true;
        public string BelowFont = "";
        public int BelowSize = 0;
        public string BelowColor = "";
        public bool BelowBold = false;
        public int BelowOpacity = 100;
        public int BelowOffsetX = 0;
        public int BelowOffsetY = 0;

        // ---- date line (clock) ----
        public string DateFont = "";
        public int DateSize = 0;
        public string DateColor = "";
        public bool DateBold = false;
        public int DateOpacity = 100;
        public int DateOffsetX = 0;
        public int DateOffsetY = 0;

        // ---- extra lines (world clocks, date countdown, days since, session) ----
        public bool ShowInfo = true;
        public string InfoFont = "";
        public int InfoSize = 0;
        public string InfoColor = "";
        public bool InfoBold = false;
        public int InfoOpacity = 100;
        public int InfoOffsetX = 0;
        public int InfoOffsetY = 0;

        // ---- icon ----
        public bool ShowIcon = true;
        public string IconColor = "";           // empty = same as the number colour
        public int IconOpacity = 100;
        public int IconOffsetX = 0;
        public int IconOffsetY = 0;
        public int IconRotation = 0;

        // ---- progress ring / bar ----
        public string ProgressColor = "";       // empty = follows the state colour
        public int ProgressOpacity = 100;

        // ---- order and spacing of the parts ----
        public string PartOrder = "label,above,main,bar,date,info,below";
        public int PartSpacing = 4;
    }
}
