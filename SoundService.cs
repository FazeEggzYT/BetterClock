// Plays the countdown alarm: your chosen sound file (mp3, wav, wma...) or a built-in beep.
using System;
using System.IO;
using System.Media;
using System.Windows.Media;
using System.Windows.Threading;

namespace BetterClock
{
    public static class SoundService
    {
        private static MediaPlayer _player;
        private static DispatcherTimer _beepTimer;
        private static int _remaining;

        // force = true ignores the "sound enabled" setting (used by the Test button).
        public static void PlayCountdownAlarm(AppSettings s, bool force)
        {
            Stop();
            if (!s.CountdownSoundEnabled && !force) return;

            _remaining = ColorUtil.Clamp(s.CountdownSoundRepeat, 1, 10);

            string path = s.CountdownSoundPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                PlayFile(path, ColorUtil.Clamp(s.CountdownSoundVolume, 0, 100) / 100.0);
            }
            else
            {
                PlayBeeps();
            }
        }

        public static void Stop()
        {
            _remaining = 0;
            if (_beepTimer != null)
            {
                _beepTimer.Stop();
                _beepTimer = null;
            }
            if (_player != null)
            {
                try { _player.Stop(); _player.Close(); } catch (Exception) { }
                _player = null;
            }
        }

        private static void PlayFile(string path, double volume)
        {
            try
            {
                var p = new MediaPlayer();
                _player = p;
                p.Volume = volume;
                p.MediaEnded += delegate
                {
                    if (_player != p) return;
                    _remaining--;
                    if (_remaining > 0)
                    {
                        p.Position = TimeSpan.Zero;
                        p.Play();
                    }
                };
                p.MediaFailed += delegate
                {
                    if (_player != p) return;
                    _player = null;
                    PlayBeeps();   // the file couldn't be played: use the built-in beep instead
                };
                p.Open(new Uri(path, UriKind.Absolute));
                p.Play();
            }
            catch (Exception)
            {
                PlayBeeps();
            }
        }

        private static void PlayBeeps()
        {
            if (_remaining < 1) _remaining = 1;
            SystemSounds.Exclamation.Play();
            _remaining--;
            if (_remaining < 1) return;

            _beepTimer = new DispatcherTimer();
            _beepTimer.Interval = TimeSpan.FromMilliseconds(900);
            _beepTimer.Tick += delegate
            {
                SystemSounds.Exclamation.Play();
                _remaining--;
                if (_remaining < 1 && _beepTimer != null)
                {
                    _beepTimer.Stop();
                    _beepTimer = null;
                }
            };
            _beepTimer.Start();
        }
    }
}
