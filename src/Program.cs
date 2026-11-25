using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace MegaHubPlus;

class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        Startup.Smoke = args.Contains("--smoke-test");
        Startup.Begin();
        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnLastWindowClose,
        };
        AppLog.Write("start" + (Startup.SafeMode ? " (safe start)" : "") + (Startup.Smoke ? " (smoke test)" : ""));
        AppDomain.CurrentDomain.UnhandledException += (_, e) => { Startup.Crashed = true; AppLog.Write("unhandled: " + e.ExceptionObject); ShellControl.RestoreDesktopIfNeeded(); };
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            AppLog.Write("exit");
            if (!Startup.Crashed) Startup.MarkOk();   // a normal quit is not a failed start
            ShellControl.RestoreDesktopIfNeeded();
        };
        try
        {
            BuildAvaloniaApp().SetupWithLifetime(lifetime);
            return lifetime.Start(args);
        }
        catch (Exception e)
        {
            Startup.Crashed = true;
            AppLog.Write("crash: " + e);
            ShellControl.RestoreDesktopIfNeeded();
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
