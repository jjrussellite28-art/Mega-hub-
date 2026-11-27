using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Home layout "Game Row": one row of covers, the title big underneath, the picture of the game behind everything.
public class GameRowLook : LookBase
{
    public static int LastSel;
    public Action<Game>? OpenGame;

    readonly StackPanel row = new() { Orientation = Orientation.Horizontal, Spacing = 14 };
    readonly ScrollViewer scroller;
    readonly TextBlock source = Ui.Text("", 15, false, "#D2E3F5");
    readonly TextBlock title = Ui.Text("", 56, true);
    readonly Border openBtn;
    readonly TextBlock empty = Ui.Text("No games found yet. Add folders in Settings › Games.", 20, false, "#CCFFFFFF");
    readonly GameRowStyle.Wall[] walls = { new(), new() };
    readonly Grid atmosphereHost = new();
    readonly List<Border> cards = new();
    GameRowReveal? reveal;
    int front = 1;
    int request;
    double cardW = 130;

    static bool IsFav(AppSettings s, Game g) => s.Favorites.Contains(GameRowStyle.Key(g));

    public GameRowLook(AppSettings s) : base(s)
    {
        Games = GameLibrary.Games.OrderBy(g => IsFav(s, g) ? 0 : 1).ToArray();
        Sel = Math.Clamp(LastSel, 0, Math.Max(Games.Length - 1, 0));
        Background = Ui.B("#07111E");

        Children.Add(atmosphereHost);
        Children.Add(walls[0]); Children.Add(walls[1]);
        Children.Add(GameRowStyle.Scrims(false));

        var page = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), Margin = new Thickness(64, 26, 64, 70) };

        // header: brand, mode, clock, settings
        var clock = Ui.Clock(); clock.FontSize = 20;
        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        right.Children.Add(clock);
        var gear = new Border
        {
            Padding = new Thickness(9), CornerRadius = new CornerRadius(8), Background = Ui.B("#E8101C30"), BorderBrush = Ui.B("#558BC4FF"), BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = "", FontFamily = new FontFamily("Segoe MDL2 Assets, Segoe Fluent Icons"), FontSize = 18, Foreground = Brushes.White },
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        gear.PointerPressed += (_, e) => { OpenSettings?.Invoke(); e.Handled = true; };
        right.Children.Add(gear);
        var header = GameRowStyle.Header(s.ShellMode, right);
        page.Children.Add(header);

        // the row of covers
        var lib = new StackPanel { Margin = new Thickness(0, 22, 0, 0) };
        var heading = Ui.Text("Games", 16, true);
        heading.Margin = new Thickness(0, 0, 0, 10);
        lib.Children.Add(heading);
        scroller = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Content = row };
        lib.Children.Add(scroller);
        Grid.SetRow(lib, 1);
        page.Children.Add(lib);

        // title block, bottom left
        var summary = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Left };
        source.Margin = new Thickness(0, 0, 0, 8);
        title.TextWrapping = TextWrapping.Wrap; title.LineHeight = 58;
        openBtn = new Border
        {
            Margin = new Thickness(0, 22, 0, 0), Padding = new Thickness(22, 12), CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left, Cursor = new Cursor(StandardCursorType.Hand),
            Child = Ui.Text("Game actions", 17, true, "#F7FBFF"),
        };
        openBtn.PointerPressed += (_, e) => { Open(); e.Handled = true; };
        summary.Children.Add(source); summary.Children.Add(title); summary.Children.Add(openBtn);
        empty.IsVisible = false; empty.TextWrapping = TextWrapping.Wrap;
        summary.Children.Add(empty);
        Grid.SetRow(summary, 2);
        page.Children.Add(summary);
        Children.Add(page);

        BuildCards();
        reveal = new GameRowReveal(PaintSelection, PaintBackdrop);
        AttachedToVisualTree += (_, _) =>
        {
            if (Games.Length > 0) reveal.Select(Games[Sel], true); else PaintSelection(null);
        };
        DetachedFromVisualTree += (_, _) => { reveal?.Dispose(); reveal = null; };
        SizeChanged += (_, _) => { ResizeCards(); reveal?.Repaint(); };
    }

    void BuildCards()
    {
        row.Children.Clear(); cards.Clear();
        for (int i = 0; i < Games.Length; i++)
        {
            int idx = i; var g = Games[i];
            var art = GameLibrary.ArtBrush(g.CoverPath, Stretch.Uniform);
            var inner = new Grid();
            if (art == null)
            {
                var (a, _, _) = GameRowStyle.Tone(g);
                var t = Ui.Text(g.Title, 15, true); t.TextWrapping = TextWrapping.Wrap; t.TextAlignment = TextAlignment.Center;
                t.VerticalAlignment = VerticalAlignment.Center; t.Margin = new Thickness(8);
                inner.Children.Add(t);
            }
            if (IsFav(S, g))
            {
                var star = Ui.Text("★", 20, true, "#FFD54A");
                star.HorizontalAlignment = HorizontalAlignment.Right; star.VerticalAlignment = VerticalAlignment.Top; star.Margin = new Thickness(0, 2, 6, 0);
                inner.Children.Add(star);
            }
            var card = new Border
            {
                Background = art ?? Ui.B("#152A43"), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(2),
                BorderBrush = Ui.B("#55A1BBDF"), Opacity = 0.82, ClipToBounds = true, Child = inner, Cursor = new Cursor(StandardCursorType.Hand),
            };
            if (art is ImageBrush ib) { ib.AlignmentY = AlignmentY.Center; }
            card.Background = art != null ? new SolidColorBrush(Color.Parse("#152A43")) : Ui.B("#152A43");
            if (art != null) inner.Background = art;
            card.PointerEntered += (_, _) => { Sel = idx; reveal?.Select(Games[idx]); };
            card.PointerPressed += (_, e) => { Sel = idx; reveal?.Select(Games[idx], true); Open(); e.Handled = true; };
            cards.Add(card); row.Children.Add(card);
        }
        ResizeCards();
    }

    void ResizeCards()
    {
        cardW = Math.Clamp(Bounds.Width > 0 ? Bounds.Width * 0.095 : 130, 96, 190);
        foreach (var c in cards) { c.Width = cardW; c.Height = cardW * 1.5; }
        row.Margin = new Thickness(4, 8, 4, 14);
    }

    // title, source and card highlights move at once
    void PaintSelection(Game? g)
    {
        if (g == null)
        {
            title.Text = ""; source.Text = ""; openBtn.IsVisible = false; empty.IsVisible = true;
            return;
        }
        empty.IsVisible = false; openBtn.IsVisible = true;
        title.Text = g.Title;
        title.FontSize = Math.Clamp(Bounds.Width * 0.045, 34, 72); title.LineHeight = title.FontSize * 1.03;
        source.Text = g.AppId != null ? "Steam" : g.Folder != null && g.Folder.Contains("Epic", StringComparison.OrdinalIgnoreCase) ? "Epic Games" : g.Exe != null ? "Added by you" : "";
        var (accent, light, _) = GameRowStyle.Tone(g);
        openBtn.BorderBrush = Ui.B(accent);
        openBtn.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = new GradientStops { new GradientStop(Color.Parse(light), 0), new GradientStop(Color.Parse("#152934"), 1) },
        };
        for (int i = 0; i < cards.Count; i++)
        {
            bool on = i == Sel;
            cards[i].Opacity = on ? 1 : 0.82;
            cards[i].BorderBrush = Ui.B(on ? accent : "#55A1BBDF");
            cards[i].BoxShadow = on ? BoxShadows.Parse("0 0 20 2 " + accent + "AA") : default;
        }
        if (Sel < cards.Count) cards[Sel].BringIntoView();
    }

    // the picture behind everything changes after the pause: new layer fades in over the old one
    void PaintBackdrop(Game g)
    {
        int my = ++request;
        var path = GameRowStyle.WallPath(g);
        var bmp = GameLibrary.LoadBitmap(path);
        if (my != request || reveal?.Selected == null) return;
        int next = 1 - front;
        int fit = S.GameFit.TryGetValue(GameRowStyle.Key(g), out var f) ? f : 0;
        walls[next].Show(bmp, fit, Bounds.Height, true);
        walls[front].Opacity = 0;
        front = next;
        var (_, _, ambient) = GameRowStyle.Tone(g);
        atmosphereHost.Children.Clear();
        atmosphereHost.Children.Add(GameRowStyle.Atmosphere(ambient));
    }

    void Open()
    {
        if (Games.Length == 0) return;
        reveal?.RevealNow();
        LastSel = Sel;
        OpenGame?.Invoke(Games[Sel]);
    }

    void ToggleFavorite()
    {
        if (Games.Length == 0) return;
        var g = Games[Sel]; var k = GameRowStyle.Key(g);
        if (!S.Favorites.Remove(k)) S.Favorites.Add(k);
        S.Save();
        Games = GameLibrary.Games.OrderBy(x => IsFav(S, x) ? 0 : 1).ToArray();
        Sel = Math.Max(0, Array.IndexOf(Games, g));
        BuildCards();
        PaintSelection(Games[Sel]);
    }

    public override void OnKey(Key key)
    {
        if (key == Key.S || key == Key.F2) { OpenSettings?.Invoke(); return; }
        if (Games.Length == 0) return;
        if (key == Key.Right && Sel < Games.Length - 1) { Sel++; reveal?.Select(Games[Sel]); }
        else if (key == Key.Left && Sel > 0) { Sel--; reveal?.Select(Games[Sel]); }
        else if (key == Key.Enter || key == Key.Space) Open();
        else if (key == Key.Y) ToggleFavorite();
    }

    protected override void Refresh() { }
}
