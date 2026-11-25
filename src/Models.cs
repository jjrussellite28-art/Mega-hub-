using System.Text.Json;

namespace MegaHubPlus;

public enum LookKind { ConsoleRow, Blades, WideHome }
public enum TileShape { Wide, Short, Poster }   // 16:9, 3:4, 2:3

public record Game(string Title, string C1, string C2, string? AppId = null, string? CoverPath = null, string? HeroPath = null);

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

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MegaHubPlus", "settings.json");

    public static AppSettings Load()
    {
        try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch { }
        return new();
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
