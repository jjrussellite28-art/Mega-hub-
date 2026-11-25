using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MegaHubPlus;

// First-time setup: one question per screen, big cards, step dots on top.
public class SetupView : Grid, IKeyHandler
{
    record Card(string Title, string Sub, string[] Bullets, string? Note, bool Current, string Icon, double Ratio = 0);
    record Step(string Title, string Line, string Later, List<Card> Cards, Action<int> Apply, bool Small = false);

    readonly AppSettings s;
    readonly Action applyMode;
    readonly Action done;
    readonly HintBar hints;
    readonly List<Step> steps = new();
    int step, sel;
    bool wantShell;
    const string Icons = "Segoe Fluent Icons, Segoe MDL2 Assets";

    public SetupView(AppSettings s, Action applyMode, Action done, HintBar hints)
    {
        this.s = s; this.applyMode = applyMode; this.done = done; this.hints = hints;
        wantShell = s.ShellMode;
        Background = Ui.Grad("#0A1430", "#0B2F70");

        steps.Add(new("Welcome!", "Your PC as a console. A few questions, then you're ready.", "Settings › Startup & Modes",
            new() { new("English", "Interface in English", Array.Empty<string>(), null, true, "") },
            _ => { }));

        var shapes = new (TileShape v, string name, string sub, double ratio)[]
        {
            (TileShape.Short, "Box cover", "3:4", 3.0 / 4), (TileShape.Poster, "Steam poster", "2:3", 2.0 / 3), (TileShape.Square, "Square", "1:1", 1),
            (TileShape.Retro, "Retro wide", "4:3", 4.0 / 3), (TileShape.Slim, "Slim poster", "9:16", 9.0 / 16), (TileShape.Wide, "Widescreen", "16:9", 16.0 / 9),
            (TileShape.Original, "Original shape", "Each cover as it is", 2.0 / 3),
        };
        steps.Add(new("How should your games look?", "Pick the shape of the game covers.", "Settings › Appearance › Tile shape",
            shapes.Select(x => new Card(x.name, x.sub, Array.Empty<string>(), null, s.Shape == x.v, "")).ToList(),
            i => { s.Shape = shapes[i].v; }, true));

        var walls = new (WallFit v, string name, string sub)[]
        {
            (WallFit.Fill, "Fill the screen", "The whole picture, edge to edge"),
            (WallFit.Crop, "Crop to fit", "Zoomed to fill, edges trimmed"),
            (WallFit.Fit, "Show whole picture", "Nothing cut off, soft edges"),
            (WallFit.Cover, "Cover art (blurred)", "The poster, softly blurred"),
        };
        steps.Add(new("Pick a wallpaper style", "This is the picture behind your games.", "Settings › Appearance › Wallpaper",
            walls.Select(x => new Card(x.name, x.sub, Array.Empty<string>(), null, s.Wall == x.v, "")).ToList(),
            i => { s.Wall = walls[i].v; }));

        steps.Add(new("How should Mega Hub + run?", "You can switch any time.", "Settings › Startup & Modes",
            new()
            {
                new("App", "Alongside Windows", new[] { "Starts like any other app", "Windows stays as it is", "Steam & co. stay untouched" }, null, !s.ShellMode, ""),
                new("Shell", "As a console", new[] { "Replaces the desktop, starts at sign-in", "Back to Windows any time (Ctrl+Alt+Q)" }, "Takes effect the next time you sign in.", s.ShellMode, ""),
            },
            i => { wantShell = i == 1; }));

        step = 0;
        Render();
    }

    void Render()
    {
        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        var st = steps[step];
        if (sel >= st.Cards.Count) sel = 0;

        var col = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(40, 60, 40, 90) };

