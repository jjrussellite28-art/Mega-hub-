using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace MegaHubPlus;

public class MainWindow : Window
{
    readonly AppSettings st = AppSettings.Load();
    readonly Gamepad pad;

    public MainWindow()
    {
        Title = "Mega Hub +";
        Width = 1280;
        Height = 720;
        Background = Brushes.Black;
        KeyDown += OnKeyDown;
        pad = new Gamepad(k => { if (IsActive && Content is IKeyHandler h) h.OnKey(k); }, () => Environment.Exit(0));
        ApplyMode();
        if (st.Look == null) ShowPicker(); else ShowLook();
    }

    void ShowPicker() => Content = new PickerView(kind => { st.Look = kind; st.Save(); ShowLook(); });

    void ShowLook()
    {
        LookBase look = st.Look switch
        {
            LookKind.Blades => new BladesLook(st),
            LookKind.WideHome => new WideHomeLook(st),
            _ => new ConsoleRowLook(st),
        };
        look.OpenSettings = ShowSettings;
        Content = look;
    }

    void ShowSettings() => Content = new SettingsView(st, ShowLook, ApplyMode);

    // App mode = normal window. Shell mode = full screen, always on top (no Windows changes).
    void ApplyMode()
    {
        Topmost = st.ShellMode;
        WindowState = st.ShellMode ? WindowState.FullScreen : WindowState.Normal;
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
