using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MegaHubPlus;

// Button pictures (keyboard, Xbox, PlayStation, Nintendo, Sega). Automatic = the pad that is plugged in, else the keyboard.
public static class PadIcons
{
    public static readonly string[] All =
    {
        "Keyboard",
        "Xbox-Series", "Xbox-One", "Xbox-360", "Original-Xbox",
        "PS5", "PS4", "PS3", "PS2", "PS1-Digital",
        "Switch", "Switch-2", "GameCube", "Nintendo-64", "Wii", "Super-Famicom", "NES",
        "Dreamcast", "Saturn", "Genesis-3-button", "Master-System",
    };

    public static string Pretty(string id) => id == "auto" ? "Automatic" : id.Replace('-', ' ');

    // the saved choice ("auto" or a preset) + what is plugged in -> which preset to show
    public static string Resolve(string? choice, bool padConnected, string padPreset)
    {
        if (!string.IsNullOrEmpty(choice) && choice != "auto" && Array.IndexOf(All, choice) >= 0) return choice;
        return padConnected ? padPreset : "Keyboard";
    }

    // pad name -> preset (best guess, user can always pick one by hand)
    public static string FromName(string? name)
    {
        var n = (name ?? "").ToLowerInvariant();
        if (n.Contains("dualsense") || n.Contains("ps5")) return "PS5";
        if (n.Contains("dualshock 3") || n.Contains("sixaxis") || n.Contains("ps3")) return "PS3";
        if (n.Contains("ps2")) return "PS2";
        if (n.Contains("dualshock") || n.Contains("ps4") || n.Contains("wireless controller") || n.Contains("playstation")) return "PS4";
        if (n.Contains("xbox 360")) return "Xbox-360";
        if (n.Contains("xbox one")) return "Xbox-One";
        if (n.Contains("xbox")) return "Xbox-Series";
        if (n.Contains("gamecube")) return "GameCube";
        if (n.Contains("nintendo 64") || n.Contains("n64")) return "Nintendo-64";
        if (n.Contains("wii")) return "Wii";
        if (n.Contains("switch 2")) return "Switch-2";
        if (n.Contains("switch") || n.Contains("joy-con") || n.Contains("pro controller") || n.Contains("nintendo")) return "Switch";
        if (n.Contains("dreamcast")) return "Dreamcast";
        if (n.Contains("saturn")) return "Saturn";
        if (n.Contains("genesis") || n.Contains("mega drive")) return "Genesis-3-button";
        if (n.Contains("super famicom") || n.Contains("snes")) return "Super-Famicom";
        if (n.Contains("nes")) return "NES";
        return "Xbox-Series";
    }

    static readonly Dictionary<string, string[]> RoleFiles = new()
    {
        ["A"] = new[] { "a", "cross", "enter", "one-start" },
        ["B"] = new[] { "b", "circle", "escape", "two" },
        ["Y"] = new[] { "y", "triangle" },
        ["≡"] = new[] { "settings", "start" },
        ["BACK"] = new[] { "back" },
        ["X"] = new[] { "x", "square" },
        ["LT"] = new[] { "lt", "l2", "zl" },
        ["RT"] = new[] { "rt", "r2", "zr" },
        ["UP"] = new[] { "dpad-up", "up" },
        ["DOWN"] = new[] { "dpad-down", "down" },
        ["LEFT"] = new[] { "dpad-left", "left" },
        ["RIGHT"] = new[] { "dpad-right", "right" },
        ["LS"] = new[] { "left-stick", "ls" },
        ["RS"] = new[] { "right-stick", "c-stick", "rs" },
        ["LB"] = new[] { "lb", "l1", "l", "zl" },
        ["RB"] = new[] { "rb", "r1", "r", "zr" },
    };

    static readonly Dictionary<string, Bitmap?> cache = new();

    // picture for a role (A = select, B = back, Y = later, ≡ = settings) on a preset; null = none in the pack
    public static Bitmap? Role(string preset, string role, bool pressed)
    {
        if (!RoleFiles.TryGetValue(role, out var files)) return null;
        foreach (var f in files)
        {
            var key = preset + "/" + (pressed ? "Pressed/" : "") + f;
            if (cache.TryGetValue(key, out var bm)) { if (bm != null) return bm; continue; }
            bm = null;
            try
            {
                var uri = new Uri("avares://MegaHubPlus/Assets/pads/" + key + ".png");
                if (AssetLoader.Exists(uri)) bm = new Bitmap(AssetLoader.Open(uri));
            }
            catch { }
            cache[key] = bm;
            if (bm != null) return bm;
        }
        // not in this controller's set: the plain glossy one made for gaps (Back, Start, stick clicks)
        foreach (var f in files)
        {
            var key = "_generic/" + (pressed ? "Pressed/" : "") + f;
            if (cache.TryGetValue(key, out var bm)) { if (bm != null) return bm; continue; }
            bm = null;
            try
            {
                var uri = new Uri("avares://MegaHubPlus/Assets/pads/" + key + ".png");
                if (AssetLoader.Exists(uri)) bm = new Bitmap(AssetLoader.Open(uri));
            }
            catch { }
            cache[key] = bm;
            if (bm != null) return bm;
        }
        return null;
    }
}