        // step dots
        var dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 22) };
        for (int i = 0; i < steps.Count; i++)
            dots.Children.Add(new Border
            {
                Width = i == step ? 34 : 10, Height = 10, CornerRadius = new CornerRadius(5), Margin = new Thickness(4, 0),
                Background = Ui.B(i == step ? "#46D2FF" : i < step ? "#2E9CCB" : "#55FFFFFF"),
            });
        col.Children.Add(dots);

        if (step == 0)
        {
            var brand = Ui.Text("Mega Hub +", 44, true);
            brand.HorizontalAlignment = HorizontalAlignment.Center;
            col.Children.Add(brand);
        }
        var title = Ui.Text(st.Title, step == 0 ? 30 : 38, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Margin = new Thickness(0, 6, 0, 4);
        col.Children.Add(title);
        var line = Ui.Text(st.Line, 17, false, "#DDFFFFFF");
        line.HorizontalAlignment = HorizontalAlignment.Center;
        line.Margin = new Thickness(0, 0, 0, 34);
        col.Children.Add(line);

        var row = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 1100 };
        for (int i = 0; i < st.Cards.Count; i++) row.Children.Add(MakeCard(st, i));
        col.Children.Add(row);

        var later = Ui.Text("Change it later: " + st.Later, 14, false, "#CCFFFFFF");
        later.HorizontalAlignment = HorizontalAlignment.Center;
        later.Margin = new Thickness(0, 34, 0, 0);
        col.Children.Add(later);
        Children.Add(col);

        var clock = Ui.Clock(); clock.FontSize = 22;
        clock.HorizontalAlignment = HorizontalAlignment.Right; clock.VerticalAlignment = VerticalAlignment.Top;
        clock.Margin = new Thickness(0, 28, 40, 0);
        Children.Add(clock);

        if (step == 0) hints.Set(("A", "Enter", "Choose"), ("Y", "Y", "Later"), ("≡", "S", "Exit setup"));
        else hints.Set(("A", "Enter", "Choose"), ("B", "Esc", "Back"), ("Y", "Y", "Later"), ("≡", "S", "Exit setup"));
    }

    Control MakeCard(Step st, int i)
    {
        var c = st.Cards[i];
        bool on = i == sel;
        double w = st.Small ? 230 : 340;
        var inner = new StackPanel();
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        top.Children.Add(new TextBlock { Text = c.Icon, FontFamily = new FontFamily(Icons), FontSize = 26, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left });
        if (c.Current)
        {
            var badge = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 2), Child = Ui.Text("Current", 12, true, "#0B2F70") };
            Grid.SetColumn(badge, 1);
            top.Children.Add(badge);
        }
        inner.Children.Add(top);
        if (c.Ratio > 0)
        {
            // a real-proportion preview of the shape (width x height follows the ratio)
            double ph = Math.Min(130, 200 / c.Ratio), pw = ph * c.Ratio;
            inner.Children.Add(new Border
            {
                Width = pw, Height = ph, Margin = new Thickness(0, 12, 0, 4), HorizontalAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White, Background = Ui.B(on ? "#55FFFFFF" : "#33FFFFFF"),
                Child = Ui.Text(c.Sub.Length <= 5 ? c.Sub : "?", 15, true),
                BoxShadow = BoxShadows.Parse("0 0 0 0 #00000000"),
            });
            ((Border)inner.Children[^1]).Child!.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        }
        var name = Ui.Text(c.Title, st.Small ? 22 : 30, true);
        name.Margin = new Thickness(0, 14, 0, 2);
        inner.Children.Add(name);
        inner.Children.Add(Ui.Text(st.Small ? c.Sub : c.Sub.ToUpperInvariant(), 13, true, on ? "#FFFFFF" : "#46D2FF"));
        foreach (var b in c.Bullets)
        {
            var bl = Ui.Text("•  " + b, 15, false, "#EEFFFFFF");
            bl.TextWrapping = TextWrapping.Wrap;
            bl.Margin = new Thickness(0, 8, 0, 0);
            inner.Children.Add(bl);
        }
        if (c.Note != null)
        {
            var n = Ui.Text(c.Note, 13, false, "#CCFFFFFF");
            n.TextWrapping = TextWrapping.Wrap;
            n.Margin = new Thickness(0, 14, 0, 0);
            inner.Children.Add(n);
        }
        var card = new Border
        {
            Width = w, MinHeight = st.Small ? 200 : 230, Margin = new Thickness(10), Padding = new Thickness(22, 18),
            CornerRadius = new CornerRadius(20),
            Background = Ui.B(on ? "#2A9AD0" : "#30294A7A"),
            BorderBrush = Ui.B(on ? "#7FE3FF" : "#554A7FB8"),
            BorderThickness = new Thickness(on ? 3 : 1),
            Child = inner,
        };
        if (on) card.BoxShadow = BoxShadows.Parse("0 0 30 4 #8846D2FF");
        card.PointerPressed += (_, _) => { sel = i; Choose(); };
        return card;
    }

    void Choose()
    {
        steps[step].Apply(sel);
        s.Save();
        if (step < steps.Count - 1) { step++; sel = CurrentIndex(steps[step]); Render(); }
        else Finish();
    }

    static int CurrentIndex(Step st) { var i = st.Cards.FindIndex(c => c.Current); return i < 0 ? 0 : i; }

    void Finish()
    {
        if (wantShell != s.ShellMode)
        {
            if (wantShell) { if (ShellControl.Enable()) s.ShellMode = true; }
            else { ShellControl.Disable(); s.ShellMode = false; }
        }
        s.SetupDone = true;
        s.Save();
        applyMode();
        done();
    }

    void Exit() { s.SetupDone = true; s.Save(); done(); }

    public void OnKey(Key key)
    {
        var st = steps[step];
        if (key == Key.Left && sel > 0) { sel--; Render(); }
        else if (key == Key.Right && sel < st.Cards.Count - 1) { sel++; Render(); }
        else if (key == Key.Up && st.Small && sel >= 4) { sel -= 4; Render(); }
        else if (key == Key.Down && st.Small && sel + 4 < st.Cards.Count) { sel += 4; Render(); }
        else if (key == Key.Enter || key == Key.Space) Choose();
        else if (key == Key.Escape && step > 0) { step--; sel = CurrentIndex(steps[step]); Render(); }
        else if (key == Key.Y)   // Later: skip this question
        {
            if (step < steps.Count - 1) { step++; sel = CurrentIndex(steps[step]); Render(); }
            else Finish();
        }
        else if (key == Key.S) Exit();
    }
}
