using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// The page of one game in the Game Row layout: big picture, title, description, Play / Settings / Info.
public class GameRowPage : Grid, IKeyHandler
{
    readonly Game g;
    readonly AppSettings s;
    readonly Action back;
    readonly string accent, light, ambient;
    readonly List<(Border box, Action act)> items = new();
    readonly TextBlock genre, desc, notice;
    readonly StackPanel settingsPanel = new() { IsVisible = false };
    readonly StackPanel infoPanel = new() { IsVisible = false };
    readonly GameRowStyle.Wall wall = new(0);
    readonly Border fitBtn;
    readonly TextBlock fitText;
    Border? cover;
    int sel = 1;   // Play
    int fit;

    public GameRowPage(Game g, AppSettings s, Action back, HintBar hints)
    {
        this.g = g; this.s = s; this.back = back;
        (accent, light, ambient) = GameRowStyle.Tone(g);
        fit = s.GameFit.TryGetValue(GameRowStyle.Key(g), out var f) ? f : 0;
        Background = Ui.B("#07111E");
        Children.Add(GameRowStyle.Atmosphere(ambient));

        // picture
        var path = GameRowStyle.WallPath(g);
        var bmp = GameLibrary.LoadBitmap(path);
        Children.Add(wall);
        wall.Show(bmp, fit, 760, true);
        Opacity = 1;
        SizeChanged += (_, _) => wall.Show(bmp, fit, Bounds.Height, true);
        if (bmp == null && GameLibrary.LoadBitmap(g.CoverPath) is { } cv)
        {
            // no wide picture: the cover sits on the right, whole
            var im = new Image { Source = cv, Stretch = Stretch.Uniform, MaxHeight = 520, Margin = new Thickness(0, 0, 90, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.95 };
            Children.Add(im);
        }
        Children.Add(GameRowStyle.Scrims(true));

        var page = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), Margin = new Thickness(64, 26, 64, 70) };
        var clock = Ui.Clock(); clock.FontSize = 20;
        page.Children.Add(GameRowStyle.Header(s.ShellMode, clock));

        var backBtn = Button("Back to games", false, back);
        backBtn.HorizontalAlignment = HorizontalAlignment.Left; backBtn.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(backBtn, 1);
        page.Children.Add(backBtn);

