using Silk.NET.SDL;

namespace MegaHubPlus;

// PlayStation / Nintendo / other controllers through SDL (it knows the layout of hundreds of pads).
// Used only when no Xbox-style (XInput) controller is found. If SDL can't load, this quietly does nothing.
public sealed unsafe class SdlPad
{
    Sdl? sdl;
    GameController* pad;
    bool failed;
    DateTime nextScan;
    public string Kind = "Xbox";

    bool Init()
    {
        if (sdl != null) return true;
        if (failed) return false;
        try
        {
            sdl = Sdl.GetApi();
            if (sdl.Init(Sdl.InitGamecontroller) < 0) { failed = true; sdl = null; return false; }
            return true;
        }
        catch { failed = true; sdl = null; return false; }
    }

    static string KindOf(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("ps4") || n.Contains("ps5") || n.Contains("dualshock") || n.Contains("dualsense") || n.Contains("playstation") || n.Contains("wireless controller")) return "PlayStation";
        if (n.Contains("nintendo") || n.Contains("switch") || n.Contains("joy-con") || n.Contains("pro controller")) return "Switch";
        return "Xbox";
    }

    public bool TryRead(out ushort buttons, out short lx)
    {
        buttons = 0; lx = 0;
        try
        {
            if (!Init()) return false;
            if (pad == null && DateTime.UtcNow >= nextScan)
            {
                nextScan = DateTime.UtcNow.AddSeconds(1);
                sdl!.GameControllerUpdate();
                int n = sdl.NumJoysticks();
                for (int i = 0; i < n && pad == null; i++)
                    if (sdl.IsGameController(i) == SdlBool.True)
                    {
                        pad = sdl.GameControllerOpen(i);
                        if (pad != null) Kind = KindOf(sdl.GameControllerNameS(pad) ?? "");
                    }
            }
            if (pad == null) return false;
            sdl!.GameControllerUpdate();
            if (sdl.GameControllerGetAttached(pad) != SdlBool.True) { sdl.GameControllerClose(pad); pad = null; return false; }
            bool B(GameControllerButton b) => sdl!.GameControllerGetButton(pad, b) != 0;
            if (B(GameControllerButton.Dpadup)) buttons |= 0x0001;
            if (B(GameControllerButton.Dpaddown)) buttons |= 0x0002;
            if (B(GameControllerButton.Dpadleft)) buttons |= 0x0004;
            if (B(GameControllerButton.Dpadright)) buttons |= 0x0008;
            if (B(GameControllerButton.Start)) buttons |= 0x0010;
            if (B(GameControllerButton.Back)) buttons |= 0x0020;
            if (B(GameControllerButton.A)) buttons |= 0x1000;
            if (B(GameControllerButton.B)) buttons |= 0x2000;
            lx = sdl.GameControllerGetAxis(pad, GameControllerAxis.Leftx);
            return true;
        }
        catch { failed = true; pad = null; return false; }
    }
}
