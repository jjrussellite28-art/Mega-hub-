namespace MegaHubPlus;

// One log file in %AppData%\MegaHubPlus\logs. Never throws.
public static class AppLog
{
    public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus", "logs");
    public static string FilePath => Path.Combine(Dir, "run.log");

    public static void Write(string m)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var f = FilePath;
            if (System.IO.File.Exists(f) && new FileInfo(f).Length > 1_000_000) System.IO.File.Move(f, f + ".old", true);
            System.IO.File.AppendAllText(f, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + m + "\n");
        }
        catch { }
    }

    public static string Tail(int lines = 200)
    {
        try { return string.Join("\n", System.IO.File.ReadAllLines(FilePath).TakeLast(lines)); }
        catch { return ""; }
    }
}