        var copy = new StackPanel { MaxWidth = 680, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        genre = Ui.Text("", 15, true, accent);
        genre.Margin = new Thickness(0, 0, 0, 8);
        var title = Ui.Text(g.Title, 52, true); title.TextWrapping = TextWrapping.Wrap; title.LineHeight = 54; title.MaxWidth = 640;
        desc = Ui.Text("", 17, false, "#E4EFFF"); desc.TextWrapping = TextWrapping.Wrap; desc.MaxWidth = 520; desc.LineHeight = 26; desc.Margin = new Thickness(0, 16, 0, 0);
        notice = Ui.Text("", 15, false, "#EAF2FF"); notice.Margin = new Thickness(0, 14, 0, 0); notice.TextWrapping = TextWrapping.Wrap; notice.IsVisible = false;
        copy.Children.Add(genre); copy.Children.Add(title); copy.Children.Add(desc);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 24, 0, 0) };
        var play = Button("Play", true, () =>
        {
            if (GameLibrary.Launch(g)) ShowNotice("Starting " + g.Title + "...");
            else ShowNotice("Could not start " + g.Title + ". Check that it is installed.");
        });
        var settingsBtn = Button("Settings", false, () => TogglePanel(settingsPanel, infoPanel));
        var infoBtn = Button("Info", false, () => TogglePanel(infoPanel, settingsPanel));
        actions.Children.Add(play); actions.Children.Add(settingsBtn); actions.Children.Add(infoBtn);
        copy.Children.Add(actions);

        // settings panel: background fit
        fitText = Ui.Text("", 16, true);
        fitBtn = new Border { Padding = new Thickness(14, 10), CornerRadius = new CornerRadius(7), Background = Ui.B("#0B1727"), BorderBrush = Ui.B("#807AA7CE"), BorderThickness = new Thickness(1), Child = fitText, Cursor = new Cursor(StandardCursorType.Hand) };
        fitBtn.PointerPressed += (_, e) => { CycleFit(); e.Handled = true; };
        var lab = Ui.Text("Background fit", 15, true); lab.Margin = new Thickness(0, 0, 0, 6);
        settingsPanel.Children.Add(lab); settingsPanel.Children.Add(fitBtn);
        copy.Children.Add(Panel(settingsPanel));
        copy.Children.Add(Panel(infoPanel));
        copy.Children.Add(notice);
        Grid.SetRow(copy, 2);
        page.Children.Add(copy);

        // bottom: cover + source
        var dock = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Bottom };
        if (GameLibrary.LoadBitmap(g.CoverPath) is { } thumb)
            dock.Children.Add(new Border { Width = 48, Height = 72, CornerRadius = new CornerRadius(6), BorderBrush = Ui.B("#66963B7DC"), BorderThickness = new Thickness(1), ClipToBounds = true, Child = new Image { Source = thumb, Stretch = Stretch.Uniform } });
        var src = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var srcName = g.AppId != null ? "Steam" : g.Folder != null && g.Folder.Contains("Epic", StringComparison.OrdinalIgnoreCase) ? "Epic Games" : g.Exe != null ? "Added by you" : "PC";
        src.Children.Add(Ui.Text(srcName, 16, true)); src.Children.Add(Ui.Text("PC game", 14, false, "#AFC5E3"));
        dock.Children.Add(src);
        Grid.SetRow(dock, 3);
        page.Children.Add(dock);
        Children.Add(page);

        // order for the controller: Back, Play, Settings, Info (then the fit button when Settings is open)
        items.Insert(0, (backBtn, back));
        RefreshFit(); FocusRing();
        FillInfo(null);
        genre.Text = "";
        desc.Text = "";
        _ = LoadMeta();
        hints.Set(("A", "Enter", "Select"), ("B", "Esc", "Back"));
    }

    Border Panel(StackPanel inner) => new Border
    {
        Margin = new Thickness(0, 14, 0, 0), Padding = new Thickness(16, 14), MaxWidth = 560, HorizontalAlignment = HorizontalAlignment.Left,
        Background = Ui.B("#ED0A1729"), BorderBrush = Ui.B(accent), BorderThickness = new Thickness(2, 0, 0, 0), CornerRadius = new CornerRadius(0, 8, 8, 0),
        Child = inner, [!IsVisibleProperty] = inner[!IsVisibleProperty],
    };

    Border Button(string text, bool primary, Action act)
    {
        var b = new Border
        {
            MinWidth = primary ? 145 : 100, Height = 48, Padding = new Thickness(18, 0), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = Ui.B(primary ? accent : "#7782ACCF"), Background = Ui.B(primary ? accent : "#ED112339"), Cursor = new Cursor(StandardCursorType.Hand),
            Child = Ui.Text(text, 17, true, primary ? "#07111E" : "#FFFFFF"),
        };
        ((TextBlock)b.Child).HorizontalAlignment = HorizontalAlignment.Center;
        b.PointerPressed += (_, e) => { act(); e.Handled = true; };
        items.Add((b, act));
        return b;
    }

    void FocusRing()
    {
        for (int i = 0; i < items.Count; i++)
        {
            bool on = i == sel;
            items[i].box.BoxShadow = on ? BoxShadows.Parse("0 0 0 3 #89DBFF") : default;
        }
        fitBtn.BoxShadow = settingsPanel.IsVisible && sel == items.Count ? BoxShadows.Parse("0 0 0 3 #89DBFF") : default;
    }

    void TogglePanel(StackPanel show, StackPanel other)
    {
        notice.IsVisible = false;
        other.IsVisible = false;
        show.IsVisible = !show.IsVisible;
        FocusRing();
    }

    void ShowNotice(string t) { settingsPanel.IsVisible = false; infoPanel.IsVisible = false; notice.Text = t; notice.IsVisible = true; }

    void RefreshFit() => fitText.Text = fit == 0 ? "Show whole picture" : "Fill screen (crop edges)";

    void CycleFit()
    {
        fit = 1 - fit;
        s.GameFit[GameRowStyle.Key(g)] = fit; s.Save();
        RefreshFit();
        wall.Show(GameLibrary.LoadBitmap(GameRowStyle.WallPath(g)), fit, Bounds.Height, true);
    }

    // info panel: only the facts that are known
    record Meta(string? Desc, string? Dev, string? Pub, string? Date, string? Genres, string? Esrb);

    void Fact(string label, string? value, Grid grid)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        int r = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var l = Ui.Text(label, 14, false, "#A7BDDE"); l.Margin = new Thickness(0, 4, 14, 0);
        var v = Ui.Text(value, 15, true); v.TextWrapping = TextWrapping.Wrap; v.Margin = new Thickness(0, 4, 0, 0);
        Grid.SetRow(l, r); Grid.SetRow(v, r); Grid.SetColumn(v, 1);
        grid.Children.Add(l); grid.Children.Add(v);
    }

    void FillInfo(Meta? m)
    {
        infoPanel.Children.Clear();
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("150,*") };
        var (last, mins) = GameView.PlayInfo(g);
        if (g.AppId != null)
        {
            var days = last == null ? -1 : (int)(DateTime.Now.Date - last.Value.Date).TotalDays;
            Fact("Last played", last == null ? "Not played" : days == 0 ? "Played today" : days == 1 ? "Yesterday" : last.Value.ToString("MMM d, yyyy"), grid);
            Fact("Time played", mins <= 0 ? "Not played" : mins < 60 ? mins + " min" : (mins / 60) + " h " + (mins % 60) + " min", grid);
        }
        Fact("Developer", m?.Dev, grid); Fact("Publisher", m?.Pub, grid); Fact("PC release", m?.Date, grid);
        Fact("Genres", m?.Genres, grid);
        if (m?.Esrb != null) Fact("ESRB", m.Esrb, grid);
        if (g.SizeBytes > 0) Fact("Size", (g.SizeBytes / 1073741824.0).ToString("0.0") + " GB", grid);
        Fact("Installed in", g.Folder, grid);
        if (grid.Children.Count == 0) Fact("Details", g.AppId != null ? "Loading..." : "No details for this game yet.", grid);
        infoPanel.Children.Add(grid);
    }

    // description, developer and so on come from Steam's public store data (saved after the first time) and IGDB if set up
    async System.Threading.Tasks.Task LoadMeta()
    {
        Meta? m = null;
        try
        {
            if (g.AppId != null)
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
                string? J(string name) => d.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", a.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.TryGetProperty("description", out var de) ? de.GetString() : null).Where(x => x != null)) : null;
                m = new Meta(
                    d.TryGetProperty("short_description", out var sd) ? System.Net.WebUtility.HtmlDecode(sd.GetString() ?? "") : null,
                    J("developers"), J("publishers"),
                    d.TryGetProperty("release_date", out var rd) && rd.TryGetProperty("date", out var dt) ? dt.GetString() : null,
                    J("genres"),
                    d.TryGetProperty("ratings", out var rt) && rt.TryGetProperty("esrb", out var es) && es.TryGetProperty("rating", out var er)
                        ? (er.GetString() ?? "").ToLowerInvariant() switch { "e" => "E", "e10" => "E10+", "t" => "T", "m" => "M", "ao" => "AO", "ec" => "EC", var o => o.ToUpperInvariant() } : null);
            }
        }
        catch (Exception e) { AppLog.Write("game row details failed: " + e.Message); }
        string? summary = null;
        try { summary = (await Igdb.GetAsync(g))?.Summary; } catch { }
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            desc.Text = !string.IsNullOrWhiteSpace(m?.Desc) ? m!.Desc! : summary ?? "";
            genre.Text = m?.Genres?.Split(',')[0] ?? "";
            FillInfo(m ?? new Meta(null, null, null, null, null, null));
        });
    }

    public void OnKey(Key key)
    {
        if (key == Key.Escape) { back(); return; }
        int count = items.Count + (settingsPanel.IsVisible ? 1 : 0);
        if (key == Key.Down || key == Key.Right) sel = (sel + 1) % count;
        else if (key == Key.Up || key == Key.Left) sel = (sel - 1 + count) % count;
        else if (key == Key.Enter || key == Key.Space)
        {
            if (sel < items.Count) items[sel].act(); else CycleFit();
            return;
        }
        else return;
        FocusRing();
    }
}
