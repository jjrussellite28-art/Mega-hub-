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
        string log = Path.Combine(Directory.GetCurrentDirectory(), "..", "run.log");
        void Log(string m) { try { File.AppendAllText(log, DateTime.Now.ToString("HH:mm:ss ") + m + "\n"); } catch { } }
        Log("start");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("unhandled: " + e.ExceptionObject);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Log("exit\n" + Environment.StackTrace); ShellControl.RestoreDesktopIfNeeded(); };
        try
        {
            BuildAvaloniaApp().SetupWithLifetime(lifetime);
            lifetime.Start(args);
        }
        catch (Exception e)
        {
            try { File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "..", "crash.log"), e.ToString()); } catch { }
            ShellControl.RestoreDesktopIfNeeded();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
