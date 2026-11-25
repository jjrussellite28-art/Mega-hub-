using System.Diagnostics;
using Microsoft.Win32;

namespace MegaHubPlus;

// Real shell replacement, per Windows user only (HKCU). Reversible any time.
// Shell mode  = Windows starts Mega Hub + instead of the desktop at the next sign-in.
// Desktop mode = value removed, normal Windows desktop (Explorer) as usual.
public static class ShellControl
{
    const string Key = @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon";
    public static bool SkipRestore;   // set while updating so Explorer isn't started in between

    static string? Exe => Environment.ProcessPath;

    public static bool IsRegistered
    {
        get
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(Key);
                var v = k?.GetValue("Shell") as string;
                return v != null && Exe != null && v.Trim('"').Equals(Exe, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }

    public static bool ExplorerRunning() { try { return Process.GetProcessesByName("explorer").Length > 0; } catch { return true; } }

    public static bool Enable()
    {
        try
        {
            if (Exe == null || !Exe.EndsWith("MegaHubPlus.exe", StringComparison.OrdinalIgnoreCase)) return false; // never for dev builds
            using var k = Registry.CurrentUser.CreateSubKey(Key);
            k.SetValue("Shell", "\"" + Exe + "\"", RegistryValueKind.String);
            return true;
        }
        catch { return false; }
    }

    // Only removes the value if it is ours.
    public static void Disable()
    {
        try
        {
            if (!IsRegistered) return;
            using var k = Registry.CurrentUser.OpenSubKey(Key, true);
            k?.DeleteValue("Shell", false);
        }
        catch { }
    }

    public static void StartExplorer()
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true }); } catch { }
    }

    // Safety net on any exit: if we are the shell and there is no desktop, bring the desktop back.
    public static void RestoreDesktopIfNeeded()
    {
        if (SkipRestore) return;
        if (IsRegistered && !ExplorerRunning()) StartExplorer();
    }

    public static void SignOut()
    {
        try { Process.Start(new ProcessStartInfo("shutdown.exe", "/l") { UseShellExecute = false, CreateNoWindow = true }); } catch { }
    }
}
