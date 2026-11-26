using System.Net.Http.Headers;
using System.Text.Json;

namespace MegaHubPlus;

// SteamGridDB (steamgriddb.com): wide hero pictures made by fans. Optional: each person pastes their own free key in Settings.
// Pictures are saved on this PC after the first download.
public static class Sgdb
{
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(20) };
    static AppSettings? st;
    public static void Init(AppSettings s) => st = s;
    public static bool Configured => st != null && !string.IsNullOrWhiteSpace(st.SgdbKey);

    static string Dir
    {
        get
        {
            var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus", "sgdb");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    static string Key(Game g) => g.AppId ?? new string(g.Title.Where(char.IsLetterOrDigit).ToArray());

    public static string? ArtPath(Game g)
    {
        try { var f = Path.Combine(Dir, Key(g) + ".jpg"); return File.Exists(f) ? f : null; } catch { return null; }
    }

    static async Task<JsonDocument> Get(string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.steamgriddb.com/api/v2" + url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", st!.SgdbKey!.Trim());
        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) throw new Exception("SteamGridDB said " + (int)resp.StatusCode + (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized ? " (check the key)" : ""));
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    }

    public static async Task<bool> FetchAsync(Game g)
    {
        if (!Configured) return false;
        try
        {
            var file = Path.Combine(Dir, Key(g) + ".jpg");
            if (File.Exists(file)) return true;
            string url;
            if (g.AppId != null) url = "/heroes/steam/" + g.AppId + "?dimensions=1920x620,3840x1240&nsfw=false&humor=false";
            else
            {
                using var s = await Get("/search/autocomplete/" + Uri.EscapeDataString(g.Title.Replace("\u2122", "").Replace("\u00AE", "")));
                var data = s.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0) return false;
                url = "/heroes/game/" + data[0].GetProperty("id").GetInt32() + "?dimensions=1920x620,3840x1240&nsfw=false&humor=false";
            }
            using var doc = await Get(url);
            var list = doc.RootElement.GetProperty("data");
            if (list.GetArrayLength() == 0) return false;
            var img = list[0].GetProperty("url").GetString();
            if (img == null) return false;
            var bytes = await http.GetByteArrayAsync(img);
            if (bytes.Length < 10000) return false;
            await File.WriteAllBytesAsync(file, bytes);
            return true;
        }
        catch (Exception e) { AppLog.Write("sgdb: " + e.Message); return false; }
    }

    public static async Task PrefetchAsync(IEnumerable<Game> games)
    {
        if (!Configured) return;
        foreach (var g in games.ToList())
        {
            if (ArtPath(g) != null) continue;
            await FetchAsync(g);
            await Task.Delay(400);
        }
    }

    public static async Task<string> Test()
    {
        if (!Configured) return "Paste the key first.";
        try { using var d = await Get("/search/autocomplete/portal"); return "Connected to SteamGridDB."; }
        catch (Exception e) { return e.Message; }
    }
}
