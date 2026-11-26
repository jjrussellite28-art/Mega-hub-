using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace MegaHubPlus;

public class MainWindow : Window
{
    readonly AppSettings st = AppSettings.Load();
    readonly Gamepad pad;
    readonly HintBar hints;
    readonly Grid root = new();

    public MainWindow()
    {
        Title = "Mega Hub +";
        Width = 1280;
        Height = 720;
        Background = Brushes.Black;
        KeyDown += OnKeyDown;
        base.Content = root;
        pad = new Gamepad(k => { if (IsActive) { hints.Flash(k); if (Content is IKeyHandler h) h.OnKey(k); } }, () => Environment.Exit(0));
        hints = new HintBar(() => pad.Connected, () => pad.Kind, k => { hints!.Flash(k); if (Content is IKeyHandler h) h.OnKey(k); }, () => PadIcons.Resolve(st.PadIcons, pad.Connected, pad.Preset));
        Opened += (_, _) =>
        {
            if (st.ShellMode) ApplyMode();
            // stayed up for 8 seconds = a good start
            var ok = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            ok.Tick += (_, _) => { ok.Stop(); Startup.MarkOk(); };
            ok.Start();
            if (Startup.Smoke)
            {
                var done = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                done.Tick += (_, _) => { AppLog.Write("smoke test ok"); Environment.Exit(0); };
                done.Start();
            }
        };
        GameLibrary.Configure(st); Igdb.Init(st);
        _ = Igdb.PrefetchAsync(GameLibrary.Games);
        ApplyMode();
        if (Startup.SafeMode) ShowSafe();
        else if (!st.SetupDone) ShowSetup();
        else ShowLook();
    }

    // current screen sits under the hint bar
    new object? Content
    {
        get => root.Children.Count > 0 ? root.Children[0] : null;
        set
        {
            root.Children.Clear();
            if (value is Control c) { root.Children.Add(c); root.Children.Add(hints); }
        }
    }

    void ShowSafe()
    {
        Content = new SafeView(
            () => { Startup.SafeMode = false; Startup.MarkOk(); ShowLook(); },
            () =>
            {
                ShellControl.Disable();
                if (!ShellControl.ExplorerRunning()) ShellControl.StartExplorer();
                st.ShellMode = false; st.Save(); ApplyMode();
                Startup.SafeMode = false; Startup.MarkOk();
                ShowLook();
            },
            () =>
            {
                st.ShellMode = false; st.Shape = TileShape.Short; st.Save(); ApplyMode();
                ShellControl.Disable();
                Startup.SafeMode = false; Startup.MarkOk();
                ShowLook();
            },
            () => Environment.Exit(0));
        hints.Set(("A", "Enter", "Select"), ("▲▼", "↑↓", "Move"));
    }

    void ShowLook()
    {
        LookBase look = new ConsoleRowLook(st);
        look.OpenSettings = ShowSettings;
        if (look is ConsoleRowLook c0) c0.OpenGame = ShowGame;
        Content = look;
        void BrowseHints() => hints.Set(("A", "Enter", "Open"), ("Y", "Y", "Favorite"), ("LB", "PageUp", "Tab"), ("RB", "PageDown", "Tab"));
        BrowseHints();
        if (look is ConsoleRowLook crl)
            crl.MenuChanged = open => { if (open) hints.Set(("A", "Enter", "Choose"), ("B", "Esc", "Close")); else BrowseHints(); };
    }

    void ShowGame(Game g)
    {
        Content = new GameView(g, ShowLook, hints);
    }

    void ShowSetup()
    {
        Content = new SetupView(st, ApplyMode, ShowLook, hints, () => { Content = new SettingsView(st, ShowLook, ApplyMode, ShowSetup, 4); hints.Set(("A", "Enter", "Select"), ("B", "Esc", "Back")); });
    }

    void ShowSettings()
    {
        Content = new SettingsView(st, ShowLook, ApplyMode, ShowSetup);
        hints.Set(("A", "Enter", "Select"), ("B", "Esc", "Back"));
    }

    // App mode = normal window. Shell mode = full screen, always on top (no Windows changes).
    void ApplyMode()
    {
        Topmost = st.ShellMode;
        if (!st.ShellMode) { WindowState = WindowState.Normal; SystemDecorations = SystemDecorations.Full; return; }
        // shell mode goes on the screen that is NOT the main one (if there is one), so the desktop stays usable
        // real shell (no Explorer running) = main screen; preview before sign-out = the other screen
        var target = ShellControl.ExplorerRunning() ? (Screens.All.FirstOrDefault(sc => !sc.IsPrimary) ?? Screens.Primary) : Screens.Primary;
        if (target == null) { WindowState = WindowState.FullScreen; return; }
        // size the window to exactly that screen ourselves (true pixels / that screen's scaling),
        // so it can't spill past the edges on a screen with different scaling than the main one
        void Fit()
        {
            WindowState = WindowState.Normal;
            SystemDecorations = SystemDecorations.None;
            var k = target.Scaling > 0 ? target.Scaling : 1.0;
            Position = target.Bounds.Position;
            Width = target.Bounds.Width / k;
            Height = target.Bounds.Height / k;
        }
        Fit();
        // fix it again once the window has settled on that screen
        DispatcherTimer.RunOnce(Fit, TimeSpan.FromMilliseconds(400));
        DispatcherTimer.RunOnce(Fit, TimeSpan.FromMilliseconds(1500));
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        hints.Flash(e.Key);
        if (e.Key == Key.Q && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Alt))
            Environment.Exit(0); // emergency exit, always works
        else if (e.Key == Key.F11)
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        else if (Content is IKeyHandler h)
            h.OnKey(e.Key);
    }
}
