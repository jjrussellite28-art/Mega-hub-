using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MegaHubPlus;

// IGDB (the game database Playnite also uses): wide artwork, description, developer, publisher, genres.
// Optional add-on. Each person pastes their own free Twitch Client ID + Secret in Settings. Everything is saved on this PC.
public static class Igdb
{
    public record Meta(string? Summary, string? Dev, string? Pub, string? Date, string? Genres, string? ArtFile);

    static string? token;
    static DateTime tokenUntil;
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(20) };
    static AppSettings? st;

    public static void Init(AppSettings s) => st = s;
    public static bool Configured => st != null && !string.IsNullOrWhiteSpace(st.IgdbId) && !string.IsNullOrWhiteSpace(st.IgdbSecret);

    static string Dir
    {
        get
        {
            var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus", "igdb");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    static string Key(Game g) => g.AppId ?? new string(g.Title.Where(char.IsLetterOrDigit).ToArray());

    public static string? ArtPath(Game g)
    {
        try { var f = Path.Combine(Dir, Key(g) + ".jpg"); return File.Exists(f) ? f : null; } catch { return null; }
    }

    static async Task<string?> Token()
    {
        if (token != null && DateTime.UtcNow < tokenUntil) return token;
        var url = "https://id.twitch.tv/oauth2/token?client_id=" + Uri.EscapeDataString(st!.IgdbId!.Trim())
                + "&client_secret=" + Uri.EscapeDataString(st.IgdbSecret!.Trim()) + "&grant_type=client_credentials";
        using var resp = await http.PostAsync(url, null);
        if (!resp.IsSuccessStatusCode) throw new Exception("IGDB sign-in failed (" + (int)resp.StatusCode + "). Check the Client ID and Secret.");
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        token = doc.RootElement.GetProperty("access_token").GetString();
        tokenUntil = DateTime.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32() - 600);
        return token;
    }

    // Finds the game on IGDB, saves its info and the best wide picture. Cached after the first time.
    public static async Task<Meta?> GetAsync(Game g)
    {
        if (!Configured) return null;
        try
        {
            var metaFile = Path.Combine(Dir, Key(g) + ".json");
            var artFile = Path.Combine(Dir, Key(g) + ".jpg");
            string json;
            if (File.Exists(metaFile)) json = await File.ReadAllTextAsync(metaFile);
            else
            {
                var tk = await Token();
                var name = g.Title.Replace("™", "").Replace("®", "").Replace("\"", "");
                var body = "search \"" + name + "\"; fields name,summary,first_release_date,genres.name,involved_companies.developer,involved_companies.publisher,involved_companies.company.name,artworks.image_id,screenshots.image_id,cover.image_id; limit 1;";
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games") { Content = new StringContent(body, Encoding.UTF8, "text/plain") };
                req.Headers.Add("Client-ID", st!.IgdbId!.Trim());
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tk);
                using var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) throw new Exception("IGDB search failed (" + (int)resp.StatusCode + ")");
                json = await resp.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(metaFile, json);
            }
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.GetArrayLength() == 0) return null;
            var d = doc.RootElement[0];

            if (!File.Exists(artFile))
            {
                string? id = null;
                if (d.TryGetProperty("artworks", out var aw) && aw.GetArrayLength() > 0) id = aw[0].GetProperty("image_id").GetString();
                else if (d.TryGetProperty("screenshots", out var ss) && ss.GetArrayLength() > 0) id = ss[0].GetProperty("image_id").GetString();
                if (id != null)
                {
                    var bytes = await http.GetByteArrayAsync("https://images.igdb.com/igdb/image/upload/t_1080p/" + id + ".jpg");
                    if (bytes.Length > 10000) await File.WriteAllBytesAsync(artFile, bytes);
                }
            }

            string? Companies(string flag)
            {
                if (!d.TryGetProperty("involved_companies", out var ic)) return null;
                var list = ic.EnumerateArray().Where(c => c.TryGetProperty(flag, out var f) && f.ValueKind == JsonValueKind.True)
                             .Select(c => c.TryGetProperty("company", out var co) && co.TryGetProperty("name", out var n) ? n.GetString() : null).Where(x => x != null);
                var s = string.Join(", ", list);
                return s == "" ? null : s;
            }
            string? genres = d.TryGetProperty("genres", out var gn) ? string.Join(", ", gn.EnumerateArray().Select(x => x.GetProperty("name").GetString())) : null;
            string? date = d.TryGetProperty("first_release_date", out var fr) ? DateTimeOffset.FromUnixTimeSeconds(fr.GetInt64()).ToString("MMM d, yyyy") : null;
            return new Meta(d.TryGetProperty("summary", out var sm) ? sm.GetString() : null, Companies("developer"), Companies("publisher"), date, genres,
                File.Exists(artFile) ? artFile : null);
        }
        catch (Exception e) { AppLog.Write("igdb: " + e.Message); return null; }
    }

    // fetch art for every game in the background, one at a time
    public static async Task PrefetchAsync(IEnumerable<Game> games)
    {
        if (!Configured) return;
        foreach (var g in games.ToList())
        {
            if (ArtPath(g) != null) continue;
            await GetAsync(g);
            await Task.Delay(300);
        }
    }

    public static async Task<string> Test()
    {
        if (!Configured) return "Enter the Client ID and Secret first.";
        try { token = null; await Token(); return "Connected to IGDB."; }
        catch (Exception e) { return e.Message; }
    }
}
