using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Look A: PS5-style top-tab row, big background, Play button, last-played panel.
public class ConsoleRowLook : LookBase
{
    readonly StackPanel row = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(30, 10, 0, 0) };
    readonly TextBlock title = Ui.Text("", 42, true);

    public ConsoleRowLook(AppSettings s) : base(s)
    {
        RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto");

        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 0, 0, 0) };
        tabs.Children.Add(Ui.Pill("Games", "#33FFFFFF", "#FFFFFF", 14));
        foreach (var t in new[] { "Media", "Store", "Friends" })
            tabs.Children.Add(Ui.Pill(t, "#00000000", "#CCFFFFFF", 14));
        bar.Children.Add(Ui.Text("Mega Hub Plus", 18, true));
        Grid.SetColumn(tabs, 1);
        bar.Children.Add(tabs);
        var clock = Ui.Clock();
        Grid.SetColumn(clock, 2);
        bar.Children.Add(clock);
        var barBox = new Border
        {
            Margin = new Thickness(20), Padding = new Thickness(22, 6),
            CornerRadius = new CornerRadius(26), Background = Ui.B("#66000000"), Child = bar,
        };
        SetRow(barBox, 0);
        Children.Add(barBox);

        SetRow(row, 1);
        Children.Add(row);

        var bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(40, 0, 40, 36) };
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
        left.Children.Add(title);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var play = Ui.Pill("▶  Play", "#F5E9D6", "#1A1A1A", 18);
        play.PointerPressed += (_, _) => { if (Games.Length > 0) GameLibrary.Launch(Games[Sel]); };
        buttons.Children.Add(play);
        buttons.Children.Add(Ui.Pill("•••", "#44FFFFFF", "#FFFFFF", 18));
        left.Children.Add(buttons);
        bottom.Children.Add(left);
        SetRow(bottom, 3);
        Children.Add(bottom);

        Refresh();
    }

    protected override void Refresh()
    {
        row.Children.Clear();
        if (Games.Length == 0)
        {
            Background = Ui.Grad("#1B2A6B", "#0B0F1A", false);
            title.Text = "No games found";
            return;
        }
        var g = Games[Sel];
        Background = Ui.Grad(g.C1, "#0B0F1A", false);
        title.Text = g.Title;
        int start = Math.Max(0, Math.Min(Sel - 2, Games.Length - 6));
        for (int i = start; i < Math.Min(Games.Length, start + 6); i++)
            row.Children.Add(Ui.Tile(Games[i], S.Shape, i == Sel, "#3FE0D0"));
    }
}
