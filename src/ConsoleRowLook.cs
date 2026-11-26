using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Look A: PS5-style top-tab row, big background, Play button, last-played panel.
public class ConsoleRowLook : LookBase
{
    readonly StackPanel gridHost = new() { Margin = new Thickness(24, 0, 24, 0), VerticalAlignment = VerticalAlignment.Top };
    readonly TextBlock title = Ui.Text("", 34, true);
    readonly Image logo = new() { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, HorizontalAlignment = HorizontalAlignment.Left, MaxHeight = 150, MaxWidth = 520, IsVisible = false };
    readonly Border dim = new();        // light tint over the wallpaper
    readonly Border hero = new();       // blurred copy fills the whole screen
    readonly Border heroSharp = new();  // the whole picture, nothing cut off
    int top;                       // first visible row of the poster grid
    static readonly string[] TabNames = { "Favorites", "Games", "Media", "Store", "Friends" };
    public static int LastTab = 1;
    int tab;
    StackPanel? tabsHost;

    static string FavKey(Game g) => g.AppId ?? g.Title;
    bool IsFav(Game g) => S.Favorites.Contains(FavKey(g));

    // which games the current tab shows (Media, Store and Friends have nothing behind them yet)
    void ApplyTab()
    {
        Games = tab == 0 ? GameLibrary.Games.Where(IsFav).ToArray() : tab == 1 ? GameLibrary.Games : Array.Empty<Game>();
    }

    void BuildTabs()
    {
        if (tabsHost == null) return;
        tabsHost.Children.Clear();
        for (int i = 0; i < TabNames.Length; i++)
        {
            int idx = i;
            var pill = Ui.Pill(TabNames[i], i == tab ? "#33FFFFFF" : "#00000000", i == tab ? "#FFFFFF" : "#CCFFFFFF", 18);
            pill.Cursor = new Cursor(StandardCursorType.Hand);
            pill.PointerPressed += (_, e) => { SetTab(idx); e.Handled = true; };
            tabsHost.Children.Add(pill);
        }
    }

    void SetTab(int i)
    {
        if (i < 0 || i >= TabNames.Length || i == tab) return;
        tab = i; LastTab = i; LastSel = 0; Sel = 0; top = 0;
        ApplyTab(); BuildTabs(); Refresh();
    }

    void ToggleFavorite()
    {
        if (Games.Length == 0) return;
        var g = Games[Sel]; var k = FavKey(g);
        if (!S.Favorites.Remove(k)) S.Favorites.Add(k);
        S.Save();
        if (tab == 0) { ApplyTab(); Sel = Math.Clamp(Sel, 0, Math.Max(Games.Length - 1, 0)); }
        Refresh();
    }
    bool menuOpen;
    int menuIdx;
    Control? overlay;
    public Action<bool>? MenuChanged;

