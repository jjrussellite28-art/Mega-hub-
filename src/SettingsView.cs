using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Settings: categories on the left, the rows for the chosen category on the right.
public class SettingsView : Grid, IKeyHandler
{
    record Row(string Label, string Value, Action? Act);

    static readonly (string name, string icon)[] Cats =
    {
        ("Appearance", "\uE790"),
        ("Startup & Modes", "\uE7F4"),
        ("Games", "\uE7FC"),
        ("Power", "\uE7E8"),
    };
    static readonly (WallFit v, string name)[] Walls =
    {
        (WallFit.Fill, "Fill the screen"), (WallFit.Crop, "Crop to fit"),
        (WallFit.Fit, "Show whole picture"), (WallFit.Cover, "Cover art (blurred)"),
    };
    static readonly (TileShape v, string name)[] Shapes =
    {
        (TileShape.Wide, "Wide 16:9"), (TileShape.Short, "Short 3:4"), (TileShape.Poster, "Tall poster 2:3"),
    };
    const string Icons = "Segoe Fluent Icons, Segoe MDL2 Assets";

    readonly AppSettings s;
    readonly Action back;
    readonly Action applyMode;
    readonly TextBlock status = Ui.Text("", 14, false, "#F5C542");
    bool confirmUninstall;
    bool confirmShell;
    int cat, ri;
    bool inRight;
    List<Row> rows = new();

    public SettingsView(AppSettings s, Action back, Action applyMode)
    {
        this.s = s;
        this.back = back;
        this.applyMode = applyMode;
        Background = Ui.Grad("#080C18", "#0A2460");
        Build();
    }

    List<Row> RowsFor(int c, List<(string text, string color)> notes)
    {
        var r = new List<Row>();
        switch (c)
        {
            case 0:
                var wi = Array.FindIndex(Walls, w => w.v == s.Wall);
                r.Add(new("Wallpaper", Walls[Math.Max(wi, 0)].name, () => { s.Wall = Walls[(Math.Max(wi, 0) + 1) % Walls.Length].v; s.Save(); Build(); }));
                var si = Array.FindIndex(Shapes, w => w.v == s.Shape);
                r.Add(new("Tile shape", Shapes[Math.Max(si, 0)].name, () => { s.Shape = Shapes[(Math.Max(si, 0) + 1) % Shapes.Length].v; s.Save(); Build(); }));
                break;
            case 1:
                if (confirmShell)
                {
                    notes.Add(("Shell mode replaces your Windows desktop with Mega Hub + the next time you sign in (no taskbar, no Start menu). Switch back any time with Desktop mode. If Mega Hub + ever closes or crashes, the normal desktop comes back by itself.", "#F5C542"));
                    r.Add(new("Yes, make it my shell", "", EnableShell));
                    r.Add(new("Cancel", "", () => { confirmShell = false; Build(); }));
                }
                else
                {
                    r.Add(new("Desktop mode (normal Windows)", s.ShellMode ? "" : "On", DesktopMode));
                    r.Add(new("Shell mode (replaces desktop)", s.ShellMode ? "On" : "Off", () => { confirmShell = true; confirmUninstall = false; status.Text = ""; Build(); }));
                    if (s.ShellMode && ShellControl.IsRegistered && ShellControl.ExplorerRunning())
                        r.Add(new("Sign out now to start Shell mode", "", ShellControl.SignOut));
                    notes.Add(("Stuck? Press Ctrl+Alt+Q, or hold Back + Start on the controller for 3 seconds. You get the normal desktop back.", "#CCFFFFFF"));
                }
                break;
            case 2:
                r.Add(new("Games found", GameLibrary.Info, null));
                break;
            default:
                r.Add(new("Update Mega Hub +", "", DoUpdate));
                r.Add(new("Copy log", "", () => { _ = CopyLog(); }));
                r.Add(new("Quit Mega Hub +", "", () => Environment.Exit(0)));
                if (confirmUninstall)
                {
                    notes.Add(("WARNING: this removes Mega Hub + and everything it saved. Everything. Nothing is left behind.", "#FF8A8A"));
                    r.Add(new("Yes, remove everything", "", DoUninstall));
                    r.Add(new("Cancel", "", () => { confirmUninstall = false; Build(); }));
                }
                else r.Add(new("Uninstall...", "", () => { confirmUninstall = true; status.Text = ""; Build(); }));
                break;
        }
        return r;
    }

