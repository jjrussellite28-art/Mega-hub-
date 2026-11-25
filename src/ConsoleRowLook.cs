using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Look A: PS5-style top-tab row, big background, Play button, last-played panel.
public class ConsoleRowLook : LookBase
{
    readonly StackPanel gridHost = new() { Margin = new Thickness(24, 0, 24, 0), VerticalAlignment = VerticalAlignment.Top };
    readonly TextBlock title = Ui.Text("", 34, true);
    int top;                       // first visible row of the poster grid
    bool menuOpen;
    int menuIdx;
    Control? overlay;
    public Action<bool>? MenuChanged;

    public ConsoleRowLook(AppSettings s) : base(s)
    {
        RowDefinitions = new RowDefinitions("Auto,*,Auto");
        SizeChanged += (_, _) => Refresh();

        // dark fade over the big background art so text stays readable
        var fade = new Border
        {
            IsHitTestVisible = false,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.7, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#F2060A14"), 0),
                    new GradientStop(Color.Parse("#99060A14"), 0.45),
                    new GradientStop(Color.Parse("#00060A14"), 1),
                },
            },
        };
        SetRowSpan(fade, 3);
        Children.Add(fade);

        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 0, 0, 0) };
        tabs.Children.Add(Ui.Pill("Games", "#33FFFFFF", "#FFFFFF", 14));
        foreach (var t in new[] { "Media", "Store", "Friends" })
            tabs.Children.Add(Ui.Pill(t, "#00000000", "#CCFFFFFF", 14));
        bar.Children.Add(Ui.Text("Mega Hub +", 18, true));
        Grid.SetColumn(tabs, 1);
        bar.Children.Add(tabs);
        var clock = Ui.Clock();
        Grid.SetColumn(clock, 2);
        bar.Children.Add(clock);
        var barBox = new Border
        {
            Margin = new Thickness(20), Padding = new Thickness(22, 6),
            CornerRadius = new CornerRadius(26), Background = Ui.B("#88101826"),
            BorderBrush = Ui.B("#33FFFFFF"), BorderThickness = new Thickness(1), Child = bar,
        };
        SetRow(barBox, 0);
        Children.Add(barBox);

        SetRow(gridHost, 1);
        Children.Add(gridHost);

        // just the selected game's name at the bottom; Play and other actions live in the menu on the poster
        var bottom = new StackPanel { Margin = new Thickness(40, 0, 40, 36), VerticalAlignment = VerticalAlignment.Bottom };
        bottom.Children.Add(title);
        SetRow(bottom, 2);
        Children.Add(bottom);

        Refresh();
    }

    int Cols => S.Shape == TileShape.Wide ? 4 : 6;

    int VisibleRows
    {
        get
        {
            if (Bounds.Height < 100) return 2;
            var (_, h) = Ui.Size(S.Shape);
            return Math.Clamp((int)((Bounds.Height - 230) / (h * 1.1 + 24)), 1, 4);
        }
    }

    protected override void Refresh()
    {
        gridHost.Children.Clear();
        if (Games.Length == 0)
        {
            Background = Ui.Grad("#1B2A6B", "#0B0F1A", false);
            title.Text = "No games found";
            return;
        }
        var g = Games[Sel];
        Background = GameLibrary.ArtBrush(g.HeroPath ?? g.CoverPath) ?? Ui.Grad(g.C1, "#0B0F1A", false);
        title.Text = g.Title;

        int cols = Cols, rows = VisibleRows, selRow = Sel / cols;
        if (selRow < top) top = selRow;
        if (selRow >= top + rows) top = selRow - rows + 1;
        for (int r = top; r < top + rows; r++)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            for (int c = 0; c < cols; c++)
            {
                int i = r * cols + c;
                if (i >= Games.Length) break;
                var tile = Ui.Tile(Games[i], S.Shape, i == Sel, "#3FE0D0");
                tile.PointerPressed += (_, _) => { Sel = i; Refresh(); OpenMenu(); };
                line.Children.Add(tile);
            }
            gridHost.Children.Add(line);
        }
    }

    public override void OnKey(Key key)
    {
        if (menuOpen) { MenuKey(key); return; }
        if (key == Key.S || key == Key.F2) { OpenSettings?.Invoke(); return; }
        if (Games.Length == 0) return;
        int c = Cols;
        switch (key)
        {
            case Key.Right: if (Sel < Games.Length - 1) Sel++; break;
            case Key.Left: if (Sel > 0) Sel--; break;
            case Key.Down:
                if (Sel + c < Games.Length) Sel += c;
                else if (Sel / c < (Games.Length - 1) / c) Sel = Games.Length - 1;
                break;
            case Key.Up: if (Sel - c >= 0) Sel -= c; break;
            case Key.Enter: OpenMenu(); return;
            default: return;
        }
        Refresh();
    }

    // The menu that opens on a poster: Play, Open in Steam, Close.
    List<(string label, Action act)> MenuItems()
    {
        var g = Games[Sel];
        var items = new List<(string, Action)> { ("Play", () => { CloseMenu(); GameLibrary.Launch(g); }) };
        if (g.AppId != null)
            items.Add(("Open in Steam", () =>
            {
                CloseMenu();
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("steam://nav/games/details/" + g.AppId) { UseShellExecute = true }); } catch { }
            }));
        items.Add(("Close", CloseMenu));
        return items;
    }

    void OpenMenu()
    {
        if (Games.Length == 0) return;
        menuOpen = true; menuIdx = 0;
        BuildMenu();
        MenuChanged?.Invoke(true);
    }

    void CloseMenu()
    {
        menuOpen = false;
        if (overlay != null) { Children.Remove(overlay); overlay = null; }
        MenuChanged?.Invoke(false);
    }

    void MenuKey(Key key)
    {
        var items = MenuItems();
        if (key == Key.Up && menuIdx > 0) menuIdx--;
        else if (key == Key.Down && menuIdx < items.Count - 1) menuIdx++;
        else if (key == Key.Enter) { items[menuIdx].act(); return; }
        else if (key == Key.Escape) { CloseMenu(); return; }
        else return;
        BuildMenu();
    }

    void BuildMenu()
    {
        if (overlay != null) Children.Remove(overlay);
        var box = new StackPanel { Width = 320 };
        box.Children.Add(Ui.Text(Games[Sel].Title, 18, true));
        box.Children.Add(new Border { Height = 10 });
        var items = MenuItems();
        for (int i = 0; i < items.Count; i++)
        {
            var act = items[i].act;
            bool on = i == menuIdx;
            var b = new Border
            {
                Background = Ui.B(on ? "#F5E9D6" : "#33FFFFFF"),
                CornerRadius = new CornerRadius(14), Padding = new Thickness(20, 12), Margin = new Thickness(0, 4),
                Child = Ui.Text(items[i].label, 17, true, on ? "#1A1A1A" : "#FFFFFF"),
            };
            b.PointerPressed += (_, e) => { act(); e.Handled = true; };
            box.Children.Add(b);
        }
        var panel = new Border
        {
            Background = Ui.B("#EE101826"), BorderBrush = Ui.B("#44FFFFFF"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20), Padding = new Thickness(24), Child = box,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        var dim = new Grid { Background = Ui.B("#99000000") };
        dim.Children.Add(panel);
        dim.PointerPressed += (_, _) => CloseMenu();
        SetRowSpan(dim, 3);
        overlay = dim;
        Children.Add(overlay);
    }
}
