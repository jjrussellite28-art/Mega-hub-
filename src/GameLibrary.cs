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

    static AppSettings? settings;
    public static void Configure(AppSettings s) => settings = s;
    public static void Reload() => cached = null;

    public static Game[] Games => cached ??= (settings == null || settings.UseSteam ? LoadSteam() : Array.Empty<Game>()).Concat(settings == null || settings.UseEpic ? LoadEpic() : Array.Empty<Game>()).Concat(settings == null || settings.UseGog ? LoadGog() : Array.Empty<Game>()).Concat(LoadManual()).OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase).ToArray();

    // Epic Games Launcher keeps one small file per installed game
    static IEnumerable<Game> LoadEpic()
    {
        var list = new List<Game>();
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
            if (!Directory.Exists(dir)) return list;
            int n = 0;
            foreach (var f in Directory.EnumerateFiles(dir, "*.item"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(f));
                    var r = doc.RootElement;
                    string? Str(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
                    var name = Str("DisplayName"); var loc = Str("InstallLocation"); var launch = Str("LaunchExecutable");
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(loc) || string.IsNullOrEmpty(launch)) continue;
                    var exe = Path.Combine(loc, launch);
                    if (!File.Exists(exe)) continue;
                    var pal = Palette[(n++) % Palette.Length];
                    list.Add(new Game(name, pal[0], pal[1], null, null, null, null, loc, 0, exe));
                }
                catch { }
            }
        }
        catch (Exception e) { AppLog.Write("epic: " + e.Message); }
        return list;
    }

    // GOG Galaxy / GOG installers register every game in the registry
    static IEnumerable<Game> LoadGog()
    {
        var list = new List<Game>();
        try
        {
            using var games = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
            if (games == null) return list;
            int n = 0;
            foreach (var id in games.GetSubKeyNames())
            {
                using var k = games.OpenSubKey(id);
                var name = k?.GetValue("gameName") as string; var exe = k?.GetValue("exe") as string; var path = k?.GetValue("path") as string;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(exe) || !File.Exists(exe)) continue;
                var pal = Palette[(n++) % Palette.Length];
                list.Add(new Game(name, pal[0], pal[1], null, null, null, null, path, 0, exe));
            }
        }
        catch (Exception e) { AppLog.Write("gog: " + e.Message); }
        return list;
    }

    static readonly string[] SkipExe = { "unins", "crash", "setup", "redist", "vc_", "dxsetup", "dotnet", "installer", "launcher_helper", "uninstall", "updater" };

    // games the player pointed us to: every sub-folder with a program in it is one game (or the folder itself)
    static IEnumerable<Game> LoadManual()
    {
        var list = new List<Game>();
        if (settings == null) return list;
        int n = 0;
        foreach (var root in settings.GameFolders.ToList())
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                var dirs = Directory.EnumerateDirectories(root).ToList();
                var candidates = dirs.Count > 0 ? dirs : new List<string> { root };
                if (dirs.Count > 0 && BestExe(root, null) != null) candidates.Add(root);
                foreach (var d in candidates)
                {
                    var exe = BestExe(d, Path.GetFileName(d));
                    if (exe == null) continue;
                    var title = Path.GetFileName(d.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (string.IsNullOrEmpty(title)) title = Path.GetFileNameWithoutExtension(exe);
                    var pal = Palette[(n++) % Palette.Length];
                    long size = 0; try { size = new FileInfo(exe).Length; } catch { }
                    list.Add(new Game(title, pal[0], pal[1], null, null, null, null, d, size, exe));
                }
            }
            catch (Exception e) { AppLog.Write("manual games: " + e.Message); }
        }
        return list;
    }

    // the program that starts a game: one named like the folder, else the biggest one that isn't an installer or helper
    static string? BestExe(string dir, string? name)
    {
        try
        {
            var exes = Directory.EnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly)
                .Where(f => !SkipExe.Any(s => Path.GetFileName(f).Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
            if (exes.Count == 0)
            {
                // some games keep it one level down (bin, game, x64)
                foreach (var sub in Directory.EnumerateDirectories(dir).Take(8))
                    exes.AddRange(Directory.EnumerateFiles(sub, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(f => !SkipExe.Any(s => Path.GetFileName(f).Contains(s, StringComparison.OrdinalIgnoreCase))));
            }
            if (exes.Count == 0) return null;
            if (name != null)
            {
                var same = exes.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Replace(" ", "").Equals(name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (same != null) return same;
            }
            return exes.OrderByDescending(f => new FileInfo(f).Length).First();
        }
        catch { return null; }
    }

    static readonly string[][] Palette =
    {
        new[] { "#1B2A6B", "#E0703A" }, new[] { "#2A1050", "#E040A0" }, new[] { "#12402E", "#7DB36B" },
        new[] { "#5A0F18", "#E0603A" }, new[] { "#10394F", "#40C0D0" }, new[] { "#3A2A10", "#E0B040" },
    };

    public static string? SteamPath()
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
                    var instDir = Regex.Match(text, "\"installdir\"\\s+\"(.*?)\"").Groups[1].Value;
                    long.TryParse(Regex.Match(text, "\"SizeOnDisk\"\\s+\"(\\d+)\"").Groups[1].Value, out var sizeOnDisk);
                    var folder = instDir == "" ? null : Path.Combine(apps, "common", instDir);
                    var pal = Palette[int.Parse(id) % Palette.Length];
                    list.Add(new Game(name, pal[0], pal[1], id,
                        FindArt(steam, id, "library_600x900"), FindArt(steam, id, "library_hero"), FindArt(steam, id, "logo", "png"), folder, sizeOnDisk));
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

    public static bool Launch(Game g)
    {
        try
        {
            if (g.Exe != null) Process.Start(new ProcessStartInfo(g.Exe) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(g.Exe) ?? "" });
            else if (g.AppId != null) Process.Start(new ProcessStartInfo("steam://rungameid/" + g.AppId) { UseShellExecute = true });
            else return false;
            return true;
        }
        catch (Exception e) { AppLog.Write("launch failed: " + e.Message); return false; }
    }
}
