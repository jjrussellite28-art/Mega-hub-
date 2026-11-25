using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace MegaHubPlus;

// Reads installed Steam games straight from Steam's own files. No login, no setup.
public static class GameLibrary
{
    static Game[]? cached;
    public static string Info = "Steam not checked yet.";
    static readonly Dictionary<string, Bitmap?> bitmaps = new();

    public static Game[] Games => cached ??= LoadSteam();

    static readonly string[][] Palette =
    {
        new[] { "#1B2A6B", "#E0703A" }, new[] { "#2A1050", "#E040A0" }, new[] { "#12402E", "#7DB36B" },
        new[] { "#5A0F18", "#E0603A" }, new[] { "#10394F", "#40C0D0" }, new[] { "#3A2A10", "#E0B040" },
    };

    static string? SteamPath()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is string p && Directory.Exists(p)) return p;
        }
        catch { }
        foreach (var guess in new[] { @"C:\Program Files (x86)\Steam", @"C:\Program Files\Steam" })
            if (Directory.Exists(guess)) return guess;
        return null;
    }

    static Game[] LoadSteam()
    {
        try
        {
            var steam = SteamPath();
            if (steam == null) { Info = "Steam was not found on this PC."; return Array.Empty<Game>(); }

            var libs = new List<string> { steam };
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"(.+?)\""))
                {
                    var lib = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(lib) && !libs.Contains(lib, StringComparer.OrdinalIgnoreCase)) libs.Add(lib);
                }

            var list = new List<Game>();
            var seen = new HashSet<string>();
            foreach (var lib in libs)
            {
                var apps = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(apps)) continue;
                foreach (var file in Directory.EnumerateFiles(apps, "appmanifest_*.acf"))
                {
                    var text = File.ReadAllText(file);
                    var flags = Regex.Match(text, "\"StateFlags\"\\s+\"(\\d+)\"").Groups[1].Value;
                    if (!int.TryParse(flags, out var sf) || (sf & 4) == 0) continue; // only fully installed games
                    var id = Regex.Match(text, "\"appid\"\\s+\"(\\d+)\"").Groups[1].Value;
                    var name = Regex.Match(text, "\"name\"\\s+\"(.*?)\"").Groups[1].Value;
                    if (id == "" || name == "" || !seen.Add(id)) continue;
                    if (name.StartsWith("Steamworks", StringComparison.OrdinalIgnoreCase)
                        || name.StartsWith("Proton", StringComparison.OrdinalIgnoreCase)
                        || name.StartsWith("Steam Linux Runtime", StringComparison.OrdinalIgnoreCase)) continue;
                    var pal = Palette[int.Parse(id) % Palette.Length];
                    list.Add(new Game(name, pal[0], pal[1], id,
                        FindArt(steam, id, "library_600x900"), FindArt(steam, id, "library_hero"), FindArt(steam, id, "logo", "png")));
                }
            }
            Info = "Steam found at " + steam + ". Libraries checked: " + libs.Count + ". Installed games found: " + list.Count + ".";
            return list.OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception e) { Info = "Steam reading failed: " + e.Message; return Array.Empty<Game>(); }
    }

    static string? FindArt(string steam, string appId, string kind, string ext = "jpg")
    {
        try
        {
            var root = Path.Combine(steam, "appcache", "librarycache");
            var old = Path.Combine(root, appId + "_" + kind + "." + ext);
            if (File.Exists(old)) return old;
            var dir = Path.Combine(root, appId);
            if (Directory.Exists(dir))
                return Directory.EnumerateFiles(dir, kind + "." + ext, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch { }
        return null;
    }

    public static Bitmap? LoadBitmap(string? path)
    {
        if (path == null) return null;
        if (!bitmaps.TryGetValue(path, out var bmp))
        {
            try { bmp = new Bitmap(path); } catch { bmp = null; }
            bitmaps[path] = bmp;
        }
        return bmp;
    }

    public static IBrush? ArtBrush(string? path, Stretch stretch = Stretch.UniformToFill)
    {
        if (path == null) return null;
        if (!bitmaps.TryGetValue(path, out var bmp))
        {
            try { bmp = new Bitmap(path); } catch { bmp = null; }
            bitmaps[path] = bmp;
        }
        return bmp == null ? null : new ImageBrush(bmp) { Stretch = stretch, AlignmentY = stretch == Stretch.Uniform ? AlignmentY.Top : AlignmentY.Center, AlignmentX = AlignmentX.Center };
    }

    public static void Launch(Game g)
    {
        if (g.AppId == null) return;
        try { Process.Start(new ProcessStartInfo("steam://rungameid/" + g.AppId) { UseShellExecute = true }); }
        catch { }
    }
}
