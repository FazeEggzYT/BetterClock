// What changed in each version. Shown in the Changelog tab of the settings window.
using System.Collections.Generic;

namespace BetterClock
{
    public class ChangelogEntry
    {
        public string Version;
        public string Date;
        public string Title;
        public string[] Items;
    }

    public static class Changelog
    {
        public static List<ChangelogEntry> Entries()
        {
            var list = new List<ChangelogEntry>();

            list.Add(new ChangelogEntry
            {
                Version = "1.4.1",
                Date = "21 Nov 2026",
                Title = "Time features",
                Items = new string[]
                {
                    "New: alarm clock - rings at a time you choose and shows your message on the clock.",
                    "New: world clocks - other cities shown as lines under the clock.",
                    "New: countdown to a date and time (for example a release day).",
                    "New: days since a date.",
                    "New: session timer shows how long you have been playing.",
                    "New: break reminder every N minutes (default 45).",
                    "New: timer results history in the Timer tab (saved when you press Reset)."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.4.0",
                Date = "21 Nov 2026",
                Title = "Countdown and timer power",
                Items = new string[]
                {
                    "New: progress ring or bar for the countdown (empties) and the timer target (fills).",
                    "New: countdown turns amber, then red, as time runs low.",
                    "New: glowing pulse in the last 10 seconds of a countdown.",
                    "New: timer target - the timer turns green and chimes when it reaches your time.",
                    "New: overtime - the countdown keeps counting up (+00:05) after zero.",
                    "New: chained countdowns, for example 25:00 Work then 5:00 Break, with the step name shown above.",
                    "New: Pomodoro mode with work, short break and long break cycles.",
                    "New: warning beeps at 10, 5, 3, 2 and 1 seconds left (you can change the list).",
                    "Fixed the Update now button not showing when a new version is available."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.3.2",
                Date = "21 Nov 2026",
                Title = "Update button fix",
                Items = new string[]
                {
                    "Fixed the Update now button not showing when a new version is available."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.3.1",
                Date = "21 Nov 2026",
                Title = "Bug fix",
                Items = new string[]
                {
                    "Fixed timer not disappearing."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.3.0",
                Date = "21 Nov 2026",
                Title = "Updates inside the app",
                Items = new string[]
                {
                    "Update from inside Better Clock: it downloads the new version, installs it and restarts itself.",
                    "Tray menu > Check for updates asks \"Update now?\". Settings > Updates has an Update now button.",
                    "Optional: install new versions automatically when Better Clock starts.",
                    "Your settings are kept across updates."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.2.0",
                Date = "21 Nov 2026",
                Title = "Number styles, pictures and extra text",
                Items = new string[]
                {
                    "Number styles: Normal, Flip-clock cards, LED seven-segment, Retro terminal, and an Analog clock face (clock only).",
                    "Blinking colon option.",
                    "Custom text above and below the numbers (for example \"Round 2\").",
                    "Optional day name and date lines under the clock.",
                    "A small icon next to the numbers: pick one or use your own picture.",
                    "A picture or logo behind the numbers, with opacity and fit options.",
                    "Drag the overlays with the mouse (Move mode, Ctrl+Alt+M). They can even be dropped on another monitor.",
                    "Exact X / Y position boxes next to the 3x3 grid.",
                    "\"All monitors\" option: one overlay on every monitor at the same time. Each overlay can also use its own monitor.",
                    "New Games tab: each game can have its own looks that switch on while that program is running.",
                    "Update check: tells you when a newer version is on GitHub (tray menu, Settings tab, and at startup)."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.1.0",
                Date = "21 Nov 2026",
                Title = "Looks, themes and animations",
                Items = new string[]
                {
                    "New Changelog tab (this one). It also opens by itself the first time after an update.",
                    "New Settings tab: dark mode, light mode or follow Windows, plus an accent colour.",
                    "Animations everywhere: the window, tabs, switches, buttons, folding cards, theme change. Turn them off or change the speed in Settings.",
                    "UI size slider (80% - 150%) for the settings window.",
                    "Clock, Timer and Countdown each have their own tab with their OWN look and their OWN position/monitor.",
                    "All three can be on screen at the same time, each in a different place.",
                    "Per-mode show animation: Fade, Slide, Pop or None.",
                    "Copy this look to the other modes, or reset one mode's look, with one click.",
                    "New key: Hide all overlays (Ctrl+Alt+H). Optional extra keys that only control the Timer or only the Countdown.",
                    "Export and import your settings as a file, open the settings folder, reset everything.",
                    "Better live preview: shows all three looks and dims the ones you are not editing.",
                    "Your v1.0 settings are carried over automatically."
                }
            });

            list.Add(new ChangelogEntry
            {
                Version = "1.0.0",
                Date = "21 Nov 2026",
                Title = "First release",
                Items = new string[]
                {
                    "Clock, Timer (counts up) and Countdown overlay that sits on top of games and videos.",
                    "Click-through and never steals focus, works over borderless/windowed fullscreen.",
                    "Shows on the monitor the game is on, a chosen monitor, or the main one.",
                    "9 screen positions, full text, colour, background, border and shadow customisation.",
                    "Global hotkeys, tray icon, start with Windows.",
                    "Startup loading screen with percentage and a \"Better Clock is running\" message.",
                    "Countdown with your own sound, flashing and auto-show at zero."
                }
            });

            return list;
        }
    }
}
