// Undo / redo, share codes, file import and export, and "same look on all three" (v1.4.8).
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace BetterClock
{
    public partial class SettingsWindow
    {
        private readonly List<string> _hist = new List<string>();
        private int _histPos = -1;

        // Called when a change has settled (from the save timer).
        private void AfterEdit()
        {
            if (S.SyncLooks) SyncFromActiveMode();
            PushHistory();
        }

        // Copies the look of the page that is open to the other two overlays.
        private void SyncFromActiveMode()
        {
            TabItem tab = _tabs.SelectedItem as TabItem;
            if (tab == null) return;
            string h = tab.Header as string;
            if (h == null) return;
            OverlayMode from;
            if (h == "Clock") from = OverlayMode.Clock;
            else if (h == "Timer") from = OverlayMode.Timer;
            else if (h == "Countdown") from = OverlayMode.Countdown;
            else return;
            ModeStyle src = S.StyleFor(from);
            foreach (OverlayMode m in new OverlayMode[] { OverlayMode.Clock, OverlayMode.Timer, OverlayMode.Countdown })
            {
                if (m == from) continue;
                S.StyleFor(m).CopyFrom(src, true);
                RefillMode(m);
            }
            AppHost.ApplySettings();
            UpdatePreview();
        }

        private void PushHistory()
        {
            string now = ShareCode.Snapshot(S);
            if (_histPos >= 0 && _histPos < _hist.Count && _hist[_histPos] == now) return;
            if (_histPos < _hist.Count - 1) _hist.RemoveRange(_histPos + 1, _hist.Count - _histPos - 1);
            _hist.Add(now);
            if (_hist.Count > 60) _hist.RemoveAt(0);
            _histPos = _hist.Count - 1;
        }

        private void Undo() { StepHistory(-1); }
        private void Redo() { StepHistory(1); }

        private void StepHistory(int dir)
        {
            _saveTimer.Stop();
            AfterEditNoSync();
            int target = _histPos + dir;
            if (target < 0 || target >= _hist.Count) return;
            _histPos = target;
            ShareCode.Restore(S, _hist[_histPos]);
            RefillAllModes();
            AppHost.ApplySettings();
            AppHost.SaveSettings();
        }

        // Records an edit that is still waiting, without copying it to the other overlays.
        private void AfterEditNoSync()
        {
            PushHistory();
        }

        // Ctrl+Z and Ctrl+Y, unless a text box has the keyboard.
        private void OnHistoryKey(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
            if (Keyboard.FocusedElement is TextBox) return;
            if (e.Key == Key.Z) { Undo(); e.Handled = true; }
            else if (e.Key == Key.Y) { Redo(); e.Handled = true; }
        }

        // The undo, redo and "same look" controls at the top of a mode page.
        private void AddHistoryTools(WrapPanel tools)
        {
            tools.Children.Add(MakeButton("Undo", delegate { Undo(); }));
            tools.Children.Add(MakeButton("Redo", delegate { Redo(); }));
        }

        // Share code, file import / export and Randomise on the Presets tab.
        private void BuildShareSection(StackPanel page)
        {
            AnimatedSection share = new AnimatedSection("Share your look", true);
            share.Body.Children.Add(Hint("Copy a code and send it to a friend. They paste it here and get your look on all three overlays. Positions and label texts are not included."));

            var box = new TextBox();
            box.TextWrapping = TextWrapping.Wrap;
            box.AcceptsReturn = false;
            box.Height = 70;
            box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            box.Margin = new Thickness(0, 4, 0, 6);
            share.Body.Children.Add(box);

            var row = new WrapPanel();
            row.Children.Add(MakeButton("Make a code from my look", delegate { box.Text = ShareCode.Make(S); }));
            row.Children.Add(MakeButton("Copy the code", delegate
            {
                if (box.Text.Length == 0) box.Text = ShareCode.Make(S);
                try { Clipboard.SetText(box.Text); SetStatus("Code copied.", false); } catch (Exception) { }
            }));
            row.Children.Add(MakeButton("Use this code", delegate
            {
                PushHistory();
                if (ShareCode.Apply(S, box.Text))
                {
                    RefillAllModes();
                    Changed();
                    SetStatus("Look applied.", false);
                }
                else MessageBox.Show(this, "That is not a Better Clock code. It should start with BC1:", Program.AppName);
            }));
            share.Body.Children.Add(row);

            var files = new WrapPanel();
            files.Margin = new Thickness(0, 8, 0, 0);
            files.Children.Add(MakeButton("Export to a file", delegate
            {
                var dlg = new SaveFileDialog();
                dlg.Filter = "Better Clock look (*.ini)|*.ini";
                dlg.FileName = "my-look.ini";
                if (dlg.ShowDialog(this) == true)
                {
                    if (LookPresets.ExportToFile(dlg.FileName)) SetStatus("Look saved.", false);
                    else MessageBox.Show(this, "Could not save the file.", Program.AppName);
                }
            }));
            files.Children.Add(MakeButton("Import from a file", delegate
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "Better Clock look (*.ini)|*.ini|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) == true)
                {
                    PushHistory();
                    if (LookPresets.ImportFromFile(dlg.FileName))
                    {
                        RefillAllModes();
                        Changed();
                    }
                    else MessageBox.Show(this, "Could not read that file.", Program.AppName);
                }
            }));
            share.Body.Children.Add(files);
            page.Children.Add(share);

            AnimatedSection fun = new AnimatedSection("Surprise me", true);
            fun.Body.Children.Add(Hint("Makes a random look for all three overlays. Press it until you like one. Undo (Ctrl+Z) brings your old look back."));
            var fr = new WrapPanel();
            fr.Children.Add(MakeButton("Randomise my look", delegate
            {
                PushHistory();
                LookPresets.Randomise();
                RefillAllModes();
                Changed();
            }));
            fun.Body.Children.Add(fr);
            page.Children.Add(fun);
        }
    }
}
