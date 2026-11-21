// Downloads the newest BetterClock.exe from the GitHub release, swaps it in, and restarts the app.
// Windows lets a running EXE be renamed, so: current -> .old, new -> current, start the new one, exit.
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace BetterClock
{
    public static class Updater
    {
        private static bool _busy;

        public static bool Busy { get { return _busy; } }

        private static string ExePath
        {
            get { return Process.GetCurrentProcess().MainModule.FileName; }
        }

        // Called at every start: remove the previous version left over from an update.
        public static void CleanUp()
        {
            try
            {
                string old = ExePath + ".old";
                if (File.Exists(old)) File.Delete(old);
                string partial = ExePath + ".new";
                if (File.Exists(partial)) File.Delete(partial);
            }
            catch (Exception)
            {
            }
        }

        // status: progress text (UI thread). done: success flag + message (UI thread; not called on success because the app restarts).
        public static void Install(UpdateResult update, Action<string> status, Action<string> failed)
        {
            if (_busy) return;
            if (update == null || string.IsNullOrEmpty(update.DownloadUrl))
            {
                if (failed != null) failed("This release has no BetterClock.exe attached yet.");
                return;
            }
            _busy = true;

            Task.Run(() =>
            {
                string error = null;
                try
                {
                    string exe = ExePath;
                    string fresh = exe + ".new";
                    string old = exe + ".old";

                    Report(status, "Downloading version " + update.Latest + "...");
                    Download(update.DownloadUrl, fresh, status);

                    // sanity checks: big enough and really a Windows program ("MZ")
                    var info = new FileInfo(fresh);
                    bool ok = info.Length > 100 * 1024;
                    if (ok)
                    {
                        using (var fs = File.OpenRead(fresh))
                        {
                            ok = fs.ReadByte() == 'M' && fs.ReadByte() == 'Z';
                        }
                    }
                    if (!ok)
                    {
                        try { File.Delete(fresh); } catch (Exception) { }
                        throw new InvalidDataException("The download was not a valid program.");
                    }

                    Report(status, "Installing...");
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(exe, old);
                    try
                    {
                        File.Move(fresh, exe);
                    }
                    catch (Exception)
                    {
                        File.Move(old, exe);   // put the working version back
                        throw;
                    }

                    // start the new one after this one has closed (so the single-instance lock is free)
                    var psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c ping -n 4 127.0.0.1 >nul & start \"\" \"" + exe + "\" --updated";
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);

                    Application app = Application.Current;
                    if (app != null) app.Dispatcher.BeginInvoke(new Action(delegate { app.Shutdown(); }));
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    error = "Better Clock is in a folder it can't change. Move BetterClock.exe to somewhere like Documents and try again.";
                }
                catch (IOException ex)
                {
                    error = "Could not replace the file: " + ex.Message;
                }
                catch (WebException)
                {
                    error = "The download failed. Check your internet and try again.";
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                _busy = false;
                Application a2 = Application.Current;
                if (a2 != null && failed != null)
                    a2.Dispatcher.BeginInvoke(new Action(delegate { failed(error); }));
            });
        }

        private static void Report(Action<string> status, string text)
        {
            Application app = Application.Current;
            if (app != null && status != null)
                app.Dispatcher.BeginInvoke(new Action(delegate { status(text); }));
        }

        private static void Download(string url, string path, Action<string> status)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "BetterClock/" + AppInfo.Version;
            req.Timeout = 20000;
            req.ReadWriteTimeout = 20000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (Stream input = resp.GetResponseStream())
            using (var output = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                long total = resp.ContentLength;
                long done = 0;
                int lastPercent = -1;
                var buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    done += read;
                    if (total > 0)
                    {
                        int percent = (int)(done * 100 / total);
                        if (percent != lastPercent && percent % 5 == 0)
                        {
                            lastPercent = percent;
                            Report(status, "Downloading... " + percent + "%");
                        }
                    }
                }
            }
        }
    }
}
