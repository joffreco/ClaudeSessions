using System.Windows;
using System.Windows.Media;

namespace ClaudeSessions.Windows.Views;

/// <summary>Colors of the window around the terminals: tab strip, tabs, banner and window buttons.</summary>
internal static class ChromePalette
{
    /// <summary>Background of the window and of its tab strip.</summary>
    public static readonly Brush Chrome = Frozen(Color.FromRgb(0x1F, 0x1F, 0x1F));

    /// <summary>Background of the selected tab.</summary>
    public static readonly Brush SelectedTab = Frozen(Color.FromRgb(0x0C, 0x0C, 0x0C));

    /// <summary>Background of the other tabs.</summary>
    public static readonly Brush IdleTab = Frozen(Color.FromRgb(0x2D, 0x2D, 0x2D));

    /// <summary>Text and glyphs.</summary>
    public static readonly Brush Text = Frozen(Color.FromRgb(0xE0, 0xE0, 0xE0));

    /// <summary>The state mark of a session that waits for the user, green as in Windows Terminal.</summary>
    public static readonly Brush IdleMark = Frozen(Color.FromRgb(0x16, 0xC6, 0x0C));

    /// <summary>Background of a tab whose session finished working while nobody watched it.</summary>
    public static readonly Brush WaitingTab = Frozen(Color.FromRgb(0x1E, 0x4D, 0x24));

    /// <summary>Background of a tab whose session asks for an approval while nobody watches it.</summary>
    public static readonly Brush ApprovalTab = Frozen(Color.FromRgb(0x8A, 0x4B, 0x08));

    /// <summary>Badge of the taskbar button of a window with a waiting tab: a green dot with a dark ring.</summary>
    public static readonly ImageSource WaitingBadge = Badge(IdleMark);

    /// <summary>Badge of the taskbar button of a window whose session asks for an approval: an orange dot with a dark ring.</summary>
    public static readonly ImageSource ApprovalBadge = Badge(Frozen(Color.FromRgb(0xF7, 0x8C, 0x0C)));

    /// <summary>Background of the banner of a failed session.</summary>
    public static readonly Brush Banner = Frozen(Color.FromRgb(0x5A, 0x1D, 0x1D));

    /// <summary>Background of a window button under the mouse.</summary>
    public static readonly Brush CaptionHover = Frozen(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));

    /// <summary>Background of the close button under the mouse.</summary>
    public static readonly Brush CloseHover = Frozen(Color.FromRgb(0xC4, 0x2B, 0x1C));

    private static DrawingImage Badge(Brush fill)
    {
        Pen ring = new(Frozen(Color.FromRgb(0x0C, 0x0C, 0x0C)), 2);
        ring.Freeze();
        GeometryDrawing dot = new(fill, ring, new EllipseGeometry(new Point(8, 8), 7, 7));
        DrawingImage image = new(dot);
        image.Freeze();
        return image;
    }

    private static SolidColorBrush Frozen(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
