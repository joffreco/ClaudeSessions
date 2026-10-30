using Microsoft.Terminal.Wpf;

namespace ClaudeSessions.Windows.Terminal;

/// <summary>Color themes for the terminals.</summary>
internal static class TerminalThemes
{
    /// <summary>Background of the terminals and of the window around them, as 0xRRGGBB.</summary>
    public const uint BackgroundRgb = 0x0C0C0C;

    /// <summary>The Campbell theme of Windows Terminal.</summary>
    /// <returns>The theme.</returns>
    public static TerminalTheme Campbell()
    {
        return new TerminalTheme
        {
            DefaultBackground = ColorRef(BackgroundRgb),
            DefaultForeground = ColorRef(0xCCCCCC),
            DefaultSelectionBackground = ColorRef(0x264F78),
            CursorStyle = CursorStyle.BlinkingBar,
            ColorTable =
            [
                ColorRef(0x0C0C0C), ColorRef(0xC50F1F), ColorRef(0x13A10E), ColorRef(0xC19C00),
                ColorRef(0x0037DA), ColorRef(0x881798), ColorRef(0x3A96DD), ColorRef(0xCCCCCC),
                ColorRef(0x767676), ColorRef(0xE74856), ColorRef(0x16C60C), ColorRef(0xF9F1A5),
                ColorRef(0x3B78FF), ColorRef(0xB4009E), ColorRef(0x61D6D6), ColorRef(0xF2F2F2),
            ],
        };
    }

    private static uint ColorRef(uint rgb)
    {
        uint red = (rgb >> 16) & 0xFF;
        uint green = (rgb >> 8) & 0xFF;
        uint blue = rgb & 0xFF;
        return red | (green << 8) | (blue << 16);
    }
}
