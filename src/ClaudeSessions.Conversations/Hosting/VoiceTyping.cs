using System.Runtime.InteropServices;

namespace ClaudeSessions.Conversations.Hosting;

/// <summary>
/// Turns the voice typing of Windows on and off by pressing Win+H for the user, the shortcut that toggles it.
/// Windows does not tell whether voice typing listens, so the state kept here is what was last asked: it
/// drifts when Windows stops listening by itself, after a long silence or when the focus leaves the field.
/// </summary>
internal static partial class VoiceTyping
{
    private const byte LeftWindowsKey = 0x5B;
    private const byte HKey = 0x48;
    private const uint KeyUp = 0x0002;

    /// <summary>Gets a value indicating whether voice typing was last turned on from here.</summary>
    public static bool IsOn { get; private set; }

    /// <summary>Turns voice typing on, or off when it was on.</summary>
    public static void Toggle()
    {
        PressWinH();
        IsOn = !IsOn;
    }

    /// <summary>Forgets that voice typing was on, when Windows has stopped it by itself.</summary>
    public static void Forget()
    {
        IsOn = false;
    }

    private static void PressWinH()
    {
        KeyboardEvent(LeftWindowsKey, 0, 0, 0);
        KeyboardEvent(HKey, 0, 0, 0);
        KeyboardEvent(HKey, 0, KeyUp, 0);
        KeyboardEvent(LeftWindowsKey, 0, KeyUp, 0);
    }

    [LibraryImport("user32.dll", EntryPoint = "keybd_event")]
    private static partial void KeyboardEvent(byte virtualKey, byte scanCode, uint flags, nuint extraInfo);
}
