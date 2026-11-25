using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// Shown when the app failed to start twice in a row. Plain, big, controller-friendly.
public class SafeView : Grid, IKeyHandler
{
    readonly List<(Border box, Action act)> items = new();
    int focus;

    public SafeView(Action continueNormally, Action toDesktop, Action resetSettings, Action quit)
    {
        Background = Ui.B("#1A0F14");
        var col = new StackPanel { Margin = new Thickness(60, 50), MaxWidth = 700, HorizontalAlignment = HorizontalAlignment.Left };
        col.Children.Add(Ui.Text("Safe start", 34, true));
        var t = Ui.Text("Mega Hub + did not start properly the last two times. Pick what to do. Nothing has been deleted.", 16, false, "#CCFFFFFF");
        t.TextWrapping = TextWrapping.Wrap;
        t.Margin = new Thickness(0, 10, 0, 24);
        col.Children.Add(t);
        void Add(string text, Action a)
        {
            var b = new Border
            {
                CornerRadius = new CornerRadius(12), Padding = new Thickness(22, 14), Margin = new Thickness(0, 6),
                BorderThickness = new Thickness(2), Child = Ui.Text(text, 17, true), HorizontalAlignment = HorizontalAlignment.Left,
            };
            b.PointerPressed += (_, _) => a();
            items.Add((b, a));
            col.Children.Add(b);
        }
        Add("Try again normally", continueNormally);
        Add("Switch to the normal Windows desktop", toDesktop);
        Add("Reset my settings", resetSettings);
        Add("Quit", quit);
        Children.Add(col);
        Paint();
    }

    void Paint()
    {
        for (int i = 0; i < items.Count; i++)
        {
            bool on = i == focus;
            items[i].box.Background = Ui.B(on ? "#553FE0D0" : "#33FFFFFF");
            items[i].box.BorderBrush = Ui.B(on ? "#FFFFFF" : "#33FFFFFF");
        }
    }

    public void OnKey(Key key)
    {
        if (key == Key.Up && focus > 0) focus--;
        else if (key == Key.Down && focus < items.Count - 1) focus++;
        else if (key == Key.Enter || key == Key.Space) { items[focus].act(); return; }
        Paint();
    }
}
