using Microsoft.Terminal.Wpf;

namespace ClaudeSessions.Windows.Terminal;

/// <summary>Color themes for the terminals.</summary>
internal static class TerminalThemes
{
    /// <summary>Background of the terminals in the dark mode, as 0xRRGGBB.</summary>
    public const uint DarkBackgroundRgb = 0x0C0C0C;

    /// <summary>Background of the terminals in the light mode, as 0xRRGGBB.</summary>
    public const uint LightBackgroundRgb = 0xFAFAFA;

    /// <summary>The theme of the light or the dark mode.</summary>
    /// <param name="dark">True for the dark mode.</param>
    /// <returns>Campbell when dark, One Half Light otherwise, as in Windows Terminal.</returns>
    public static TerminalTheme For(bool dark)
    {
        return dark ? Campbell() : OneHalfLight();
    }

    /// <summary>The background of the light or the dark theme.</summary>
    /// <param name="dark">True for the dark mode.</param>
    /// <returns>The background, as 0xRRGGBB.</returns>
    public static uint BackgroundOf(bool dark)
    {
        return dark ? DarkBackgroundRgb : LightBackgroundRgb;
    }

    /// <summary>The Campbell theme of Windows Terminal.</summary>
    /// <returns>The theme.</returns>
    public static TerminalTheme Campbell()
    {
        return new TerminalTheme
        {
            DefaultBackground = ColorRef(DarkBackgroundRgb),
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

    /// <summary>The One Half Light theme of Windows Terminal.</summary>
    /// <returns>The theme.</returns>
    public static TerminalTheme OneHalfLight()
    {
        return new TerminalTheme
        {
            DefaultBackground = ColorRef(LightBackgroundRgb),
            DefaultForeground = ColorRef(0x383A42),
            DefaultSelectionBackground = ColorRef(0xBFCEFF),
            CursorStyle = CursorStyle.BlinkingBar,
            ColorTable =
            [
                ColorRef(0x383A42), ColorRef(0xE45649), ColorRef(0x50A14F), ColorRef(0xC18301),
                ColorRef(0x0184BC), ColorRef(0xA626A4), ColorRef(0x0997B3), ColorRef(0xFAFAFA),
                ColorRef(0x4F525D), ColorRef(0xDF6C75), ColorRef(0x98C379), ColorRef(0xE4C07A),
                ColorRef(0x61AFEF), ColorRef(0xC577DD), ColorRef(0x56B5C1), ColorRef(0xFFFFFF),
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
