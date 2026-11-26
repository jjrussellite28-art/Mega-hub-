using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// The page for one game: wallpaper, logo, buttons, description and a details panel.
public class GameView : Grid, IKeyHandler
{
    readonly Game g;
    readonly Action back;
    readonly List<(Border box, Action act)> buttons = new();
    readonly StackPanel info = new();
    readonly TextBlock desc = Ui.Text("", 16, false, "#F0FFFFFF");
    int sel;

    public GameView(Game g, Action back, HintBar hints)
    {
        this.g = g; this.back = back;
        Background = Ui.B("#060A14");

        // wallpaper + readable fade
        var hero = new Border { IsHitTestVisible = false };
        RenderOptions.SetBitmapInterpolationMode(hero, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        hero.Background = GameLibrary.ArtBrush(g.HeroPath ?? g.CoverPath, Stretch.UniformToFill) ?? Ui.Grad(g.C1, "#0B0F1A", false);
        Children.Add(hero);
        Children.Add(new Border
        {
            IsHitTestVisible = false,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 0.2, RelativeUnit.Relative),
                GradientStops = new GradientStops { new GradientStop(Color.Parse("#EE060A14"), 0), new GradientStop(Color.Parse("#00060A14"), 1) },
            },
        });

        // left: logo, buttons, description
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(50, 0, 0, 90), MaxWidth = 820 };
        var logo = GameLibrary.LoadBitmap(g.LogoPath);
        if (logo != null)
            left.Children.Add(new Image { Source = logo, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, MaxHeight = 170, MaxWidth = 560, HorizontalAlignment = HorizontalAlignment.Left });
        else
            left.Children.Add(Ui.Text(g.Title, 44, true));
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 18, 0, 0) };
        row.Children.Add(Button("▶  Play", true, () => GameLibrary.Launch(g)));
        if (g.AppId != null)
        {
            row.Children.Add(Button("Open in Steam", false, () => Open("steam://nav/games/details/" + g.AppId)));
            row.Children.Add(Button("Uninstall", false, () => Open("steam://uninstall/" + g.AppId)));
        }
        row.Children.Add(Button("Back", false, back));
        left.Children.Add(row);
        desc.TextWrapping = TextWrapping.Wrap;
        desc.MaxHeight = 120;
        desc.Margin = new Thickness(4, 18, 0, 0);
        left.Children.Add(desc);
        Children.Add(left);

        // right: details panel
        Children.Add(new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 50, 90), Padding = new Thickness(24, 18), Width = 440,
            CornerRadius = new CornerRadius(18), Background = Ui.B("#99101A33"), BorderBrush = Ui.B("#33FFFFFF"), BorderThickness = new Thickness(1),
            Child = info,
        });

        var clock = Ui.Clock(); clock.FontSize = 22;
        clock.HorizontalAlignment = HorizontalAlignment.Right; clock.VerticalAlignment = VerticalAlignment.Top; clock.Margin = new Thickness(0, 26, 40, 0);
        Children.Add(clock);

        FillInfo(null);
        Focus();
        _ = LoadStoreInfo();
        _ = LoadIgdb(hero);
        hints.Set(("A", "Enter", "Choose"), ("B", "Esc", "Back"));
    }

    static void Open(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    Border Button(string text, bool primary, Action act)
    {
        var b = new Border
        {
            Padding = new Thickness(26, 12), Margin = new Thickness(0, 0, 12, 0), CornerRadius = new CornerRadius(26),
            Background = Ui.B(primary ? "#5AB8F5" : "#55101A33"), BorderBrush = Ui.B("#66FFFFFF"), BorderThickness = new Thickness(2),
            Child = Ui.Text(text, 17, true, primary ? "#06101F" : "#FFFFFF"),
        };
        b.PointerPressed += (_, _) => act();
        buttons.Add((b, act));
        return b;
    }

    void Focus()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool f = i == sel;
            buttons[i].box.BorderBrush = Ui.B(f ? "#FFFFFF" : "#66FFFFFF");
            buttons[i].box.BoxShadow = f ? BoxShadows.Parse("0 0 18 2 #AA7FE3FF") : default;
        }
    }

    void Line(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("130,*"), Margin = new Thickness(0, 5) };
        grid.Children.Add(Ui.Text(label, 15, false, "#BBFFFFFF"));
        var v = Ui.Text(value, 15, true);
        v.TextWrapping = TextWrapping.Wrap;
        Grid.SetColumn(v, 1);
        grid.Children.Add(v);
        info.Children.Add(grid);
    }

    record Store(string? Desc, string? Dev, string? Pub, string? Date, string? Genres, string? Esrb = null);

    // last played and play time come from Steam's own file for the signed-in account
    (DateTime? last, int minutes) PlayInfo()
    {
        try
        {
            var steam = GameLibrary.SteamPath();
            if (steam == null || g.AppId == null) return (null, 0);
            foreach (var f in Directory.EnumerateFiles(Path.Combine(steam, "userdata"), "localconfig.vdf", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(f);
                var m = Regex.Match(text, "\"" + g.AppId + "\"\\s*\\{([^{}]*)\\}");
                if (!m.Success) continue;
                var block = m.Groups[1].Value;
                long.TryParse(Regex.Match(block, "\"LastPlayed\"\\s+\"(\\d+)\"").Groups[1].Value, out var lp);
                int.TryParse(Regex.Match(block, "\"Playtime\"\\s+\"(\\d+)\"").Groups[1].Value, out var pt);
                if (lp > 0 || pt > 0) return (lp > 0 ? DateTimeOffset.FromUnixTimeSeconds(lp).LocalDateTime : null, pt);
            }
        }
        catch { }
        return (null, 0);
    }

    void FillInfo(Store? st)
    {
        info.Children.Clear();
        if (st?.Esrb != null)
            info.Children.Add(new Border { Background = Brushes.Transparent, BorderBrush = Brushes.White, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 2), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 6), Child = Ui.Text("ESRB " + st.Esrb, 15, true) });
        var (last, mins) = PlayInfo();
        if (g.AppId != null)
        {
            var days = last == null ? -1 : (int)(DateTime.Now.Date - last.Value.Date).TotalDays;
            Line("Last played", last == null ? "Not played" : days == 0 ? "Played today" : days == 1 ? "Yesterday" : last.Value.ToString("MMM d, yyyy"));
            Line("Time played", mins <= 0 ? "Not played" : mins < 60 ? mins + " min" : (mins / 60) + " h " + (mins % 60) + " min");
        }
        Line("Source", g.AppId != null ? "Steam" : g.Exe != null ? "Added by you" : null);
        Line("Developer", st?.Dev);
        Line("Publisher", st?.Pub);
        Line("Released", st?.Date);
        Line("Genres", st?.Genres);
        if (g.SizeBytes > 0) Line("Size", (g.SizeBytes / 1073741824.0).ToString("0.0") + " GB");
        Line("Installed in", g.Folder);
        if (st == null && g.AppId != null) Line("", "Loading details...");
    }

    // description, developer, publisher, date and genres come from Steam's public store page data (no login). Saved so it works offline next time.
    async System.Threading.Tasks.Task LoadStoreInfo()
    {
        if (g.AppId == null) return;
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus", "cache");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "app_" + g.AppId + ".json");
            string json;
            if (File.Exists(file)) json = await File.ReadAllTextAsync(file);
            else
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("MegaHubPlus");
                json = await http.GetStringAsync("https://store.steampowered.com/api/appdetails?appids=" + g.AppId + "&l=english");
                await File.WriteAllTextAsync(file, json);
            }
            using var doc = JsonDocument.Parse(json);
            var d = doc.RootElement.GetProperty(g.AppId).GetProperty("data");
            string? J(string name, string sep) => d.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array
                ? string.Join(sep, a.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.TryGetProperty("description", out var de) ? de.GetString() : null).Where(x => x != null)) : null;
            var st = new Store(
                d.TryGetProperty("short_description", out var sd) ? System.Net.WebUtility.HtmlDecode(sd.GetString() ?? "") : null,
                J("developers", ", "), J("publishers", ", "),
                d.TryGetProperty("release_date", out var rd) && rd.TryGetProperty("date", out var dt) ? dt.GetString() : null,
                J("genres", ", "),
                d.TryGetProperty("ratings", out var rt) && rt.TryGetProperty("esrb", out var es) && es.TryGetProperty("rating", out var er)
                    ? (er.GetString() ?? "").ToLowerInvariant() switch { "e" => "E", "e10" => "E10+", "t" => "T", "m" => "M", "ao" => "AO", "ec" => "EC", var o => o.ToUpperInvariant() } : null);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { desc.Text = st.Desc ?? ""; FillInfo(st); });
        }
        catch (Exception e)
        {
            AppLog.Write("game details failed: " + e.Message);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => FillInfo(new Store(null, null, null, null, null)));
        }
    }

    async System.Threading.Tasks.Task LoadIgdb(Border hero)
    {
        var m = await Igdb.GetAsync(g);
        if (m == null) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (m.ArtFile != null)
            {
                var bm = GameLibrary.LoadBitmap(m.ArtFile);
                var wide = bm != null && bm.Size.Height > 0 && bm.Size.Width / bm.Size.Height is > 1.6 and < 2.0;
                hero.Background = GameLibrary.ArtBrush(m.ArtFile, wide ? Stretch.Fill : Stretch.UniformToFill);
            }
            if (!string.IsNullOrWhiteSpace(m.Summary) && string.IsNullOrWhiteSpace(desc.Text)) desc.Text = m.Summary;
        });
    }

    public void OnKey(Key key)
    {
        if (key == Key.Escape) { back(); return; }
        if (key == Key.Left && sel > 0) sel--;
        else if (key == Key.Right && sel < buttons.Count - 1) sel++;
        else if (key == Key.Enter || key == Key.Space) { buttons[sel].act(); return; }
        else return;
        Focus();
    }
}
