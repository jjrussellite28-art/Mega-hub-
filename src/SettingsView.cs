using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Settings tab: look, tile shape, and Power (Update, Uninstall). Buttons are here for now and may move later.
public class SettingsView : Grid, IKeyHandler
{
    readonly AppSettings s;
    readonly Action back;
    readonly Action applyMode;
    readonly StackPanel col = new() { Margin = new Thickness(60, 40) };
    readonly TextBlock status = Ui.Text("", 14, false, "#F5C542");
    bool confirmUninstall;
    bool confirmShell;
    // controller/keyboard focus: rows of buttons
    readonly List<List<(Border box, Action act)>> grid = new();
    int fr, fc;

    public SettingsView(AppSettings s, Action back, Action applyMode)
    {
        this.s = s;
        this.back = back;
        this.applyMode = applyMode;
        Background = Ui.Grad("#0B0F1A", "#1A1030");
        Children.Add(col);
        Build();
    }

    void Build()
    {
        col.Children.Clear();
        grid.Clear();
        col.Children.Add(Ui.Text("Settings", 34, true));

        col.Children.Add(Section("Wallpaper"));
        grid.Add(new());
        var walls = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (w, name) in new[] { (WallFit.Fill, "Fill the screen"), (WallFit.Crop, "Crop to fit"), (WallFit.Fit, "Show whole picture") })
            walls.Children.Add(Option(name, s.Wall == w, () => { s.Wall = w; s.Save(); Build(); }));
        col.Children.Add(walls);

