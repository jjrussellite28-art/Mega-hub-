using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace MegaHubPlus;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnLastWindowClose,
        };
        try
        {
            BuildAvaloniaApp().SetupWithLifetime(lifetime);
            lifetime.Start(args);
        }
        catch (Exception e)
        {
            try { File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "..", "crash.log"), e.ToString()); } catch { }
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
