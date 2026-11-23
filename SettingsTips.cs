// The little help texts shown when you hover a setting.
using System.Collections.Generic;

namespace BetterClock
{
    public static class SettingsTips
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>();

        public static string Get(string field)
        {
            string tip;
            return Map.TryGetValue(field, out tip) ? tip : null;
        }

        private static void T(string field, string tip) { Map[field] = tip; }

        static SettingsTips()
        {
            // ---- where ----
            T("Position", "Which of the nine spots on the screen it sits in.");
            T("EdgeMargin", "How far it stays away from the screen edge.");
            T("MonitorMode", "Which monitor it shows on: the game's, the main one, a chosen one, or all of them.");
            T("MonitorIndex", "Which monitor to use when a specific one is chosen.");
            T("UseCustomPosition", "Use exact pixel numbers (or a drag in move mode) instead of the nine-spot grid.");
            T("CustomX", "Pixels from the left edge of the monitor.");
            T("CustomY", "Pixels from the top edge of the monitor.");

            // ---- text ----
            T("FontFamily", "The font of the numbers.");
            T("FontSize", "How big the numbers are.");
            T("FontBold", "Make the numbers bold.");
            T("FontItalic", "Slant the numbers.");
            T("TextShadow", "A shadow behind the numbers so they stay readable.");

            // ---- text effects (1.4.5) ----
            T("OutlineSize", "A coloured line around every number. 0 = off.");
            T("OutlineColor", "Colour of the outline.");
            T("NumberGradient", "Fade the numbers from their normal colour to a second colour.");
            T("NumberColor2", "The colour the gradient fades to.");
            T("NumberGradientAngle", "Direction of the fade: 0 = left to right, 90 = top to bottom.");
            T("NumberGlowSize", "A soft glow around the numbers. 0 = off.");
            T("NumberGlowColor", "Colour of the glow. Empty = same as the numbers.");
            T("NumberGlowStrength", "1 = soft, 3 = very bright.");
            T("TextShadowColor", "Colour of the shadow behind the numbers.");
            T("TextShadowBlur", "How soft the shadow is.");
            T("TextShadowDistance", "How far the shadow sits from the numbers.");
            T("TextShadowAngle", "Where the shadow falls: 0 = right, 90 = up, 180 = left, 270 = down.");
            T("TextShadowOpacity", "How dark the shadow is.");
            T("LetterSpacing", "Extra space between the numbers.");
            T("LineHeight", "Height of the line the numbers sit in. 100 = normal.");
            T("TextCase", "UPPERCASE or lowercase for the label, the text above and below, the date and the extra lines.");
            T("FontFile", "Your own font (.ttf or .otf) for the numbers. It is copied into the Better Clock folder.");
            T("FontFileForAll", "Also use the font file for the label, text above and below, date and extra lines.");
            T("Separator", "What goes between hours, minutes and seconds.");
            T("LeadingZero", "Show 05:00 (on) or 5:00 (off).");
            T("TextColor", "The normal colour of the numbers.");
            T("RunningColor", "Colour while the timer or countdown is running.");
            T("PausedColor", "Colour while it is paused.");
            T("FinishedColor", "Colour when the countdown reaches zero.");
            T("WarnColor", "Colour when the countdown is running low (amber).");
            T("DangerColor", "Colour when the countdown is almost out (red).");
            T("TargetColor", "Colour when the timer reaches its target.");

            // ---- numbers, extra text, icon ----
            T("DigitStyle", "How the numbers are drawn: normal, flip cards, seven-segment, terminal or analog (clock only).");
            T("BlinkColon", "Make the colon in the time blink every half second.");
            T("TextAbove", "A line of your own text above the numbers.");
            T("TextBelow", "A line of your own text below the numbers.");
            T("ExtraTextSize", "Size of the extra lines (above, below, date, world clocks).");
            T("ExtraTextColor", "Colour of the extra lines.");
            T("ShowDayName", "Show the day of the week under the clock.");
            T("ShowDate", "Show the date under the clock.");
            T("Icon", "A small picture next to the numbers.");
            T("IconPath", "Your own picture for the icon (set Icon to Custom).");
            T("IconSize", "How big the icon is.");
            T("IconOnRight", "Put the icon on the right of the numbers instead of the left.");
            T("ShowLabel", "Show a small label above the numbers.");
            T("LabelText", "The words in the label.");
            T("LabelSize", "Size of the label.");
            T("LabelColor", "Colour of the label.");

            // ---- background and shape ----
            T("BackgroundStyle", "Solid colour, a gradient between two colours, or no background.");
            T("BackgroundColor", "The background colour.");
            T("BackgroundColor2", "The second colour for a gradient.");
            T("GradientAngle", "The direction of the gradient in degrees.");
            T("BackgroundOpacity", "How see-through the background is. 100 is solid.");
            T("BackgroundImagePath", "A picture shown behind the numbers.");
            T("BackgroundImageOpacity", "How strong the background picture is.");
            T("BackgroundImageFit", "Cover fills the whole pill, Fit shows all of the picture, Stretch squeezes it.");
            T("CornerRadius", "How round the corners are. High numbers make a pill shape.");
            T("PaddingH", "Empty space to the left and right of the numbers.");
            T("PaddingV", "Empty space above and below the numbers.");
            T("BorderColor", "The colour of the border line.");
            T("BorderThickness", "How thick the border is. 0 means no border.");
            T("ShadowEnabled", "A glow or shadow around the whole overlay.");
            T("ShadowBlur", "How big and soft the glow is.");

            // ---- size and animation ----
            T("Scale", "Makes the whole overlay bigger or smaller.");
            T("OverallOpacity", "How see-through the whole overlay is.");
            T("ShowAnimation", "How it appears: fade, slide, pop or nothing.");
            T("FadeMs", "How long the fade in and out takes, in milliseconds.");

            // ---- progress ----
            T("ProgressStyle", "A bar under the numbers or a ring beside them that shows how much time is left (or done).");
            T("ProgressThickness", "How thick the bar or ring line is.");

            // ---- each part on its own (1.4.4) ----
            T("ShowNumbers", "Show or hide the numbers (handy if you only want the label or the ring).");
            T("NumbersOpacity", "How see-through the numbers are.");
            T("NumbersOffsetX", "Nudge the numbers left (minus) or right (plus).");
            T("NumbersOffsetY", "Nudge the numbers up (minus) or down (plus).");
            T("LabelFont", "Font of the label. Empty = Segoe UI.");
            T("LabelBold", "Make the label bold.");
            T("LabelOpacity", "How see-through the label is.");
            T("LabelOffsetX", "Nudge the label left (minus) or right (plus).");
            T("LabelOffsetY", "Nudge the label up (minus) or down (plus).");
            T("AboveFont", "Font of the text above. Empty = Segoe UI.");
            T("AboveBold", "Make the text above bold.");
            T("AboveOpacity", "How see-through the text above is.");
            T("AboveOffsetX", "Nudge the text above left (minus) or right (plus).");
            T("AboveOffsetY", "Nudge the text above up (minus) or down (plus).");
            T("BelowFont", "Font of the text below. Empty = Segoe UI.");
            T("BelowBold", "Make the text below bold.");
            T("BelowOpacity", "How see-through the text below is.");
            T("BelowOffsetX", "Nudge the text below left (minus) or right (plus).");
            T("BelowOffsetY", "Nudge the text below up (minus) or down (plus).");
            T("DateFont", "Font of the date line. Empty = Segoe UI.");
            T("DateBold", "Make the date line bold.");
            T("DateOpacity", "How see-through the date line is.");
            T("DateOffsetX", "Nudge the date line left (minus) or right (plus).");
            T("DateOffsetY", "Nudge the date line up (minus) or down (plus).");
            T("InfoFont", "Font of the extra lines. Empty = Segoe UI.");
            T("InfoBold", "Make the extra lines bold.");
            T("InfoOpacity", "How see-through the extra lines is.");
            T("InfoOffsetX", "Nudge the extra lines left (minus) or right (plus).");
            T("InfoOffsetY", "Nudge the extra lines up (minus) or down (plus).");
            T("IconOpacity", "How see-through the icon is.");
            T("IconOffsetX", "Nudge the icon left (minus) or right (plus).");
            T("IconOffsetY", "Nudge the icon up (minus) or down (plus).");
            T("AboveSize", "Size of the text above. 0 = use the Extra text size.");
            T("AboveColor", "Colour of the text above. Empty = use the Extra text colour.");
            T("BelowSize", "Size of the text below. 0 = use the Extra text size.");
            T("BelowColor", "Colour of the text below. Empty = use the Extra text colour.");
            T("DateSize", "Size of the date line. 0 = use the Extra text size.");
            T("DateColor", "Colour of the date line. Empty = use the Extra text colour.");
            T("InfoSize", "Size of the extra lines. 0 = use the Extra text size.");
            T("InfoColor", "Colour of the extra lines. Empty = use the Extra text colour.");
            T("ShowAbove", "Show or hide the text above the numbers.");
            T("ShowBelow", "Show or hide the text below the numbers.");
            T("ShowInfo", "Show or hide the extra lines (world clocks, date countdown and so on).");
            T("ShowIcon", "Show or hide the icon.");
            T("IconColor", "Colour of the icon. Empty = same as the numbers.");
            T("IconRotation", "Turn the icon (degrees).");
            T("ProgressColor", "Colour of the ring or bar. Empty = follows the state colour.");
            T("ProgressOpacity", "How see-through the ring or bar is.");
            T("PartSpacing", "Space between the parts, in pixels.");
            T("PartOrder", "The order the parts are stacked in.");

            T("ProgressSize", "How big the ring is.");
            T("PulseSeconds", "The glow pulses in the last this-many seconds. 0 turns it off.");
            T("PulseColor", "Colour of the pulsing glow.");

            // ---- clock behaviour ----
            T("Clock24Hour", "Show 14:30 instead of 2:30 PM.");
            T("ClockShowSeconds", "Show seconds on the clock.");
            T("ClockHoldOpen", "Keep the clock on screen until you hide it.");
            T("ClockShowForSeconds", "How long the clock stays when you press its key.");

            // ---- clock extras ----
            T("AlarmEnabled", "Ring at the alarm time.");
            T("AlarmTime", "The alarm time in 24-hour form, like 07:30.");
            T("AlarmLabel", "The message shown when the alarm rings.");
            T("AlarmRepeatDaily", "Ring every day. If off, it rings once and switches itself off.");
            T("WorldClocks", "Other cities shown under the clock. Format: Name|TimeZone, separated by ;");
            T("DateCountdownEnabled", "Count down to a date and time.");
            T("DateCountdownName", "What the date is for, like a release day.");
            T("DateCountdownTarget", "Date and time, like 2026-12-25 18:00.");
            T("DaysSinceEnabled", "Show how many days since a date.");
            T("DaysSinceName", "What the date was.");
            T("DaysSinceDate", "The date, like 2026-01-31.");
            T("SessionShow", "Show how long Better Clock has been running (your play session).");
            T("BreakReminderEnabled", "Show a break reminder now and then.");
            T("BreakEveryMinutes", "How many minutes between break reminders.");

            // ---- timer ----
            T("TimerKeepRunningHidden", "Keep counting even when the timer is hidden.");
            T("TimerTargetEnabled", "The timer turns green and chimes at a time you choose.");
            T("TimerTargetSeconds", "The target time in seconds. 300 is 5 minutes.");
            T("TimerTargetBeep", "Play a chime when the target is reached.");

            // ---- countdown ----
            T("CountdownKeepRunningHidden", "Keep counting even when the countdown is hidden.");
            T("CountdownAutoShowAtZero", "Pop the countdown up by itself when it hits zero.");
            T("CountdownFlash", "Flash the numbers at zero.");
            T("CountdownSoundEnabled", "Play a sound at zero.");
            T("CountdownSoundVolume", "How loud your chosen sound file is.");
            T("CountdownSoundRepeat", "How many times the sound plays.");
            T("CountdownOvertime", "After zero, keep counting up (+00:05) instead of stopping.");
            T("CountdownLowColors", "Turn amber and then red as time runs low.");
            T("CountdownAmberAt", "Seconds left when it turns amber.");
            T("CountdownRedAt", "Seconds left when it turns red.");
            T("CountdownWarnBeeps", "Short beeps at the seconds listed.");
            T("CountdownWarnList", "Seconds left to beep at, separated by commas, like 10,5,3,2,1.");
            T("CountdownPlan", "Single is one plain countdown. Chain runs your steps one after another. Pomodoro does work and break cycles.");
            T("CountdownChain", "Steps like 25:00 Work;5:00 Break. A plain number means minutes.");
            T("CountdownChainLoop", "Start the chain again when it ends.");
            T("PomoWorkMin", "Length of a work round, in minutes.");
            T("PomoShortMin", "Length of a short break, in minutes.");
            T("PomoLongMin", "Length of the long break after the last round.");
            T("PomoRounds", "How many work rounds before the long break.");

            // ---- app ----
            T("ColorTheme", "Dark, Light, or System (follows Windows).");
            T("AccentColor", "The highlight colour of this window.");
            T("AnimationsEnabled", "Smooth movements in this window.");
            T("AnimationSpeed", "How fast the window animations are.");
            T("UiScale", "Makes this whole settings window bigger or smaller.");
            T("StartWithWindows", "Start Better Clock when you log in.");
            T("CheckForUpdates", "Look for a new version when Better Clock starts.");
            T("AutoInstallUpdates", "Install new versions by itself and restart.");
            T("AutoSwitchGames", "Switch looks automatically when a game from your list is running.");
        }
    }
}
