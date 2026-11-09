using System.Windows;
using System.Windows.Media;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// Colors of the window around the terminals: tab strip, tabs, banner and window buttons. The brushes are shared and
/// repainted in place when Windows switches between light and dark, so every element that uses one follows at once.
/// The selected tab keeps the background of the terminals, which stay dark in both modes, as in Windows Terminal.
/// The taskbar badges keep their own frozen colors: freezing an image freezes the brushes it holds.
/// </summary>
internal static class ChromePalette
{
    /// <summary>Background of the window and of its tab strip.</summary>
    public static readonly SolidColorBrush Chrome = new();

    /// <summary>Background of the selected tab, the one of the terminals.</summary>
    public static readonly SolidColorBrush SelectedTab = new();

    /// <summary>Background of the other tabs.</summary>
    public static readonly SolidColorBrush IdleTab = new();

    /// <summary>Text and glyphs on the chrome.</summary>
    public static readonly SolidColorBrush Text = new();

    /// <summary>Text of the selected tab, on the background of the terminals.</summary>
    public static readonly SolidColorBrush SelectedTabText = new();

    /// <summary>The state mark of a session that waits for the user, green as in Windows Terminal.</summary>
    public static readonly SolidColorBrush IdleMark = new();

    /// <summary>Background of a tab whose session finished working while nobody watched it.</summary>
    public static readonly SolidColorBrush WaitingTab = new();

    /// <summary>The mark of a session that asks for an approval.</summary>
    public static readonly SolidColorBrush ApprovalMark = new();

    /// <summary>Background of a tab whose session asks for an approval while nobody watches it.</summary>
    public static readonly SolidColorBrush ApprovalTab = new();

    /// <summary>Background of a field or list on the chrome, such as those of the tab switcher.</summary>
    public static readonly SolidColorBrush Field = new();

    /// <summary>Background of the banner of a failed session.</summary>
    public static readonly SolidColorBrush Banner = new();

    /// <summary>Background of a window button under the mouse.</summary>
    public static readonly SolidColorBrush CaptionHover = new();

    /// <summary>Background of the close button under the mouse.</summary>
    public static readonly SolidColorBrush CloseHover = new();

    /// <summary>Badge of the taskbar button of a window with a waiting tab: a green dot with a dark ring.</summary>
    public static readonly ImageSource WaitingBadge = Badge(0xFF16C60C);

    /// <summary>Badge of the taskbar button of a window whose session asks for an approval: an orange dot with a dark ring.</summary>
    public static readonly ImageSource ApprovalBadge = Badge(0xFFF78C0C);

    /// <summary>Repaints every brush for the light or the dark mode.</summary>
    /// <param name="dark">True for the dark mode.</param>
    public static void Apply(bool dark)
    {
        Paint(Chrome, dark, 0xFF1F1F1F, 0xFFF3F3F3);
        Paint(SelectedTab, dark, 0xFF0C0C0C, 0xFF0C0C0C);
        Paint(IdleTab, dark, 0xFF2D2D2D, 0xFFE3E3E3);
        Paint(Text, dark, 0xFFE0E0E0, 0xFF1A1A1A);
        Paint(SelectedTabText, dark, 0xFFE0E0E0, 0xFFE0E0E0);
        Paint(IdleMark, dark, 0xFF16C60C, 0xFF13A10E);
        Paint(WaitingTab, dark, 0xFF1E4D24, 0xFFC7E8CA);
        Paint(ApprovalMark, dark, 0xFFF78C0C, 0xFFD06A00);
        Paint(ApprovalTab, dark, 0xFF8A4B08, 0xFFFAD9AE);
        Paint(Field, dark, 0xFF0C0C0C, 0xFFFFFFFF);
        Paint(Banner, dark, 0xFF5A1D1D, 0xFFFDE7E9);
        Paint(CaptionHover, dark, 0x33FFFFFF, 0x1A000000);
        Paint(CloseHover, dark, 0xFFC42B1C, 0xFFC42B1C);
    }

    private static void Paint(SolidColorBrush brush, bool dark, uint darkArgb, uint lightArgb)
    {
        brush.Color = ColorOf(dark ? darkArgb : lightArgb);
    }

    private static Color ColorOf(uint argb)
    {
        return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }

    private static DrawingImage Badge(uint fillArgb)
    {
        SolidColorBrush fill = new(ColorOf(fillArgb));
        Pen ring = new(new SolidColorBrush(ColorOf(0xFF0C0C0C)), 2);
        GeometryDrawing dot = new(fill, ring, new EllipseGeometry(new Point(8, 8), 7, 7));
        DrawingImage image = new(dot);
        image.Freeze();
        return image;
    }
}