        col.Children.Add(Section("Tile shape"));
        grid.Add(new());
        var shapes = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (shape, name) in new[] { (TileShape.Wide, "Wide 16:9"), (TileShape.Short, "Short 3:4"), (TileShape.Poster, "Tall poster 2:3") })
            shapes.Children.Add(Option(name, s.Shape == shape, () => { s.Shape = shape; s.Save(); Build(); }));
        col.Children.Add(shapes);

        col.Children.Add(Section("Mode"));
        grid.Add(new());
        var modes = new StackPanel { Orientation = Orientation.Horizontal };
        modes.Children.Add(Option("Desktop mode (normal Windows)", !s.ShellMode, DesktopMode));
        modes.Children.Add(Option("Shell mode (replaces desktop)", s.ShellMode, () => { confirmShell = true; confirmUninstall = false; status.Text = ""; Build(); }));
        col.Children.Add(modes);
        var modeHelp = Ui.Text("Stuck? Press Ctrl+Alt+Q, or hold Back + Start on the controller for 3 seconds. You get the normal desktop back.", 13, false, "#CCFFFFFF");
        modeHelp.Margin = new Thickness(4, 4, 0, 0);
        col.Children.Add(modeHelp);

        if (confirmShell)
        {
            var warn = new StackPanel();
            var t = Ui.Text("Shell mode replaces your Windows desktop with Mega Hub + the next time you sign in (no taskbar, no Start menu). Switch back any time with Desktop mode. If Mega Hub + ever closes or crashes, the normal desktop comes back by itself.", 15, true, "#F5C542");
            t.TextWrapping = TextWrapping.Wrap;
            t.MaxWidth = 600;
            warn.Children.Add(t);
            grid.Add(new());
            var r = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            r.Children.Add(Option("Yes, make it my shell", false, EnableShell));
            r.Children.Add(Option("Cancel (B)", false, () => { confirmShell = false; Build(); }));
            warn.Children.Add(r);
            col.Children.Add(new Border
            {
                Margin = new Thickness(4, 12, 0, 0), Padding = new Thickness(16),
                Background = Ui.B("#44F5C542"), BorderBrush = Ui.B("#F5C542"), BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12), HorizontalAlignment = HorizontalAlignment.Left, Child = warn,
            });
        }
        if (s.ShellMode && ShellControl.IsRegistered && ShellControl.ExplorerRunning())
        {
            grid.Add(new());
            var so = new StackPanel { Orientation = Orientation.Horizontal };
            so.Children.Add(Option("Sign out now to start Shell mode", false, ShellControl.SignOut));
            col.Children.Add(so);
        }

        col.Children.Add(Section("Games"));
        var info = Ui.Text(GameLibrary.Games.Length >= 0 ? GameLibrary.Info : "", 14, false, "#CCFFFFFF");
        info.Margin = new Thickness(4, 0, 0, 0);
        col.Children.Add(info);

        col.Children.Add(Section("Power"));
        grid.Add(new());
        var power = new StackPanel { Orientation = Orientation.Horizontal };
        power.Children.Add(Option("Update Mega Hub +", false, () =>
        {
            DoUpdate();
        }));
        power.Children.Add(Option("Copy log", false, () => { _ = CopyLog(); }));
        power.Children.Add(Option("Quit Mega Hub +", false, () => Environment.Exit(0)));
        power.Children.Add(Option("Uninstall...", false, () => { confirmUninstall = true; status.Text = ""; Build(); }));
        col.Children.Add(power);

        if (confirmUninstall)
        {
            var warn = new StackPanel();
            var t = Ui.Text("WARNING: this removes Mega Hub + and everything it saved. Everything. Nothing is left behind.", 15, true, "#FF8A8A");
            t.TextWrapping = TextWrapping.Wrap;
            t.MaxWidth = 560;
            warn.Children.Add(t);
            grid.Add(new());
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            row.Children.Add(Option("Yes, remove everything", false, DoUninstall));
            row.Children.Add(Option("Cancel", false, () => { confirmUninstall = false; Build(); }));
            warn.Children.Add(row);
            col.Children.Add(new Border
            {
                Margin = new Thickness(4, 12, 0, 0), Padding = new Thickness(16),
                Background = Ui.B("#44FF4040"), BorderBrush = Ui.B("#FF6060"), BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12), HorizontalAlignment = HorizontalAlignment.Left, Child = warn,
            });
        }

        status.Margin = new Thickness(4, 12, 0, 0);
        col.Children.Add(status);

        grid.Add(new());
        var done = Option("Back to home  (B / Esc)", false, back);
        done.Margin = new Thickness(4, 30, 0, 0);
        col.Children.Add(done);
        ApplyFocus();
    }

    bool updating;

    void DesktopMode()
    {
        confirmShell = false;
        ShellControl.Disable();
        if (!ShellControl.ExplorerRunning()) ShellControl.StartExplorer();
        s.ShellMode = false; s.Save(); applyMode();
        status.Text = "Desktop mode is on. Windows will start normally.";
        Build();
    }

    void EnableShell()
    {
        confirmShell = false;
        if (!ShellControl.Enable())
        {
            status.Text = "Shell mode can only be turned on from the installed copy of Mega Hub +.";
            Build();
            return;
        }
        s.ShellMode = true; s.Save(); applyMode();
        status.Text = "Shell mode is set. It starts the next time you sign in.";
        Build();
    }

    async System.Threading.Tasks.Task CopyLog()
    {
        try
        {
            var text = AppLog.Tail(200);
            var cb = TopLevel.GetTopLevel(this)?.Clipboard;
            if (cb != null && text.Length > 0) { await cb.SetTextAsync(text); status.Text = "Log copied. Paste it to send it."; }
            else status.Text = "Log file is here: " + AppLog.FilePath;
        }
        catch (Exception ex) { status.Text = "Could not copy the log: " + ex.Message; }
        Build();
    }

    static void VerifyInstaller(string file, long size, string? digest)
    {
        var fi = new FileInfo(file);
        if (fi.Length < 5_000_000 || (size > 0 && fi.Length != size)) throw new Exception("the download is incomplete");
        var bytes = File.ReadAllBytes(file);
        if (bytes.Length < 2 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z') throw new Exception("the download is not a Windows program");
        if (digest != null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            var h = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            if (!h.Equals(digest.Substring(7), StringComparison.OrdinalIgnoreCase)) throw new Exception("the download does not match GitHub's checksum");
        }
    }

    // Downloads the newest installer from GitHub, checks it, runs it quietly, and closes this app so it can be replaced.
    void DoUpdate()
    {
        if (updating) return;
        updating = true;
        status.Text = "Checking GitHub for the newest version...";
        Build();
        System.Threading.Tasks.Task.Run(async () =>
        {
            var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MegaHubPlus-Update.exe");
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("MegaHubPlus-Updater");
                var json = await http.GetStringAsync("https://api.github.com/repos/jjrussellite28-art/Mega-hub-/releases/tags/Beta");
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                string? url = null; long size = 0; string? digest = null;
                foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray())
                    if (a.GetProperty("name").GetString() == "MegaHubPlus-Setup.exe")
                    {
                        url = a.GetProperty("browser_download_url").GetString();
                        size = a.GetProperty("size").GetInt64();
                        if (a.TryGetProperty("digest", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.String) digest = d.GetString();
                        break;
                    }
                if (url == null) throw new Exception("no installer found on GitHub");
                using (var resp = await http.GetAsync(url))
                {
                    resp.EnsureSuccessStatusCode();
                    await using var fs = File.Create(file);
                    await resp.Content.CopyToAsync(fs);
                }
                VerifyInstaller(file, size, digest);
                AppLog.Write("update: downloaded and checked " + size + " bytes");
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { status.Text = "Installing the update. Mega Hub + will restart."; Build(); });
                await System.Threading.Tasks.Task.Delay(800);
                Process.Start(new ProcessStartInfo(file, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS") { UseShellExecute = true });
                ShellControl.SkipRestore = true;
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                AppLog.Write("update failed: " + ex.Message);
                try { File.Delete(file); } catch { }
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { updating = false; status.Text = "Update failed: " + ex.Message + " (nothing was changed)"; Build(); });
            }
        });
    }

    // Removes saved data. Removes the program folder too, but only for a real install
    // (never a developer build, and only a folder that is clearly Mega Hub +'s own).
    void DoUninstall()
    {
        ShellControl.Disable();
        if (!ShellControl.ExplorerRunning()) ShellControl.StartExplorer();
        try
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus");
            if (Directory.Exists(data)) Directory.Delete(data, true);
        }
        catch { }

        var baseDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var folderName = Path.GetFileName(baseDir);
        bool dev = baseDir.Contains(@"\bin\Debug", StringComparison.OrdinalIgnoreCase)
                || baseDir.Contains(@"\bin\Release", StringComparison.OrdinalIgnoreCase);
        bool ownFolder = folderName.Contains("MegaHubPlus", StringComparison.OrdinalIgnoreCase)
                      || folderName.Contains("Mega Hub Plus", StringComparison.OrdinalIgnoreCase);

        // Installed copy: run the real uninstaller (removes program, shortcuts and registry entry).
        var unins = Path.Combine(baseDir, "unins000.exe");
        if (!dev && File.Exists(unins))
        {
            try { Process.Start(new ProcessStartInfo(unins, "/VERYSILENT") { UseShellExecute = true }); } catch { }
            Environment.Exit(0);
        }

        confirmUninstall = false;
        if (dev || !ownFolder)
        {
            status.Text = "Saved data removed. Program files were left in place because this is a developer build (or not a recognised install folder).";
            Build();
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 4 >nul & rmdir /s /q \"" + baseDir + "\"")
            { CreateNoWindow = true, UseShellExecute = false });
        }
        catch { }
        Environment.Exit(0);
    }

    static Control Section(string t)
    {
        var tb = Ui.Text(t, 18, true, "#3FE0D0");
        tb.Margin = new Thickness(0, 28, 0, 8);
        return tb;
    }

    Border Option(string text, bool on, Action click)
    {
        var b = new Border
        {
            Background = Ui.B(on ? "#553FE0D0" : "#33FFFFFF"),
            BorderBrush = Ui.B(on ? "#3FE0D0" : "#33FFFFFF"),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(22, 12),
            Margin = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Ui.Text(text, 15, true),
        };
        b.PointerPressed += (_, _) => click();
        grid[^1].Add((b, click));
        return b;
    }

    void ApplyFocus()
    {
        fr = Math.Clamp(fr, 0, grid.Count - 1);
        fc = Math.Clamp(fc, 0, grid[fr].Count - 1);
        for (int i = 0; i < grid.Count; i++)
            for (int j = 0; j < grid[i].Count; j++)
            {
                var b = grid[i][j].box;
                bool f = i == fr && j == fc;
                b.BorderBrush = f ? Brushes.White : b.BorderBrush;
                if (f) b.BorderThickness = new Thickness(3);
            }
    }

    public void OnKey(Key key)
    {
        if (key == Key.Escape && confirmShell) { confirmShell = false; Build(); return; }
        if (key == Key.Escape) { if (confirmUninstall) { confirmUninstall = false; Build(); } else back(); return; }
        if (key == Key.Up && fr > 0) fr--;
        else if (key == Key.Down && fr < grid.Count - 1) fr++;
        else if (key == Key.Left && fc > 0) fc--;
        else if (key == Key.Right && fc < grid[fr].Count - 1) fc++;
        else if (key == Key.Enter || key == Key.Space) { grid[fr][fc].act(); return; }
        else return;
        Build();
    }
}
