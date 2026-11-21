// Finds monitors and works out which one the game is on.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BetterClock
{
    public enum MonitorMode
    {
        GameWindow,   // the monitor the game (foreground window) is on  [default]
        Primary,      // always the primary monitor
        Specific,     // a monitor picked by number
        All           // every monitor at once
    }

    public class ScreenInfo
    {
        public IntPtr Handle;
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public bool IsPrimary;

        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }

    public static class MonitorHelper
    {
        private static readonly uint OwnPid = (uint)Process.GetCurrentProcess().Id;

        public static ScreenInfo FromHandle(IntPtr hMonitor)
        {
            var mi = new NativeMethods.MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO));
            if (hMonitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(hMonitor, ref mi)) return null;

            var s = new ScreenInfo();
            s.Handle = hMonitor;
            s.Left = mi.rcMonitor.Left;
            s.Top = mi.rcMonitor.Top;
            s.Right = mi.rcMonitor.Right;
            s.Bottom = mi.rcMonitor.Bottom;
            s.IsPrimary = (mi.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0;
            return s;
        }

        // All monitors, primary first, then left to right.
        public static List<ScreenInfo> All()
        {
            var list = new List<ScreenInfo>();
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate(IntPtr hMon, IntPtr hdc, ref NativeMethods.RECT rc, IntPtr data)
                {
                    ScreenInfo s = FromHandle(hMon);
                    if (s != null) list.Add(s);
                    return true;
                }, IntPtr.Zero);

            list.Sort(delegate(ScreenInfo a, ScreenInfo b)
            {
                if (a.IsPrimary != b.IsPrimary) return a.IsPrimary ? -1 : 1;
                return a.Left.CompareTo(b.Left);
            });
            return list;
        }

        public static ScreenInfo Primary()
        {
            foreach (ScreenInfo s in All())
            {
                if (s.IsPrimary) return s;
            }
            List<ScreenInfo> all = All();
            return all.Count > 0 ? all[0] : null;
        }

        // The monitor the game is on = the monitor of the window you are using right now.
        // If that window is Better Clock itself (for example you clicked a button in our
        // test window), use the monitor the mouse cursor is on instead.
        public static ScreenInfo ForGameWindow()
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                uint pid;
                NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
                if (pid != OwnPid)
                {
                    ScreenInfo s = FromHandle(NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST));
                    if (s != null) return s;
                }
            }

            NativeMethods.POINT pt;
            if (NativeMethods.GetCursorPos(out pt))
            {
                ScreenInfo c = FromHandle(NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST));
                if (c != null) return c;
            }
            return Primary();
        }

        // The monitors an overlay should appear on (more than one when the mode is All).
        public static List<ScreenInfo> Targets(MonitorMode mode, int index)
        {
            if (mode == MonitorMode.All)
            {
                List<ScreenInfo> all = All();
                if (all.Count > 0) return all;
            }
            var one = new List<ScreenInfo>();
            ScreenInfo s = Resolve(mode, index);
            if (s != null) one.Add(s);
            return one;
        }

        public static ScreenInfo Resolve(MonitorMode mode, int index)
        {
            switch (mode)
            {
                case MonitorMode.Primary:
                    return Primary();
                case MonitorMode.Specific:
                    List<ScreenInfo> all = All();
                    if (index >= 0 && index < all.Count) return all[index];
                    return Primary();
                default:
                    return ForGameWindow();
            }
        }
    }
}
