// Asks GitHub whether a newer release exists. It only NOTIFIES you (it never replaces the EXE by itself).
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace BetterClock
{
    public class UpdateResult
    {
        public bool Ok;
        public bool UpdateAvailable;
        public string Latest = "";
        public string Url = "";
        public string DownloadUrl = "";   // direct link to BetterClock.exe on the release
        public string Error = "";
    }

    public static class UpdateChecker
    {
        public static bool Configured
        {
            get
            {
                string r = AppInfo.Repo ?? "";
                return r.IndexOf('/') > 0 && r.IndexOf("YOUR-USERNAME", StringComparison.OrdinalIgnoreCase) < 0;
            }
        }

        public static string ReleasesUrl
        {
            get { return "https://github.com/" + AppInfo.Repo + "/releases/latest"; }
        }

        // Runs in the background; "done" is called on the UI thread.
        public static void CheckAsync(Action<UpdateResult> done)
        {
            Task.Run(() =>
            {
                UpdateResult r = Check();
                Application app = Application.Current;
                if (app != null) app.Dispatcher.BeginInvoke(new Action(delegate { done(r); }));
            });
        }

        private static UpdateResult Check()
        {
            var result = new UpdateResult();
            if (!Configured)
            {
                result.Error = "No GitHub repository is set (edit repo.txt and build again).";
                return result;
            }
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
                var req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + AppInfo.Repo + "/releases/latest");
                req.UserAgent = "BetterClock/" + AppInfo.Version;
                req.Accept = "application/vnd.github+json";
                req.Timeout = 8000;
                string json;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream()))
                {
                    json = sr.ReadToEnd();
                }

                Match tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                Match url = Regex.Match(json, "\"html_url\"\\s*:\\s*\"(https://github.com/[^\"]+/releases/tag/[^\"]+)\"");
                Match exe = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"(https://[^\"]*/BetterClock\\.exe)\"");
                if (exe.Success) result.DownloadUrl = exe.Groups[1].Value;
                if (!tag.Success)
                {
                    result.Error = "GitHub did not return a release.";
                    return result;
                }

                result.Latest = tag.Groups[1].Value.TrimStart('v', 'V');
                result.Url = url.Success ? url.Groups[1].Value : ReleasesUrl;
                result.Ok = true;
                result.UpdateAvailable = IsNewer(result.Latest, AppInfo.Version);
            }
            catch (WebException ex)
            {
                var http = ex.Response as HttpWebResponse;
                result.Error = (http != null && http.StatusCode == HttpStatusCode.NotFound)
                    ? "No release found yet on " + AppInfo.Repo + "."
                    : "Could not reach GitHub.";
            }
            catch (Exception)
            {
                result.Error = "Could not check for updates.";
            }
            return result;
        }

        private static bool IsNewer(string latest, string current)
        {
            Version a = Parse(latest);
            Version b = Parse(current);
            return a != null && b != null && a > b;
        }

        private static Version Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int dash = text.IndexOf('-');
            if (dash > 0) text = text.Substring(0, dash);
            Version v;
            return Version.TryParse(text, out v) ? v : null;
        }
    }
}
