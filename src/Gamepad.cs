using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Threading;

namespace MegaHubPlus;

// Reads an Xbox-style controller (XInput) and turns it into key presses.
// D-pad / left stick = arrows, A = Enter, B = Escape, Start = S (Settings).
public class Gamepad
{
    [StructLayout(LayoutKind.Sequential)]
    struct XState { public uint Packet; public ushort Buttons; public byte LT, RT; public short LX, LY, RX, RY; }

    [DllImport("xinput1_4.dll")]
    static extern int XInputGetState(int user, out XState state);

    readonly Action<Key> press;
    readonly Action? emergencyQuit;
    DateTime? comboSince;
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    ushort prev;
    int stickDir; // -1 left, 1 right, 0 none
    DateTime nextRepeat;

    public bool Connected { get; private set; }

    public Gamepad(Action<Key> press, Action? emergencyQuit = null)
    {
        this.press = press;
        this.emergencyQuit = emergencyQuit;
        timer.Tick += (_, _) => Poll();
        timer.Start();
    }

    void Poll()
    {
        XState s;
        try { Connected = false; for (int i = 0; i < 4; i++) if (XInputGetState(i, out s) == 0) { Connected = true; Handle(s); return; } }
        catch { timer.Stop(); }
    }

    void Handle(XState s)
    {
        ushort b = s.Buttons;
        // emergency exit: hold Back + Start for 3 seconds
        if ((b & 0x0030) == 0x0030)
        {
            comboSince ??= DateTime.UtcNow;
            if (DateTime.UtcNow - comboSince.Value > TimeSpan.FromSeconds(3)) emergencyQuit?.Invoke();
        }
        else comboSince = null;
        ushort down = (ushort)(b & ~prev);
        prev = b;
        if ((down & 0x0004) != 0) press(Key.Left);   // dpad left
        if ((down & 0x0008) != 0) press(Key.Right);  // dpad right
        if ((down & 0x0001) != 0) press(Key.Up);
        if ((down & 0x0002) != 0) press(Key.Down);
        if ((down & 0x1000) != 0) press(Key.Enter);  // A
        if ((down & 0x2000) != 0) press(Key.Escape); // B
        if ((down & 0x0010) != 0) press(Key.S);      // Start

        int dir = s.LX < -16000 ? -1 : s.LX > 16000 ? 1 : 0;
        if (dir == 0) { stickDir = 0; return; }
        var now = DateTime.UtcNow;
        if (dir != stickDir) { stickDir = dir; nextRepeat = now.AddMilliseconds(350); press(dir < 0 ? Key.Left : Key.Right); }
        else if (now >= nextRepeat) { nextRepeat = now.AddMilliseconds(130); press(dir < 0 ? Key.Left : Key.Right); }
    }
}
