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

    readonly Action<Avalonia.Input.Key>? press;

    readonly Func<string>? preset;
    string lastPreset = "";
    string? pressedRole;

    public HintBar(Func<bool> padConnected, Func<string> padKind, Action<Avalonia.Input.Key>? press = null, Func<string>? preset = null)
    {
        this.press = press;
        this.preset = preset;
        this.padConnected = padConnected;
        this.padKind = padKind;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(0, 0, 24, 14);
        Padding = new Thickness(16, 6);
        CornerRadius = new CornerRadius(18);
        Background = Ui.B("#AA0B0F1A");
        Child = items;
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        t.Tick += (_, _) => { if (padConnected() != lastPad || padKind() != lastKind || (preset != null && preset() != lastPreset)) Render(); };
        t.Start();
    }

    // lights up the button picture for a moment when it is pressed (keyboard or controller)
    public void Flash(Avalonia.Input.Key k)
    {
        var role = k == Avalonia.Input.Key.Enter ? "A" : k == Avalonia.Input.Key.Escape ? "B" : k == Avalonia.Input.Key.Y ? "Y" : k == Avalonia.Input.Key.S ? "≡" : null;
        if (role == null) return;
        pressedRole = role;
        Render();
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
        t.Tick += (_, _) => { t.Stop(); if (pressedRole == role) { pressedRole = null; Render(); } };
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
        lastPreset = preset?.Invoke() ?? "";
        items.Children.Clear();
        foreach (var h in hints)
        {
            // the real button picture when the pack has one for this controller
            var pic = lastPreset == "" ? null : PadIcons.Role(lastPreset, h.pad, pressedRole == h.pad);
            // otherwise a drawn badge (Xbox style by default): A = select, B = back
            var g = Glyph(h.pad);
            var xbox = lastKind == "Xbox" || !lastPad;
            var badge = new Border
            {
                Background = Ui.B(xbox && h.pad == "A" ? "#2FB457" : xbox && h.pad == "B" ? "#D9453F" : xbox && h.pad == "Y" ? "#E6C229" : "#FFFFFF"),
                CornerRadius = new CornerRadius(g.Length == 1 ? 14 : 8),
                MinWidth = 28, Height = 28, Padding = new Thickness(7, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = Ui.Text(g, 13, true, xbox && (h.pad == "A" || h.pad == "B") ? "#FFFFFF" : "#111111"),
            };
            var label = Ui.Text(h.what, 22, true, "#FFFFFFFF");
            label.Margin = new Thickness(8, 0, 0, 0);
            Control badgeOrPic = badge;
            if (pic != null)
            {
                var im = new Image { Source = pic, Width = 64, Height = 64, VerticalAlignment = VerticalAlignment.Center };
                RenderOptions.SetBitmapInterpolationMode(im, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
                badgeOrPic = im;
            }
            var item = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 18, 0), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            item.Children.Add(badgeOrPic);
            item.Children.Add(label);
            if (h.pad == "A" || h.pad == "B" || h.pad == "Y" || h.pad == "≡")
            {
                var key = h.pad == "A" ? Avalonia.Input.Key.Enter : h.pad == "B" ? Avalonia.Input.Key.Escape : h.pad == "Y" ? Avalonia.Input.Key.Y : Avalonia.Input.Key.S;
                item.PointerPressed += (_, _) => press?.Invoke(key);
            }
            items.Children.Add(item);
        }
    }
}
