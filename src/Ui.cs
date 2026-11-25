using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MegaHubPlus;

public interface IKeyHandler { void OnKey(Key key); }

public static class Ui
{
    public static SolidColorBrush B(string hex) => new(Color.Parse(hex));

    public static TextBlock Text(string t, double size = 14, bool bold = false, string color = "#FFFFFF") => new()
    {
        Text = t,
        FontSize = size,
        FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
        Foreground = B(color),
        VerticalAlignment = VerticalAlignment.Center,
    };

    public static IBrush Grad(string a, string b, bool diagonal = true) => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = diagonal ? new RelativePoint(1, 1, RelativeUnit.Relative) : new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops { new GradientStop(Color.Parse(a), 0), new GradientStop(Color.Parse(b), 1) },
    };

    public static (double w, double h) Size(TileShape s) => s switch
    {
        TileShape.Wide => (300, 169),    // 16:9
        TileShape.Short => (170, 227),   // 3:4
        _ => (150, 225),                 // 2:3 poster
    };

    public static Border Tile(Game g, TileShape shape, bool selected, string glow)
    {
        var (w, h) = Size(shape);
        if (selected) { w *= 1.1; h *= 1.1; }
        var art = GameLibrary.ArtBrush(shape == TileShape.Wide ? (g.HeroPath ?? g.CoverPath) : g.CoverPath);
        var tile = new Border
        {
            Width = w, Height = h,
            CornerRadius = new CornerRadius(12),
            Background = art ?? Grad(g.C1, g.C2),
            BorderBrush = B(selected ? glow : "#00000000"),
            BorderThickness = new Thickness(3),
            Margin = new Thickness(6),
            VerticalAlignment = VerticalAlignment.Top,
        };
        if (selected) tile.BoxShadow = BoxShadows.Parse("0 0 28 4 " + glow);
        if (art == null)
        {
            var label = Text(g.Title, 14, true);
            label.TextWrapping = TextWrapping.Wrap;
            label.TextAlignment = TextAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Bottom;
            label.Margin = new Thickness(8);
            tile.Child = label;
        }
        return tile;
    }

    public static Border Pill(string text, string bg, string fg, double size = 16) => new()
    {
        Background = B(bg),
        CornerRadius = new CornerRadius(22),
        Padding = new Thickness(26, 10),
        Margin = new Thickness(4),
        Child = Text(text, size, true, fg),
    };

    public static Border Chip(string text, bool selected = false) => new()
    {
        Background = B(selected ? "#66FFFFFF" : "#33000000"),
        BorderBrush = B("#44FFFFFF"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(16, 12),
        Margin = new Thickness(4),
        MinWidth = 96,
        Child = Text(text, 13, true),
    };

    public static TextBlock Clock()
    {
        var tb = Text(DateTime.Now.ToString("h:mm"), 15, true);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => { var t = DateTime.Now.ToString("h:mm"); if (tb.Text != t) tb.Text = t; };
        timer.Start();
        return tb;
    }

    public static Control SideLabel(string text, string color)
    {
        var rotated = new LayoutTransformControl
        {
            LayoutTransform = new RotateTransform(-90),
            Child = Text(text, 15, true, color),
        };
        return new Border { Padding = new Thickness(0, 22), Child = rotated, HorizontalAlignment = HorizontalAlignment.Center };
    }
}

public abstract class LookBase : Grid, IKeyHandler
{
    protected readonly AppSettings S;
    protected int Sel;
    protected readonly Game[] Games = GameLibrary.Games;
    public Action? OpenSettings;

    protected LookBase(AppSettings s) { S = s; }

    public virtual void OnKey(Key key)
    {
        if (key == Key.Right && Sel < Games.Length - 1) { Sel++; Refresh(); }
        else if (key == Key.Left && Sel > 0) { Sel--; Refresh(); }
        else if (key == Key.Enter && Games.Length > 0) GameLibrary.Launch(Games[Sel]);
        else if (key == Key.S || key == Key.F2) OpenSettings?.Invoke();
    }

    protected abstract void Refresh();
}
