using System.Security.Cryptography;
using System.Text;

namespace MegaHubPlus;

// Add-ons live in %AppData%\MegaHubPlus\addons\<name>\ with addon.json and addon.sig.
// addon.sig is a signature of addon.json made with the owner's private key. Unsigned or changed add-ons never load.
public static class Addons
{
    const string PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEo59aGYbaMo6WQXTnzt+S4D4AUZAJ6ViuIGxl4UMqwMfWK/eAHEuUWv9LTEmIb5/GyTcYzaYRMhfTS50ykbwPpw==";

    public record Item(string Name, string Info, bool Signed);

    public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegaHubPlus", "addons");

    public static List<Item> Scan()
    {
        var list = new List<Item>();
        try
        {
            Directory.CreateDirectory(Dir);
            foreach (var d in Directory.EnumerateDirectories(Dir))
            {
                var name = Path.GetFileName(d);
                var json = Path.Combine(d, "addon.json");
                var sig = Path.Combine(d, "addon.sig");
                if (!File.Exists(json)) continue;
                var ok = File.Exists(sig) && Verify(File.ReadAllBytes(json), File.ReadAllBytes(sig));
                list.Add(new Item(name, ok ? "Signed" : "Blocked (not signed)", ok));
            }
        }
        catch (Exception e) { AppLog.Write("addon scan failed: " + e.Message); }
        return list;
    }

    static bool Verify(byte[] data, byte[] sig)
    {
        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKey), out _);
            return key.VerifyData(data, sig, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch { return false; }
    }
}
