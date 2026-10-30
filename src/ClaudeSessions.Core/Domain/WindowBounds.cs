namespace ClaudeSessions.Core.Domain;

/// <summary>Position and size of a window on the desktop, in device-independent pixels.</summary>
/// <param name="Left">Distance from the left edge of the virtual screen.</param>
/// <param name="Top">Distance from the top edge of the virtual screen.</param>
/// <param name="Width">Width of the window when it is not maximized.</param>
/// <param name="Height">Height of the window when it is not maximized.</param>
/// <param name="Maximized">True when the window is maximized.</param>
public sealed record WindowBounds(double Left, double Top, double Width, double Height, bool Maximized)
{
    /// <summary>Bounds given to a new window before the desktop places it.</summary>
    public static readonly WindowBounds Default = new(120, 80, 1200, 760, false);
}
