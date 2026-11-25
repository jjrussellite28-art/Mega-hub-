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
        Title = "Mega Hub Plus";
        Width = 1280;
        Height = 720;
        Background = Brushes.Black;
        KeyDown += OnKeyDown;
        pad = new Gamepad(k => { if (IsActive && Content is IKeyHandler h) h.OnKey(k); });
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

    void ShowSettings() => Content = new SettingsView(st, ShowLook);

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        else if (Content is IKeyHandler h)
            h.OnKey(e.Key);
    }
}
