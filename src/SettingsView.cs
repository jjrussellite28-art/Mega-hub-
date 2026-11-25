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

        col.Children.Add(Section("Look"));
        grid.Add(new());
        var looks = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (kind, name) in new[] { (LookKind.ConsoleRow, "Console Row"), (LookKind.Blades, "Blades"), (LookKind.WideHome, "Wide Home") })
            looks.Children.Add(Option(name, s.Look == kind, () => { s.Look = kind; s.Save(); Build(); }));
        col.Children.Add(looks);

        col.Children.Add(Section("Tile shape"));
        grid.Add(new());
        var shapes = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (shape, name) in new[] { (TileShape.Wide, "Wide 16:9"), (TileShape.Short, "Short 3:4"), (TileShape.Poster, "Tall poster 2:3") })
            shapes.Children.Add(Option(name, s.Shape == shape, () => { s.Shape = shape; s.Save(); Build(); }));
        col.Children.Add(shapes);

        col.Children.Add(Section("Mode"));
        grid.Add(new());
        var modes = new StackPanel { Orientation = Orientation.Horizontal };
        modes.Children.Add(Option("App mode (window)", !s.ShellMode, () => { s.ShellMode = false; s.Save(); applyMode(); Build(); }));
        modes.Children.Add(Option("Shell mode (full screen)", s.ShellMode, () => { s.ShellMode = true; s.Save(); applyMode(); Build(); }));
        col.Children.Add(modes);
        var modeHelp = Ui.Text("Stuck? Press Ctrl+Alt+Q, or hold Back + Start on the controller for 3 seconds.", 13, false, "#CCFFFFFF");
        modeHelp.Margin = new Thickness(4, 4, 0, 0);
        col.Children.Add(modeHelp);

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

    // Downloads the newest installer from GitHub, runs it quietly, and closes this app so it can be replaced.
    void DoUpdate()
    {
        if (updating) return;
        updating = true;
        status.Text = "Checking GitHub for the newest version...";
        Build();
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MegaHubPlus-Update.exe");
                using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("MegaHubPlus-Updater");
                    using var resp = await http.GetAsync("https://github.com/jjrussellite28-art/Mega-hub-/releases/download/Beta/MegaHubPlus-Setup.exe");
                    resp.EnsureSuccessStatusCode();
                    await using var fs = System.IO.File.Create(file);
                    await resp.Content.CopyToAsync(fs);
                }
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { status.Text = "Installing the update. Mega Hub + will restart."; Build(); });
                await System.Threading.Tasks.Task.Delay(800);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS") { UseShellExecute = true });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { updating = false; status.Text = "Update failed: " + ex.Message + " (nothing was changed)"; Build(); });
            }
        });
    }

    // Removes saved data. Removes the program folder too, but only for a real install
    // (never a developer build, and only a folder that is clearly Mega Hub +'s own).
    void DoUninstall()
    {
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
