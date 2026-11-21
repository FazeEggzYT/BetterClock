// Makes sure only one Better Clock runs. A second launch just opens the settings of the first.
using System;
using System.Threading;

namespace BetterClock
{
    public static class SingleInstance
    {
        private const string MutexName = "Local\\BetterClock_Instance";
        private const string EventName = "Local\\BetterClock_ShowSettings";
        private static Mutex _mutex;
        private static EventWaitHandle _event;

        public static bool TryAcquire()
        {
            bool created;
            _mutex = new Mutex(true, MutexName, out created);
            if (created)
            {
                _event = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            }
            return created;
        }

        public static void SignalExisting()
        {
            try
            {
                EventWaitHandle ev;
                if (EventWaitHandle.TryOpenExisting(EventName, out ev))
                {
                    ev.Set();
                    ev.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        // Runs onSignal (on a background thread) each time another copy is launched.
        public static void Listen(Action onSignal)
        {
            if (_event == null) return;
            var t = new Thread(delegate()
            {
                while (true)
                {
                    try
                    {
                        _event.WaitOne();
                        onSignal();
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        public static void Release()
        {
            try
            {
                if (_mutex != null) { _mutex.ReleaseMutex(); _mutex.Close(); _mutex = null; }
            }
            catch (Exception)
            {
            }
        }
    }
}
