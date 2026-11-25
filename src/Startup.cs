namespace MegaHubPlus;

// Detects "the app keeps failing to start". A flag file is written at start and removed once the app has
// stayed up for a few seconds (or exits normally). Two failed starts in a row = Safe start screen.
public static class Startup
{
    public static bool SafeMode;
    public static bool Smoke;      // --smoke-test: open, wait, exit 0 (used by the GitHub build)
    public static bool Crashed;

    static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus");
    static string Flag => Path.Combine(Dir, "starting.flag");
    static string CountFile => Path.Combine(Dir, "badstarts.txt");

    static int ReadCount()
    {
        try { return int.TryParse(File.ReadAllText(CountFile).Trim(), out var n) ? n : 0; } catch { return 0; }
    }

    public static void Begin()
    {
        if (Smoke) return;
        try
        {
            Directory.CreateDirectory(Dir);
            int n = ReadCount();
            if (File.Exists(Flag)) { n++; File.WriteAllText(CountFile, n.ToString()); }
            SafeMode = n >= 2;
            File.WriteAllText(Flag, DateTime.Now.ToString("O"));
        }
        catch { }
    }

    public static void MarkOk()
    {
        if (Smoke) return;
        try { if (File.Exists(Flag)) File.Delete(Flag); File.WriteAllText(CountFile, "0"); } catch { }
    }
}
