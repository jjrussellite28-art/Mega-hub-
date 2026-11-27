using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Shared look of the Game Row home and its game page: navy night colours with a blue, mint or coral accent per game.
internal static class GameRowStyle
{
    public static string Key(Game g) => g.AppId ?? g.Title;

    public static (string accent, string light, string ambient) Tone(Game g)
    {
        int h = 0; foreach (var c in g.Title) h = (h * 31 + c) & 0x7fffffff;
        return (h % 3) switch
        {
            1 => ("#84E0C7", "#15443D", "#195E55"),
            2 => ("#FFAD9F", "#592E37", "#8C364D"),
            _ => ("#69B8FF", "#123C59", "#144C73"),
        };
    }

    // landscape picture for the background: SteamGridDB, then Steam's own hero, then IGDB; null = none
    public static string? WallPath(Game g) => Sgdb.ArtPath(g) ?? g.HeroPath ?? Igdb.ArtPath(g);

    static RadialGradientBrush Radial(double x, double y, double r, string color) => new()
    {
        Center = new RelativePoint(x, y, RelativeUnit.Relative),
        GradientOrigin = new RelativePoint(x, y, RelativeUnit.Relative),
        RadiusX = new RelativeScalar(r, RelativeUnit.Relative),
        RadiusY = new RelativeScalar(r, RelativeUnit.Relative),
        GradientStops = new GradientStops { new GradientStop(Color.Parse(color), 0), new GradientStop(Color.Parse("#00000000"), 1) },
    };

    // deep navy with soft coral and mint glows
    public static Control Atmosphere(string? ambient = null)
    {
        var g = new Grid { IsHitTestVisible = false };
        g.Children.Add(new Border { Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops { new GradientStop(Color.Parse("#0F2743"), 0), new GradientStop(Color.Parse("#080D1A"), 0.66), new GradientStop(Color.Parse("#200E19"), 1) },
        } });
        g.Children.Add(new Border { Background = Radial(0.96, 0.18, 0.46, "#532830") });
        g.Children.Add(new Border { Background = Radial(0.62, 0.61, 0.57, ambient ?? "#19504A") });
        return g;
    }

    static LinearGradientBrush Lin(double x1, double y1, double x2, double y2, params (string c, double o)[] stops)
    {
        var b = new LinearGradientBrush { StartPoint = new RelativePoint(x1, y1, RelativeUnit.Relative), EndPoint = new RelativePoint(x2, y2, RelativeUnit.Relative), GradientStops = new GradientStops() };
        foreach (var (c, o) in stops) b.GradientStops.Add(new GradientStop(Color.Parse(c), o));
        return b;
    }

    // darkens the left side (for the title) and the top and bottom edges
    public static Control Scrims(bool strongLeft)
    {
        var g = new Grid { IsHitTestVisible = false };
        g.Children.Add(new Border { Background = Lin(0, 0, 1, 0, ("#07111E", 0.01), (strongLeft ? "#EE07111E" : "#E807111E", strongLeft ? 0.29 : 0.19), (strongLeft ? "#6507111E" : "#3807111E", strongLeft ? 0.59 : 0.68), ("#0007111E", 1)) });
        g.Children.Add(new Border { Background = Lin(0, 1, 0, 0, ("#070E19", 0), ("#AB070E19", 0.24), ("#00070E19", 0.57), ("#C9070E19", 1)) });
        return g;
    }

    public static LinearGradientBrush FadeX() => Lin(0, 0, 1, 0, ("#00000000", 0), ("#FF000000", 0.29));
    public static LinearGradientBrush FadeY(double edge = 0.19) => Lin(0, 1, 0, 0, ("#00000000", 0), ("#FF000000", edge), ("#FF000000", 1 - edge), ("#00000000", 1));

    // one picture layer that can show the whole picture (soft edges) or fill the screen
    public sealed class Wall : Grid
    {
        readonly Border outer = new();
        readonly Border inner = new();
        readonly Image img = new();

        public Wall(double fadeMs = 320)
        {
            IsHitTestVisible = false;
            Opacity = 0;
            Transitions = new Transitions { new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(fadeMs) } };
            RenderOptions.SetBitmapInterpolationMode(img, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
            inner.Child = img;
            outer.Child = inner;
            Children.Add(outer);
        }

        public void Show(Avalonia.Media.Imaging.Bitmap? bmp, int fit, double areaHeight, bool visible)
        {
            img.Source = bmp;
            if (fit == 1)
            {
                Margin = new Thickness(0);
                img.Stretch = Stretch.UniformToFill;
                outer.OpacityMask = null; inner.OpacityMask = null;
                outer.VerticalAlignment = VerticalAlignment.Stretch; outer.MaxHeight = double.PositiveInfinity;
            }
            else
            {
                Margin = new Thickness(0);
                outer.HorizontalAlignment = HorizontalAlignment.Right;
                img.Stretch = Stretch.Uniform;
                outer.VerticalAlignment = VerticalAlignment.Center;
                outer.MaxHeight = Math.Max(areaHeight * 0.76, 100);
                outer.Margin = new Thickness(0);
                outer.OpacityMask = FadeX(); inner.OpacityMask = FadeY(0.23);
            }
            Opacity = visible && bmp != null ? 0.94 : 0;
        }
    }

    public static Border Pill(string text, Control? icon = null)
    {
        return new Border
        {
            Padding = new Thickness(11, 6), CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(1),
            BorderBrush = Ui.B("#448BC4FF"), Background = Ui.B("#CF0B182A"), Child = Ui.Text(text, 14, false, "#D5E6FB"),
        };
    }

    public static Border Mark()
    {
        var t = Ui.Text("M+", 15, true);
        t.HorizontalAlignment = HorizontalAlignment.Center;
        return new Border
        {
            Width = 34, Height = 34, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), BorderBrush = Ui.B("#6EBCFF"),
            Background = Lin(0, 0, 1, 1, ("#267ED7", 0), ("#122B55", 1)), Child = t,
        };
    }

    public static Control Header(bool coupled, Control? right)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(Mark());
        brand.Children.Add(Ui.Text("Mega Hub +", 22, true));
        g.Children.Add(brand);
        var r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
        r.Children.Add(Pill(coupled ? "Couch Mode" : "Desktop Mode"));
        if (right != null) r.Children.Add(right);
        Grid.SetColumn(r, 1);
        g.Children.Add(r);
        return g;
    }
}
