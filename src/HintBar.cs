using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MegaHubPlus;

// Small strip at the bottom-right showing what the buttons do. Shows controller
// buttons when a pad is connected, keyboard keys otherwise.
public class HintBar : Border
{
    readonly StackPanel items = new() { Orientation = Orientation.Horizontal };
    readonly Func<bool> padConnected;
    readonly Func<string> padKind;
    (string pad, string key, string what)[] hints = Array.Empty<(string, string, string)>();
    bool lastPad;
    string lastKind = "Xbox";

    public HintBar(Func<bool> padConnected, Func<string> padKind)
    {
        this.padConnected = padConnected;
        this.padKind = padKind;
        IsHitTestVisible = false;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(0, 0, 24, 14);
        Padding = new Thickness(16, 6);
        CornerRadius = new CornerRadius(18);
        Background = Ui.B("#AA0B0F1A");
        Child = items;
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        t.Tick += (_, _) => { if (padConnected() != lastPad || padKind() != lastKind) Render(); };
        t.Start();
    }

    public void Set(params (string pad, string key, string what)[] h) { hints = h; Render(); }

    // same button, named the way that controller names it
    string Glyph(string pad) => (lastKind, pad) switch
    {
        ("PlayStation", "A") => "✕",
        ("PlayStation", "B") => "○",
        ("PlayStation", "≡") => "Options",
        ("Switch", "≡") => "+",
        _ => pad,
    };

    void Render()
    {
        lastPad = padConnected();
        lastKind = padKind();
        items.Children.Clear();
        foreach (var h in hints)
        {
            var badge = new Border
            {
                Background = Ui.B(lastPad ? "#FFFFFF" : "#44FFFFFF"),
                CornerRadius = new CornerRadius(lastPad && Glyph(h.pad).Length == 1 ? 12 : 6),
                MinWidth = 24, Height = 24, Padding = new Thickness(6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = Ui.Text(lastPad ? Glyph(h.pad) : h.key, 12, true, lastPad ? "#111111" : "#FFFFFF"),
            };
            items.Children.Add(badge);
            var label = Ui.Text(h.what, 13, false, "#DDFFFFFF");
            label.Margin = new Thickness(6, 0, 16, 0);
            label.VerticalAlignment = VerticalAlignment.Center;
            items.Children.Add(label);
        }
    }
}
