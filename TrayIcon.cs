// The little icon near the clock in the Windows taskbar, with its right-click menu.
using System;
using WinForms = System.Windows.Forms;

namespace BetterClock
{
    public class TrayIcon : IDisposable
    {
        private readonly WinForms.NotifyIcon _icon;
        private readonly WinForms.ToolStripMenuItem _autoStartItem;

        public TrayIcon()
        {
            var menu = new WinForms.ContextMenuStrip();

            var header = new WinForms.ToolStripMenuItem(Program.AppName + "  v" + AppInfo.Version);
            header.Enabled = false;
            menu.Items.Add(header);
            menu.Items.Add(new WinForms.ToolStripSeparator());

            menu.Items.Add(Item("Show / hide clock", delegate { AppHost.Controller.ToggleClock(); }));
            menu.Items.Add(Item("Show / hide timer", delegate { AppHost.Controller.ToggleTimer(); }));
            menu.Items.Add(Item("Show / hide countdown", delegate { AppHost.Controller.ToggleCountdown(); }));
            menu.Items.Add(Item("Move overlays (unlock / lock)", delegate { AppHost.ToggleMoveMode(); }));
            menu.Items.Add(Item("Hide all overlays", delegate { AppHost.Controller.HideAll(); }));
            menu.Items.Add(Item("Start / stop", delegate { AppHost.Controller.StartStop(); }));
            menu.Items.Add(Item("Reset", delegate { AppHost.Controller.Reset(); }));
            menu.Items.Add(new WinForms.ToolStripSeparator());

            // looks: the theme gallery and your saved presets (rebuilt every time the menu opens)
            var themesItem = new WinForms.ToolStripMenuItem("Themes");
            var presetsItem = new WinForms.ToolStripMenuItem("My presets");
            menu.Items.Add(themesItem);
            menu.Items.Add(presetsItem);
            menu.Opening += delegate
            {
                themesItem.DropDownItems.Clear();
                foreach (string themeName in LookPresets.BuiltInNames)
                {
                    string chosen = themeName;
                    themesItem.DropDownItems.Add(Item(chosen, delegate { LookPresets.ApplyBuiltIn(chosen); LookPresets.Commit(); }));
                }
                presetsItem.DropDownItems.Clear();
                var saved = LookPresets.Names();
                if (saved.Count == 0)
                {
                    var none = new WinForms.ToolStripMenuItem("(none saved yet)");
                    none.Enabled = false;
                    presetsItem.DropDownItems.Add(none);
                }
                foreach (string presetName in saved)
                {
                    string chosen = presetName;
                    presetsItem.DropDownItems.Add(Item(chosen, delegate { if (LookPresets.ApplySaved(chosen)) LookPresets.Commit(); }));
                }
            };
            menu.Items.Add(new WinForms.ToolStripSeparator());

            menu.Items.Add(Item("Settings...", delegate { SettingsWindow.ShowSingleton(); }));
            menu.Items.Add(Item("Check for updates", delegate
            {
                if (!UpdateChecker.Configured) { ShowBalloon("Better Clock", "No GitHub repository is set (edit repo.txt and build again)."); return; }
                UpdateChecker.CheckAsync(delegate(UpdateResult r)
                {
                    if (!r.Ok) { ShowBalloon("Better Clock", r.Error); return; }
                    if (!r.UpdateAvailable) { ShowBalloon("Better Clock", "You are up to date (v" + AppInfo.Version + ")."); return; }

                    var answer = System.Windows.MessageBox.Show(
                        "Version " + r.Latest + " is available (you have " + AppInfo.Version + ").\n\nUpdate now? Better Clock will restart.",
                        "Better Clock update", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                    if (answer != System.Windows.MessageBoxResult.Yes) return;

                    if (string.IsNullOrEmpty(r.DownloadUrl))
                    {
                        try { System.Diagnostics.Process.Start(r.Url); } catch (Exception) { }
                        return;
                    }
                    ShowBalloon("Better Clock update", "Downloading version " + r.Latest + "...");
                    Updater.Install(r, null, delegate(string err) { ShowBalloon("Better Clock update", err); });
                });
            }));

            _autoStartItem = new WinForms.ToolStripMenuItem("Start with Windows");
            _autoStartItem.CheckOnClick = false;
            _autoStartItem.Click += delegate
            {
                bool want = !AutoStart.IsEnabled();
                if (AutoStart.Set(want))
                {
                    AppHost.Settings.StartWithWindows = want;
                    AppHost.Settings.Save();
                }
            };
            menu.Items.Add(_autoStartItem);
            menu.Opening += delegate { _autoStartItem.Checked = AutoStart.IsEnabled(); };

            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(Item("Exit", delegate { System.Windows.Application.Current.Shutdown(); }));

            _icon = new WinForms.NotifyIcon();
            _icon.Icon = IconFactory.GetIcon();
            _icon.Text = Program.AppName;
            _icon.ContextMenuStrip = menu;
            _icon.DoubleClick += delegate { SettingsWindow.ShowSingleton(); };
            _icon.Visible = true;
        }

        private static WinForms.ToolStripMenuItem Item(string text, Action onClick)
        {
            var i = new WinForms.ToolStripMenuItem(text);
            i.Click += delegate { onClick(); };
            return i;
        }

        public void ShowBalloon(string title, string text)
        {
            _icon.ShowBalloonTip(5000, title, text, WinForms.ToolTipIcon.Info);
        }

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
