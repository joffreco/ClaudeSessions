using System;

namespace ClaudeSessions.Core.Domain;

/// <summary>Position and size of a window on the desktop, when it is not maximized.</summary>
/// <param name="Left">Distance from the left edge of the virtual screen.</param>
/// <param name="Top">Distance from the top edge of the virtual screen.</param>
/// <param name="Width">Width of the window when it is not maximized.</param>
/// <param name="Height">Height of the window when it is not maximized.</param>
/// <param name="Maximized">True when the window is maximized.</param>
/// <param name="Unit">Unit of the coordinates and sizes.</param>
public sealed record WindowBounds(double Left, double Top, double Width, double Height, bool Maximized, BoundsUnit Unit = BoundsUnit.DeviceIndependent)
{
    /// <summary>Width of the title bar that must lie on a work area for the window to be reachable.</summary>
    public const double ReachableTitleWidth = 120;

    /// <summary>Height of the title bar that must lie on a work area for the window to be reachable.</summary>
    public const double ReachableTitleHeight = 30;

    /// <summary>Bounds given to a new window before the desktop places it.</summary>
    public static readonly WindowBounds Default = new(120, 80, 1200, 760, false);

    /// <summary>Tells whether enough of the title bar lies on a work area for the user to grab the window.</summary>
    /// <param name="workArea">The work area, in the unit of these bounds.</param>
    /// <returns>True when the top strip of the window overlaps the work area by at least <see cref="ReachableTitleWidth"/> (or the whole width of a narrower window) by <see cref="ReachableTitleHeight"/>.</returns>
    public bool TitleBarLiesOn(ScreenRectangle workArea)
    {
        double overlapWidth = Math.Min(Left + Width, workArea.Right) - Math.Max(Left, workArea.Left);
        double overlapHeight = Math.Min(Top + ReachableTitleHeight, workArea.Bottom) - Math.Max(Top, workArea.Top);
        return Width > 0 && overlapWidth >= Math.Min(ReachableTitleWidth, Width) && overlapHeight >= ReachableTitleHeight;
    }
}
