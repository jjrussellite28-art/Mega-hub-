using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MegaHubPlus;

// Shows every button of the controller and lights it up while it is pressed.
public class PadTestView : Grid, IKeyHandler
{
    record Tile(string Role, string Label, ushort Mask, Border Box, Image Pic, Avalonia.Media.Imaging.Bitmap? Up, Avalonia.Media.Imaging.Bitmap? Down, Border? Bar);

    readonly Action back;
    readonly Func<string> preset;
    readonly List<Tile> tiles = new();
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    Border? lDot, rDot;

    public PadTestView(Action back, Func<string> preset)
    {
        this.back = back; this.preset = preset;
        Background = Ui.Grad("#080C18", "#0A2460");
        var col = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(30) };
        col.Children.Add(Ui.Text("Button test", 34, true));
        var sub = Ui.Text(PadIcons.Pretty(preset()) + ". Press any button on your controller or keyboard; it lights up. Leave with Esc on the keyboard, or hold Back on the controller for 2 seconds.", 16, false, "#CCFFFFFF");
        sub.Margin = new Thickness(0, 4, 0, 24);
        col.Children.Add(sub);

        var wrap = new WrapPanel { MaxWidth = 1000, HorizontalAlignment = HorizontalAlignment.Center };
        void Add(string role, string label, ushort mask, bool trigger = false) { var t = Make(role, label, mask, trigger); tiles.Add(t); wrap.Children.Add(t.Box); }
        Add("LT", "LT", 0, true); Add("LB", "LB", 0x0100); Add("RB", "RB", 0x0200); Add("RT", "RT", 0, true);
        Add("UP", "Up", 0x0001); Add("DOWN", "Down", 0x0002); Add("LEFT", "Left", 0x0004); Add("RIGHT", "Right", 0x0008);
        Add("A", "A", 0x1000); Add("B", "B", 0x2000); Add("X", "X", 0x4000); Add("Y", "Y", 0x8000);
        Add("LS", "Left stick", 0x0040); Add("RS", "Right stick", 0x0080);
        Add("BACK", "Back", 0x0020); Add("≡", "Start", 0x0010);
        col.Children.Add(wrap);
        Children.Add(col);
        timer.Tick += (_, _) => Tick();
        AttachedToVisualTree += (_, _) => timer.Start();
        DetachedFromVisualTree += (_, _) => timer.Stop();
    }

    Tile Make(string role, string label, ushort mask, bool trigger)
    {
        var p = preset();
        var up = PadIcons.Role(p, role, false);
        var down = PadIcons.Role(p, role, true) ?? up;
        var img = new Image { Width = 60, Height = 60, Source = up, HorizontalAlignment = HorizontalAlignment.Center };
        RenderOptions.SetBitmapInterpolationMode(img, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        var st = new StackPanel();
        if (up != null) st.Children.Add(img);
        else
        {
            var badge = new Border { Width = 60, Height = 60, CornerRadius = new CornerRadius(30), Background = Ui.B("#334466"), Child = Ui.Text(label.Length <= 3 ? label : label.Substring(0, 2), 18, true) };
            ((TextBlock)badge.Child!).HorizontalAlignment = HorizontalAlignment.Center;
            st.Children.Add(badge);
        }
        var name = Ui.Text(label, 14, true, "#DDFFFFFF");
        name.HorizontalAlignment = HorizontalAlignment.Center; name.Margin = new Thickness(0, 6, 0, 0);
        st.Children.Add(name);
        Border? bar = null;
        if (trigger)
        {
            bar = new Border { Height = 6, Width = 0, HorizontalAlignment = HorizontalAlignment.Left, Background = Ui.B("#46D2FF"), CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 6, 0, 0) };
            st.Children.Add(bar);
        }
        var box = new Border { Width = 110, Margin = new Thickness(6), Padding = new Thickness(10), CornerRadius = new CornerRadius(14), Background = Ui.B("#22FFFFFF"), BorderBrush = Ui.B("#33FFFFFF"), BorderThickness = new Thickness(2), Child = st };
        return new Tile(role, label, mask, box, img, up, down, bar);
    }

    DateTime? backSince;

    void Tick()
    {
        if ((Gamepad.LiveButtons & 0x0020) != 0 && (Gamepad.LiveButtons & 0x0010) == 0)
        {
            backSince ??= DateTime.UtcNow;
            if (DateTime.UtcNow - backSince.Value > TimeSpan.FromSeconds(2)) { timer.Stop(); back(); return; }
        }
        else backSince = null;
        bool Key(string r) => r switch
        {
            "A" => keys.Contains(Avalonia.Input.Key.Enter),
            "B" => keys.Contains(Avalonia.Input.Key.Escape),
            "Y" => keys.Contains(Avalonia.Input.Key.Y),
            "UP" => keys.Contains(Avalonia.Input.Key.Up), "DOWN" => keys.Contains(Avalonia.Input.Key.Down),
            "LEFT" => keys.Contains(Avalonia.Input.Key.Left), "RIGHT" => keys.Contains(Avalonia.Input.Key.Right),
            "≡" => keys.Contains(Avalonia.Input.Key.S),
            "LB" => keys.Contains(Avalonia.Input.Key.PageUp), "RB" => keys.Contains(Avalonia.Input.Key.PageDown),
            _ => false,
        };
        foreach (var t in tiles)
        {
            bool on = t.Mask != 0 ? (Gamepad.LiveButtons & t.Mask) != 0 : false;
            if (t.Role == "LT" && Gamepad.LiveLT > 40) on = true;
            if (t.Role == "RT" && Gamepad.LiveRT > 40) on = true;
            if (Key(t.Role)) on = true;
            if (t.Bar != null) t.Bar.Width = 90.0 * (t.Role == "LT" ? Gamepad.LiveLT : Gamepad.LiveRT) / 255;
            t.Box.Background = Ui.B(on ? "#552A9AD0" : "#22FFFFFF");
            t.Box.BorderBrush = Ui.B(on ? "#7FE3FF" : "#33FFFFFF");
            t.Box.BoxShadow = on ? BoxShadows.Parse("0 0 22 2 #8846D2FF") : default;
            if (t.Pic.Source != null) t.Pic.Source = on ? t.Down : t.Up;
        }
        keys.Clear();
    }

    readonly HashSet<Key> keys = new();

    public void OnKey(Key key)
    {
        // Escape from the keyboard leaves; Escape made by the pad's B button just lights B up
        if (key == Key.Escape && (Gamepad.LiveButtons & 0x2000) == 0) { back(); return; }
        keys.Add(key);
    }
}
