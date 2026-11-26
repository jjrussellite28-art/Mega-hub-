using System.Text.Json;

namespace MegaHubPlus;

public enum LookKind { ConsoleRow, Blades, WideHome }
public enum WallFit { Fill, Crop, Fit, Cover }   // Cover = the game's cover art, softly blurred, full screen   // wallpaper: stretch to the screen, crop to the screen, or show whole picture
// saved as numbers, so keep this order: Wide 16:9, Short = Box cover 3:4, Poster = Steam poster 2:3, then the rest
public enum TileShape { Wide, Short, Poster, Square, Retro, Slim, Original }

public record Game(string Title, string C1, string C2, string? AppId = null, string? CoverPath = null, string? HeroPath = null, string? LogoPath = null, string? Folder = null, long SizeBytes = 0);

public static class Demo
{
    // Placeholder tiles only. Real games and cover art come later.
    public static readonly Game[] Games =
    {
        new("Game One",   "#1B2A6B", "#E0703A"),
        new("Game Two",   "#2A1050", "#E040A0"),
        new("Game Three", "#12402E", "#7DB36B"),
        new("Game Four",  "#5A0F18", "#E0603A"),
        new("Game Five",  "#10394F", "#40C0D0"),
        new("Game Six",   "#3A2A10", "#E0B040"),
        new("Game Seven", "#2E1A4F", "#7A60E0"),
        new("Game Eight", "#1A1A1A", "#808080"),
    };
    public static readonly string[] Categories =
        { "Favorites", "All Games", "Steam", "Epic", "GOG", "Apps", "Theatre" };
}

public class AppSettings
{
    public LookKind? Look { get; set; }
    public TileShape Shape { get; set; } = TileShape.Short;
    public WallFit Wall { get; set; } = WallFit.Fill;
    // false = App mode (normal window). true = Shell mode (full screen, always on top). Switch any time.
    public bool ShellMode { get; set; }
    public bool SetupDone { get; set; }
    // "auto" = the plugged-in pad (keyboard when none), or one preset name, e.g. "PS5", "GameCube", "Keyboard"
    // false = the original round icons, true = the glossy picture icons for the bell and settings
    public bool PictureIcons { get; set; }
    public string PadIcons { get; set; } = "auto";
    // games the player starred (App ID, or the title when there is none)
    public List<string> Favorites { get; set; } = new();
    public string? IgdbId { get; set; }
    public string? IgdbSecret { get; set; }

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MegaHubPlus", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
            catch (Exception ex)
            {
                // broken settings file: keep a copy, start fresh instead of crashing
                AppLog.Write("settings file was broken, reset: " + ex.Message);
                try { File.Move(FilePath, FilePath + ".bad-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); } catch { }
                return new();
            }
        }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch { }
    }
}
