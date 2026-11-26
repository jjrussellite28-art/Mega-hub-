using System.Text.Json;
using System.Xml.Linq;

namespace MegaHubPlus;

// Weather chip (Open-Meteo, no key) and the News tab (any RSS feed). Both optional, both need nothing but internet.
public static class Weather
{
    public static string? Text;
    public static Action? Changed;
    static DateTime last = DateTime.MinValue;
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };

    static string Icon(int code) => code == 0 ? "☀" : code <= 3 ? "⛅" : code <= 48 ? "☁" : code <= 67 || (code >= 80 && code <= 82) ? "☂" : code <= 77 || code == 85 || code == 86 ? "❄" : "⚡";

    public static async Task RefreshAsync(AppSettings s, bool force = false)
    {
        if (string.IsNullOrWhiteSpace(s.WeatherCity)) { Text = null; Changed?.Invoke(); return; }
        if (!force && DateTime.UtcNow - last < TimeSpan.FromMinutes(30) && Text != null) return;
        try
        {
            using var g = JsonDocument.Parse(await http.GetStringAsync("https://geocoding-api.open-meteo.com/v1/search?count=1&name=" + Uri.EscapeDataString(s.WeatherCity.Trim())));
            if (!g.RootElement.TryGetProperty("results", out var rs) || rs.GetArrayLength() == 0) { Text = "?"; Changed?.Invoke(); return; }
            var lat = rs[0].GetProperty("latitude").GetDouble(); var lon = rs[0].GetProperty("longitude").GetDouble();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            using var w = JsonDocument.Parse(await http.GetStringAsync("https://api.open-meteo.com/v1/forecast?latitude=" + lat.ToString(inv) + "&longitude=" + lon.ToString(inv) + "&current=temperature_2m,weather_code&temperature_unit=fahrenheit"));
            var cur = w.RootElement.GetProperty("current");
            Text = Icon(cur.GetProperty("weather_code").GetInt32()) + " " + Math.Round(cur.GetProperty("temperature_2m").GetDouble()) + "°F";
            last = DateTime.UtcNow;
            Changed?.Invoke();
        }
        catch (Exception e) { AppLog.Write("weather: " + e.Message); }
    }
}

public static class News
{
    public record Item(string Title, string Link, string When);
    public static List<Item> Items = new();
    public static string Status = "Loading...";
    public static Action? Changed;
    static DateTime last = DateTime.MinValue;
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task RefreshAsync(AppSettings s, bool force = false)
    {
        if (!force && DateTime.UtcNow - last < TimeSpan.FromMinutes(15) && Items.Count > 0) return;
        var url = string.IsNullOrWhiteSpace(s.NewsFeed) ? "https://feeds.bbci.co.uk/news/rss.xml" : s.NewsFeed.Trim();
        try
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MegaHubPlus");
            var doc = XDocument.Parse(await http.GetStringAsync(url));
            var list = doc.Descendants("item").Take(30).Select(i => new Item(
                (string?)i.Element("title") ?? "", (string?)i.Element("link") ?? "",
                DateTimeOffset.TryParse((string?)i.Element("pubDate"), out var d) ? d.LocalDateTime.ToString("MMM d, h:mm tt") : "")).Where(i => i.Title != "" && i.Link.StartsWith("http")).ToList();
            Items = list; Status = list.Count == 0 ? "No stories found in that feed." : "";
            last = DateTime.UtcNow;
        }
        catch (Exception e) { Status = "Could not load the news: " + e.Message; AppLog.Write("news: " + e.Message); }
        Changed?.Invoke();
    }
}