    public ConsoleRowLook(AppSettings s) : base(s)
    {
        tab = LastTab;
        ApplyTab();
        Sel = Math.Clamp(LastSel, 0, Math.Max(Games.Length - 1, 0));
        RowDefinitions = new RowDefinitions("Auto,*,Auto");
        SizeChanged += (_, _) => Refresh();

        // big game art fills the whole screen; a dark fade keeps the text readable
        hero.IsHitTestVisible = false;
        hero.Effect = new BlurEffect { Radius = 30 };
        SetRowSpan(hero, 3);
        Children.Add(hero);
        dim.IsHitTestVisible = false;
        SetRowSpan(dim, 3);
        Children.Add(dim);
        heroSharp.IsHitTestVisible = false;
        heroSharp.VerticalAlignment = VerticalAlignment.Top;
        heroSharp.OpacityMask = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops { new GradientStop(Colors.Black, 0), new GradientStop(Colors.Black, 0.8), new GradientStop(Colors.Transparent, 1) },
        };
        SetRowSpan(heroSharp, 3);
        Children.Add(heroSharp);
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
                    new GradientStop(Color.Parse("#80060A14"), 0.45),
                    new GradientStop(Color.Parse("#00060A14"), 1),
                },
            },
        };
        SetRowSpan(fade, 3);
        Children.Add(fade);

        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(28, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        tabsHost = tabs;
        BuildTabs();
        bar.Children.Add(Ui.Text("Mega Hub +", 24, true));
        Grid.SetColumn(tabs, 1);
        bar.Children.Add(tabs);

        // right side: notification bell, clock, settings gear
        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(IconButton(S.PictureIcons ? PicIcon("notification") : BellIcon(), ShowNotifications));
        var clock = Ui.Clock();
        clock.FontSize = 26;
        clock.Margin = new Thickness(10, 0, 14, 0);
        right.Children.Add(clock);
        Control gear = S.PictureIcons ? PicIcon("settings") : Glyph("\uE713", 22);
        right.Children.Add(IconButton(gear, () => OpenSettings?.Invoke()));
        Grid.SetColumn(right, 2);
        bar.Children.Add(right);
        var barBox = new Border
        {
            Margin = new Thickness(24, 20), Padding = new Thickness(28, 14),
            CornerRadius = new CornerRadius(34), Background = Ui.B("#AA101826"),
            BorderBrush = Ui.B("#44FFFFFF"), BorderThickness = new Thickness(1), Child = bar,
            BoxShadow = BoxShadows.Parse("0 6 24 0 #66000000"),
        };
        SetRow(barBox, 0);
        Children.Add(barBox);

        SetRow(gridHost, 1);
        Children.Add(gridHost);

        // just the selected game's name at the bottom; Play and other actions live in the menu on the poster
        var bottom = new StackPanel { Margin = new Thickness(40, 0, 40, 36), VerticalAlignment = VerticalAlignment.Bottom };
        RenderOptions.SetBitmapInterpolationMode(logo, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        bottom.Children.Add(logo);
        bottom.Children.Add(title);
        SetRow(bottom, 2);
        Children.Add(bottom);

        Refresh();
    }

    // Windows' own icon font: always sharp and straight
    static TextBlock Glyph(string ch, double size) => new()
    {
        Text = ch, FontSize = size, Foreground = Ui.B("#FFFFFF"),
        FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
    };

    // glossy picture icon that changes when the mouse is over it or presses it
    static Control PicIcon(string name)
    {
        Avalonia.Media.Imaging.Bitmap? Load(string folder)
        {
            try
            {
                var u = new Uri("avares://MegaHubPlus/Assets/app/" + folder + name + ".png");
                return Avalonia.Platform.AssetLoader.Exists(u) ? new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(u)) : null;
            }
            catch { return null; }
        }
        var normal = Load(""); var hover = Load("Hover/") ?? normal; var down = Load("Pressed/") ?? normal;
        if (normal == null) return name == "settings" ? Glyph("\uE713", 22) : BellIcon();
        var im = new Image { Source = normal, Width = 36, Height = 36 };
        RenderOptions.SetBitmapInterpolationMode(im, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        im.PointerEntered += (_, _) => im.Source = hover;
        im.PointerExited += (_, _) => im.Source = normal;
        im.PointerPressed += (_, _) => im.Source = down;
        im.PointerReleased += (_, _) => im.Source = hover;
        return im;
    }

    public static int NotificationCount = 0;

    // a normal-size bell: plain white when nothing is new, bright blue with a small count badge when something is
    static Control BellIcon()
    {
        var bell = Glyph("\uEA8F", 22);
        if (NotificationCount <= 0) { bell.Foreground = Ui.B("#DDFFFFFF"); return bell; }
        bell.Foreground = Ui.B("#4C8DFF");
        var badge = new Border
        {
            Background = Ui.B("#E5484D"), CornerRadius = new CornerRadius(9), MinWidth = 18, Height = 18, Padding = new Thickness(4, 0),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -6, -8, 0),
            Child = Ui.Text(NotificationCount > 9 ? "9+" : NotificationCount.ToString(), 11, true),
        };
        var g = new Grid();
        g.Children.Add(bell);
        g.Children.Add(badge);
        return g;
    }

    // round icon button: glows blue when the mouse is over it
    static Border IconButton(Control icon, Action click)
    {
        var b = new Border
        {
            Padding = new Thickness(10), CornerRadius = new CornerRadius(12), Margin = new Thickness(2, 0),
            BorderThickness = new Thickness(2), BorderBrush = Ui.B("#00000000"), Background = Ui.B("#00000000"),
            Child = icon, Cursor = new Cursor(StandardCursorType.Hand),
        };
        b.PointerEntered += (_, _) => { b.Background = Ui.B("#22408CFF"); b.BorderBrush = Ui.B("#4C8DFF"); b.BoxShadow = BoxShadows.Parse("0 0 16 0 #664C8DFF"); };
        b.PointerExited += (_, _) => { b.Background = Ui.B("#00000000"); b.BorderBrush = Ui.B("#00000000"); b.BoxShadow = default; };
        b.PointerPressed += (_, e) => { click(); e.Handled = true; };
        return b;
    }

    bool notifOpen;

    void ShowNotifications()
    {
        if (menuOpen || notifOpen) return;
        notifOpen = true;
        var box = new StackPanel { Width = 340 };
        box.Children.Add(Ui.Text("Notifications", 20, true));
        var t = Ui.Text("You're all caught up.", 16, false, "#CCFFFFFF");
        t.Margin = new Thickness(0, 12, 0, 16);
        box.Children.Add(t);
        var close = new Border { Background = Ui.B("#F5E9D6"), CornerRadius = new CornerRadius(14), Padding = new Thickness(20, 12), Child = Ui.Text("Close", 17, true, "#1A1A1A") };
        close.PointerPressed += (_, e) => { CloseNotifications(); e.Handled = true; };
        box.Children.Add(close);
        var panel = new Border
        {
            Background = Ui.B("#EE101826"), BorderBrush = Ui.B("#44FFFFFF"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20), Padding = new Thickness(24), Child = box,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 100, 30, 0),
        };
        var dim = new Grid { Background = Ui.B("#66000000") };
        dim.Children.Add(panel);
        dim.PointerPressed += (_, _) => CloseNotifications();
        SetRowSpan(dim, 3);
        overlay = dim;
        Children.Add(overlay);
        MenuChanged?.Invoke(true);
    }

    void CloseNotifications()
    {
        notifOpen = false;
        if (overlay != null) { Children.Remove(overlay); overlay = null; }
        MenuChanged?.Invoke(false);
    }

    int Cols
    {
        get
        {
            if (Bounds.Width < 200) return 4;
            var (w, _) = Ui.Size(S.Shape, Games.Length > 0 ? Games[Sel] : null);
            return Math.Max(2, (int)((Bounds.Width - 90) / (w * 1.1 + 18)));
        }
    }

    int VisibleRows
    {
        get
        {
            if (Bounds.Height < 100) return 2;
            var (_, h) = Ui.Size(S.Shape, Games.Length > 0 ? Games[Sel] : null);
            return Math.Clamp((int)((Bounds.Height - 280) / (h * 1.1 + 24)), 1, 4);
        }
    }

    protected override void Refresh()
    {
        gridHost.Children.Clear();
        if (Games.Length == 0)
        {
            Background = Ui.B("#060A14");
            hero.Background = null;
            heroSharp.Background = null;
            title.Text = tab == 0 ? "No favorites yet. Press Y on a game to add it." : tab == 1 ? "No games found" : TabNames[tab] + " is coming soon";
            title.IsVisible = true; logo.IsVisible = false;
            return;
        }
        var g = Games[Sel];
        Background = Ui.B("#060A14");
        var path = g.HeroPath ?? g.CoverPath;
        heroSharp.Background = null;
        hero.Effect = null;
        hero.Margin = new Thickness(0);
        RenderOptions.SetBitmapInterpolationMode(hero, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        dim.Background = Ui.B(S.Wall == WallFit.Fill || S.Wall == WallFit.Crop ? "#1A060A14" : "#66060A14");   // crisp, bright wallpaper
        if (S.Wall == WallFit.Cover)
        {
            // the poster's own art as a full-screen soft backdrop: always the right shape, matches the cover, any game, any screen
            hero.Background = GameLibrary.ArtBrush(g.CoverPath ?? g.HeroPath) ?? Ui.Grad(g.C1, "#0B0F1A", false);
            hero.Effect = new BlurEffect { Radius = 34 };
            hero.Margin = new Thickness(-90);   // blur edges fall off-screen
        }
        else if (S.Wall == WallFit.Fill)
        {
            // stretched to exactly the screen: nothing cut off, no borders, works at any size
            // the game's poster itself, stretched over the whole screen so every character on it shows
            var wide = Igdb.ArtPath(g);
            if (wide != null) hero.Background = GameLibrary.ArtBrush(wide, Stretch.UniformToFill) ?? Ui.Grad(g.C1, "#0B0F1A", false);
            else
            {
                // no wide picture available: a soft blurred backdrop made from the cover, never a stretched poster
                hero.Background = GameLibrary.ArtBrush(g.CoverPath ?? g.HeroPath) ?? Ui.Grad(g.C1, "#0B0F1A", false);
                hero.Effect = new BlurEffect { Radius = 34 };
                hero.Margin = new Thickness(-90);
                dim.Background = Ui.B("#66060A14");
            }
        }
        else if (S.Wall == WallFit.Crop)
        {
            hero.Background = GameLibrary.ArtBrush(path) ?? Ui.Grad(g.C1, "#0B0F1A", false);
        }
        else
        {
            // whole picture at its own shape, softened bottom edge fills the rest
            var art = GameLibrary.ArtBrush(path);
            hero.Background = art ?? Ui.Grad(g.C1, "#0B0F1A", false);
            hero.Effect = new BlurEffect { Radius = 30 };
            if (g.HeroPath != null && art is ImageBrush full && full.Source != null)
                hero.Background = new ImageBrush(full.Source)
                {
                    Stretch = Stretch.Fill,
                    SourceRect = new RelativeRect(0, 0.90, 1, 0.10, RelativeUnit.Relative),
                };
            if (g.HeroPath != null && GameLibrary.ArtBrush(g.HeroPath, Stretch.Fill) is ImageBrush ib && ib.Source is Avalonia.Media.Imaging.Bitmap bm && Bounds.Width > 0)
            {
                heroSharp.Background = ib;
                heroSharp.Height = Bounds.Width * bm.Size.Height / bm.Size.Width;
            }
        }
        // the game's own title logo (like on Steam) when it has one, otherwise its name
        var lg = GameLibrary.LoadBitmap(g.LogoPath);
        logo.Source = lg; logo.IsVisible = lg != null;
        title.Text = g.Title; title.IsVisible = lg == null;

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
                var tile = Ui.Tile(Games[i], S.Shape, i == Sel, "#3FE0D0", IsFav(Games[i]));
                tile.PointerPressed += (_, _) => { Sel = i; Refresh(); OpenMenu(); };
                line.Children.Add(tile);
            }
            gridHost.Children.Add(line);
        }
    }

    public override void OnKey(Key key)
    {
        if (notifOpen) { if (key == Key.Escape || key == Key.Enter) CloseNotifications(); return; }
        if (menuOpen) { MenuKey(key); return; }
        if (key == Key.S || key == Key.F2) { OpenSettings?.Invoke(); return; }
        if (key == Key.PageUp || key == Key.Q) { SetTab(tab - 1); return; }
        if (key == Key.PageDown || key == Key.E) { SetTab(tab + 1); return; }
        if (key == Key.Y) { ToggleFavorite(); return; }
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

    public Action<Game>? OpenGame;
    public static int LastSel;

    void OpenMenu()
    {
        if (Games.Length == 0) return;
        if (OpenGame != null) { LastSel = Sel; OpenGame(Games[Sel]); return; }
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
