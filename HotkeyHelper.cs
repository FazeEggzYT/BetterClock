// Global hotkeys that work while a game has focus.
using System;
using System.Collections.Generic;
using System.Windows.Interop;

namespace BetterClock
{
    public class HotkeyHelper : IDisposable
    {
        private readonly HwndSource _source;
        private readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();
        private int _nextId = 1;

        public HotkeyHelper()
        {
            // A hidden "message-only" window that just receives hotkey messages.
            var p = new HwndSourceParameters("BetterClockHotkeys");
            p.ParentWindow = NativeMethods.HWND_MESSAGE;
            p.Width = 0;
            p.Height = 0;
            _source = new HwndSource(p);
            _source.AddHook(WndProc);
        }

        // Returns an id (> 0) on success, or 0 if another program already owns that key.
        public int Register(uint modifiers, uint virtualKey, Action action)
        {
            int id = _nextId++;
            bool ok = NativeMethods.RegisterHotKey(_source.Handle, id,
                modifiers | NativeMethods.MOD_NOREPEAT, virtualKey);
            if (!ok) return 0;
            _actions[id] = action;
            return id;
        }

        public void UnregisterAll()
        {
            foreach (int id in new List<int>(_actions.Keys))
            {
                NativeMethods.UnregisterHotKey(_source.Handle, id);
            }
            _actions.Clear();
        }

        public void Unregister(int id)
        {
            if (_actions.ContainsKey(id))
            {
                NativeMethods.UnregisterHotKey(_source.Handle, id);
                _actions.Remove(id);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                Action a;
                if (_actions.TryGetValue(wParam.ToInt32(), out a))
                {
                    handled = true;
                    try { a(); } catch (Exception) { }
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (int id in new List<int>(_actions.Keys))
            {
                NativeMethods.UnregisterHotKey(_source.Handle, id);
            }
            _actions.Clear();
            _source.Dispose();
        }
    }
}