    void Build()
    {
        (status.Parent as Panel)?.Children.Remove(status);
        Children.Clear();
        RowDefinitions.Clear();
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        RowDefinitions.Add(new RowDefinition(GridLength.Star));

        // top bar
        var bar = new Border
        {
            Margin = new Thickness(24, 20, 24, 0), Padding = new Thickness(22, 12),
            Background = Ui.B("#331A2A4A"), BorderBrush = Ui.B("#33FFFFFF"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
        };
        var barRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        barRow.Children.Add(Ui.Text("Mega Hub +", 20, true));
        var clock = Ui.Clock(); clock.FontSize = 24; Grid.SetColumn(clock, 1);
        barRow.Children.Add(clock);
        bar.Child = barRow;
        Children.Add(bar);

        var body = new Grid { Margin = new Thickness(40, 20, 40, 60), ColumnDefinitions = new ColumnDefinitions("340,*"), RowDefinitions = new RowDefinitions("Auto,*") };
        Grid.SetRow(body, 1);
        Children.Add(body);

        var title = Ui.Text("Settings", 34, true);
        title.Margin = new Thickness(0, 0, 0, 14);
        Grid.SetColumnSpan(title, 2);
        body.Children.Add(title);

        // left: categories
        var left = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        for (int i = 0; i < Cats.Length; i++)
        {
            int idx = i;
            bool sel = i == cat;
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(new TextBlock { Text = Cats[i].icon, FontFamily = new FontFamily(Icons), FontSize = 22, Foreground = Brushes.White, Width = 44, VerticalAlignment = VerticalAlignment.Center });
            line.Children.Add(Ui.Text(Cats[i].name, 17, sel));
            var item = new Border
            {
                Padding = new Thickness(18, 16), Margin = new Thickness(0, 0, 0, 4), CornerRadius = new CornerRadius(8),
                Background = Ui.B(sel ? (inRight ? "#22FFFFFF" : "#44408CFF") : "#00000000"),
                BorderBrush = Ui.B(sel && !inRight ? "#4C8DFF" : "#00000000"), BorderThickness = new Thickness(sel ? 2 : 0),
                Child = line,
            };
            item.PointerPressed += (_, _) => { cat = idx; ri = 0; inRight = false; Build(); };
            left.Children.Add(item);
        }
        var leftBox = new Border { Background = Ui.B("#22101A33"), CornerRadius = new CornerRadius(14), Padding = new Thickness(8), VerticalAlignment = VerticalAlignment.Top, Child = left };
        Grid.SetRow(leftBox, 1);
        body.Children.Add(leftBox);

        // right: rows of the chosen category
        var notes = new List<(string text, string color)>();
        rows = RowsFor(cat, notes);
        ri = Math.Clamp(ri, 0, Math.Max(rows.Count - 1, 0));
        var right = new StackPanel();
        right.Children.Add(Ui.Text(Cats[cat].name, 24, true));
        ((TextBlock)right.Children[0]).Margin = new Thickness(0, 0, 0, 12);
        for (int i = 0; i < rows.Count; i++)
        {
            int idx = i;
            var row = rows[i];
            bool f = inRight && i == ri;
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            g.Children.Add(Ui.Text(row.Label, 17));
            var val = Ui.Text(row.Value, 16, false, "#CCFFFFFF");
            val.Margin = new Thickness(12, 0, 8, 0);
            Grid.SetColumn(val, 1);
            g.Children.Add(val);
            if (row.Act != null)
            {
                var chev = Ui.Text("\u203A", 26, false);
                Grid.SetColumn(chev, 2);
                g.Children.Add(chev);
            }
            var rb = new Border
            {
                Padding = new Thickness(22, 14), Margin = new Thickness(0, 0, 0, 8), CornerRadius = new CornerRadius(10),
                Background = Ui.B(f ? "#33408CFF" : "#22FFFFFF"),
                BorderBrush = Ui.B(f ? "#4C8DFF" : "#00000000"), BorderThickness = new Thickness(2),
                Child = g,
            };
            if (f) rb.BoxShadow = BoxShadows.Parse("0 0 18 1 #664C8DFF");
            rb.PointerPressed += (_, _) => { inRight = true; ri = idx; row.Act?.Invoke(); if (row.Act == null) Build(); };
            right.Children.Add(rb);
        }
        foreach (var (text, color) in notes)
        {
            var n = Ui.Text(text, 14, false, color);
            n.TextWrapping = TextWrapping.Wrap;
            n.MaxWidth = 640; n.HorizontalAlignment = HorizontalAlignment.Left;
            n.Margin = new Thickness(4, 6, 0, 0);
            right.Children.Add(n);
        }
        status.Margin = new Thickness(4, 12, 0, 0);
        status.TextWrapping = TextWrapping.Wrap;
        right.Children.Add(status);
        var scroll = new ScrollViewer { Content = right, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        Grid.SetColumn(scroll, 1);
        Grid.SetRow(scroll, 1);
        body.Children.Add(scroll);
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

    public void OnKey(Key key)
    {
        if (key == Key.Escape)
        {
            if (confirmShell) { confirmShell = false; Build(); }
            else if (confirmUninstall) { confirmUninstall = false; Build(); }
            else if (inRight) { inRight = false; Build(); }
            else back();
            return;
        }
        if (!inRight)
        {
            if (key == Key.Up && cat > 0) { cat--; ri = 0; }
            else if (key == Key.Down && cat < Cats.Length - 1) { cat++; ri = 0; }
            else if ((key == Key.Right || key == Key.Enter || key == Key.Space) && rows.Count > 0) { inRight = true; ri = 0; }
            else return;
            Build();
            return;
        }
        if (key == Key.Up && ri > 0) ri--;
        else if (key == Key.Down && ri < rows.Count - 1) ri++;
        else if (key == Key.Left) inRight = false;
        else if (key == Key.Enter || key == Key.Space) { rows[ri].Act?.Invoke(); return; }
        else return;
        Build();
    }
}
