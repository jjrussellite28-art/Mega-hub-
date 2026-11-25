using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Startup picker: choose one of the three looks. Switchable later in Settings.
public class PickerView : Grid, IKeyHandler
{
    readonly Action<LookKind> onPick;
    readonly StackPanel cards = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    int sel;

    static readonly (LookKind kind, string name, string desc, string c1, string c2)[] Options =
    {
        (LookKind.ConsoleRow, "Console Row", "Top tabs, big game art, Play button", "#1B2A6B", "#0B0F1A"),
        (LookKind.Blades,     "Blades",      "Side panels, glow, library tiles",    "#E0A030", "#6A1060"),
        (LookKind.WideHome,   "Wide Home",   "Big featured tile + tall tiles",      "#0A1030", "#30B080"),
    };

    public PickerView(Action<LookKind> onPick)
    {
        this.onPick = onPick;
        Background = Ui.Grad("#0B0F1A", "#1A1030");
        var col = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var h = Ui.Text("Welcome to Mega Hub Plus", 34, true);
        h.HorizontalAlignment = HorizontalAlignment.Center;
        col.Children.Add(h);
        var sub = Ui.Text("Pick your look. You can change it any time in Settings.   (Left/Right + Enter)", 15, false, "#CCFFFFFF");
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        sub.Margin = new Thickness(0, 6, 0, 30);
        col.Children.Add(sub);
        col.Children.Add(cards);
        Children.Add(col);
        Refresh();
    }

    void Refresh()
    {
        cards.Children.Clear();
        for (int i = 0; i < Options.Length; i++)
        {
            var o = Options[i];
            int index = i;
            var inner = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(14) };
            inner.Children.Add(Ui.Text(o.name, 22, true));
            var d = Ui.Text(o.desc, 13, false, "#DDFFFFFF");
            d.TextWrapping = TextWrapping.Wrap;
            inner.Children.Add(d);
            var card = new Border
            {
                Width = 300, Height = 200, Margin = new Thickness(12),
                CornerRadius = new CornerRadius(14),
                Background = Ui.Grad(o.c1, o.c2),
                BorderBrush = Ui.B(i == sel ? "#3FE0D0" : "#00000000"),
                BorderThickness = new Thickness(4),
                Child = inner,
            };
            card.PointerPressed += (_, _) => onPick(Options[index].kind);
            cards.Children.Add(card);
        }
    }

    public void OnKey(Key key)
    {
        if (key == Key.Left && sel > 0) { sel--; Refresh(); }
        else if (key == Key.Right && sel < Options.Length - 1) { sel++; Refresh(); }
        else if (key == Key.Enter) onPick(Options[sel].kind);
    }
}
