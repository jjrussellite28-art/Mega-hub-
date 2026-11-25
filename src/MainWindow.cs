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
        pad = new Gamepad(k => { if (IsActive && Content is IKeyHandler h) h.OnKey(k); }, () => Environment.Exit(0));
        hints = new HintBar(() => pad.Connected, () => pad.Kind);
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
        ApplyMode();
        if (Startup.SafeMode) ShowSafe();
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
        Content = look;
        void BrowseHints() => hints.Set(("A", "Enter", "Open"), ("◀▶▲▼", "Arrows", "Browse"), ("≡", "S", "Settings"));
        BrowseHints();
        if (look is ConsoleRowLook crl)
            crl.MenuChanged = open => { if (open) hints.Set(("A", "Enter", "Choose"), ("B", "Esc", "Close")); else BrowseHints(); };
    }

    void ShowSettings()
    {
        Content = new SettingsView(st, ShowLook, ApplyMode);
        hints.Set(("A", "Enter", "Select"), ("B", "Esc", "Back"));
    }

    // App mode = normal window. Shell mode = full screen, always on top (no Windows changes).
    void ApplyMode()
    {
        Topmost = st.ShellMode;
        if (!st.ShellMode) { WindowState = WindowState.Normal; return; }
        // shell mode goes on the screen that is NOT the main one (if there is one), so the desktop stays usable
        // real shell (no Explorer running) = main screen; preview before sign-out = the other screen
        var target = ShellControl.ExplorerRunning() ? (Screens.All.FirstOrDefault(sc => !sc.IsPrimary) ?? Screens.Primary) : Screens.Primary;
        if (target != null)
        {
            WindowState = WindowState.Normal;
            Position = new Avalonia.PixelPoint(target.Bounds.X + 50, target.Bounds.Y + 50);
        }
        WindowState = WindowState.FullScreen;
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Q && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Alt))
            Environment.Exit(0); // emergency exit, always works
        else if (e.Key == Key.F11)
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        else if (Content is IKeyHandler h)
            h.OnKey(e.Key);
    }
}
