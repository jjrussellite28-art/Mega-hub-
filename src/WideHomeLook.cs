using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Look C: dark space background, small colored side tabs, one big wide featured tile + tall tiles, Library strip.
public class WideHomeLook : LookBase
{
    readonly StackPanel recent = new() { Orientation = Orientation.Horizontal };

    public WideHomeLook(AppSettings s) : base(s)
    {
        Background = Ui.Grad("#0A1030", "#3A1A30");
        ColumnDefinitions = new ColumnDefinitions("56,*,56");

        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(Tab("Libraries", "#2A6FE0"));
        left.Children.Add(Tab("What's New", "#7A50E0"));
        SetColumn(left, 0);
        Children.Add(left);

        var right = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(Tab("Store", "#8A40E0"));
        right.Children.Add(Tab("Friends", "#30B080"));
        right.Children.Add(Tab("Settings", "#D0B030"));
        SetColumn(right, 2);
        Children.Add(right);

        var center = new StackPanel { Margin = new Thickness(16) };
        center.Children.Add(Ui.Text("Mega Hub Plus", 20, true));
        var rl = Ui.Text("Recent Games", 22, true);
        rl.Margin = new Thickness(0, 14, 0, 6);
        center.Children.Add(rl);
        center.Children.Add(recent);
        var ll = Ui.Text("Library", 16, true);
        ll.Margin = new Thickness(0, 14, 0, 4);
        center.Children.Add(ll);
        var cats = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var c in Demo.Categories) cats.Children.Add(Ui.Chip(c));
        center.Children.Add(cats);
        SetColumn(center, 1);
        Children.Add(center);

        Refresh();
    }

    static Control Tab(string text, string color)
    {
        var b = new Border
        {
            Background = Ui.B(color), CornerRadius = new CornerRadius(14), Margin = new Thickness(6),
            Child = Ui.SideLabel(text, "#FFFFFF"),
        };
        return b;
    }

    protected override void Refresh()
    {
        recent.Children.Clear();
        if (Games.Length == 0) { recent.Children.Add(Ui.Text("No games found", 20, true)); return; }
        // Featured: always the wide 16:9 tile, then up to three tiles in the chosen shape.
        recent.Children.Add(Ui.Tile(Games[Sel], TileShape.Wide, true, "#3FE0D0"));
        var rest = S.Shape == TileShape.Wide ? TileShape.Short : S.Shape;
        for (int k = 1; k <= 3; k++)
        {
            int i = (Sel + k) % Games.Length;
            recent.Children.Add(Ui.Tile(Games[i], rest, false, "#00000000"));
        }
    }
}
