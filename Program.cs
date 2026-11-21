// Better Clock - entry point.
// Tips:  BetterClock.exe --slow      makes the loading screen last about 6 seconds
//        BetterClock.exe --startup   used by "Start with Windows" (opens quietly in the tray)
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BetterClock
{
    public static class Program
    {
        public const string AppName = "Better Clock";

        private class Step
        {
            public string Name;
            public double Weight;
            public Action Work;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            bool slow = false;
            bool startup = false;
            foreach (string a in args)
            {
                if (string.Equals(a, "--slow", StringComparison.OrdinalIgnoreCase)) slow = true;
                if (string.Equals(a, "--startup", StringComparison.OrdinalIgnoreCase)) startup = true;
            }

            // Only one copy at a time. A second launch just opens the settings of the first.
            if (!SingleInstance.TryAcquire())
            {
                SingleInstance.SignalExisting();
                return 0;
            }

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.Exit += delegate
            {
                AppHost.Shutdown();
                SingleInstance.Release();
            };
            app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
            {
                MessageBox.Show("Better Clock hit a problem:\n\n" + e.Exception.Message, AppName,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            };

            // Settings are read first so we know whether to show the loading screen.
            AppHost.InitSettings();
            UiAnim.Enabled = AppHost.Settings.AnimationsEnabled;
            UiAnim.SpeedPercent = AppHost.Settings.AnimationSpeed;

            SplashWindow splash = null;
            if (AppHost.Settings.ShowSplash)
            {
                splash = new SplashWindow();
                splash.Show();
            }

            app.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await RunStartup(app, splash, slow, startup);
            }), DispatcherPriority.Background);

            return app.Run();
        }

        private static async Task RunStartup(Application app, SplashWindow splash, bool slow, bool startup)
        {
            try
            {
                int pause = splash == null ? 0 : (slow ? 1200 : 420);

                var steps = new Step[]
                {
                    new Step { Name = "Loading settings...",       Weight = 15, Work = delegate { AppHost.InitSettings(); } },
                    new Step { Name = "Preparing the overlay...",  Weight = 25, Work = delegate { AppHost.InitOverlay(); } },
                    new Step { Name = "Registering hotkeys...",    Weight = 20, Work = delegate { AppHost.InitHotkeys(); } },
                    new Step { Name = "Watching for games...",     Weight = 5,  Work = delegate { AppHost.Games = new GameWatcher(); } },
                    new Step { Name = "Starting tray icon...",     Weight = 15, Work = delegate { AppHost.Tray = new TrayIcon(); } },
                    new Step { Name = "Almost ready...",           Weight = 20, Work = delegate { ListenForSecondLaunch(app); } }
                };

                double done = 0;
                foreach (Step step in steps)
                {
                    if (splash != null) splash.SetStatus(step.Name);
                    step.Work();
                    if (pause > 0) await Task.Delay(pause);
                    done += step.Weight;
                    if (splash != null) splash.SetTarget(done);
                }

                if (splash != null)
                {
                    await splash.WaitUntilFull();
                    splash.SetStatus("Ready");
                    await Task.Delay(350);
                }

                if (AppHost.Settings.ShowStartupMessage && AppHost.Tray != null)
                {
                    AppHost.Tray.ShowBalloon("Better Clock is running",
                        AppHost.Settings.KeyClockToggle + " clock   " + AppHost.Settings.KeyTimerToggle + " timer   " +
                        AppHost.Settings.KeyCountdownToggle + " countdown\nRight-click the tray icon for settings.");
                }

                if (AppHost.Settings.CheckForUpdates && UpdateChecker.Configured)
                {
                    UpdateChecker.CheckAsync(delegate(UpdateResult r)
                    {
                        if (r.UpdateAvailable && AppHost.Tray != null)
                            AppHost.Tray.ShowBalloon("Better Clock update", "Version " + r.Latest + " is available. Right-click the tray icon > Check for updates.");
                    });
                }

                AppSettings st = AppHost.Settings;
                bool updated = st.LastSeenVersion != AppInfo.Version;
                string firstTab = null;
                if (updated)
                {
                    // First start after an update: show what is new (not on a quiet Windows start).
                    if (!string.IsNullOrEmpty(st.LastSeenVersion)) firstTab = "Changelog";
                    if (!startup) { st.LastSeenVersion = AppInfo.Version; st.Save(); }
                }
                if (!startup && (st.OpenSettingsOnLaunch || firstTab != null)) SettingsWindow.ShowSingleton(firstTab);
                if (splash != null) splash.FadeOutAndClose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Better Clock could not start:\n\n" + ex.Message, AppName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                app.Shutdown();
            }
        }

        private static void ListenForSecondLaunch(Application app)
        {
            SingleInstance.Listen(delegate
            {
                app.Dispatcher.BeginInvoke(new Action(SettingsWindow.ShowSingleton));
            });
        }
    }
}
