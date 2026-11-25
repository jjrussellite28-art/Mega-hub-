using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Look B: Xbox 360 "blades" style. Side panels, Recent Games, Library category tiles, button hints.
public class BladesLook : LookBase
{
    readonly StackPanel recent = new() { Orientation = Orientation.Horizontal };

    public BladesLook(AppSettings s) : base(s)
    {
        Background = Ui.Grad("#3A1E08", "#4A0F3A");
        ColumnDefinitions = new ColumnDefinitions("80,*,80");

        var left = new Border { Background = Ui.Grad("#E0A030", "#8A4A10", false), CornerRadius = new CornerRadius(0, 40, 40, 0), Margin = new Thickness(0, 30) };
        var leftStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        leftStack.Children.Add(Ui.SideLabel("What's New", "#FFFFFF"));
        leftStack.Children.Add(Ui.SideLabel("Libraries", "#FFF3C0"));
        left.Child = leftStack;
        SetColumn(left, 0);
        Children.Add(left);

        var right = new Border { Background = Ui.Grad("#E040A0", "#6A1060", false), CornerRadius = new CornerRadius(40, 0, 0, 40), Margin = new Thickness(0, 30) };
        var rightStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var t in new[] { "Store", "Friends", "Settings" })
            rightStack.Children.Add(Ui.SideLabel(t, "#FFFFFF"));
        right.Child = rightStack;
        SetColumn(right, 2);
        Children.Add(right);

        var center = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(24, 16) };
        var top = new StackPanel();

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        header.Children.Add(Ui.Text("Mega Hub +", 18, true));
        var search = new Border
        {
            Background = Ui.B("#33000000"), CornerRadius = new CornerRadius(16), Padding = new Thickness(14, 4),
            HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 16, 0),
            Child = Ui.Text("Search games", 12, false, "#CCFFFFFF"),
        };
        SetColumn(search, 1);
        header.Children.Add(search);
        var clock = Ui.Clock();
        SetColumn(clock, 2);
        header.Children.Add(clock);
        top.Children.Add(header);

        top.Children.Add(Ui.Text("Libraries", 28, true));
        var recentLabel = Ui.Text("Recent Games", 15, true);
        recentLabel.Margin = new Thickness(0, 8, 0, 0);
        top.Children.Add(recentLabel);
        top.Children.Add(recent);
        var libLabel = Ui.Text("Library", 15, true);
        libLabel.Margin = new Thickness(0, 12, 0, 4);
        top.Children.Add(libLabel);
        var cats = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var c in Demo.Categories) cats.Children.Add(Ui.Chip(c));
        top.Children.Add(cats);
        center.Children.Add(top);

        var hints = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var h in new[] { "(Y) Search", "(B) Close", "(A) Select" })
        {
            var t = Ui.Text(h, 13);
            t.Margin = new Thickness(18, 0);
            hints.Children.Add(t);
        }
        SetRow(hints, 1);
        center.Children.Add(hints);
        SetColumn(center, 1);
        Children.Add(center);

        Refresh();
    }

    protected override void Refresh()
    {
        recent.Children.Clear();
        int start = Math.Max(0, Math.Min(Sel - 1, Games.Length - 4));
        for (int i = start; i < Math.Min(Games.Length, start + 4); i++)
            recent.Children.Add(Ui.Tile(Games[i], S.Shape, i == Sel, "#F5C542"));
    }
}
