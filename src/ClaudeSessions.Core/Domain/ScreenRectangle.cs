namespace ClaudeSessions.Core.Domain;

/// <summary>A rectangle of the desktop, such as the work area of a monitor, its right and bottom edges excluded.</summary>
/// <param name="Left">Left edge.</param>
/// <param name="Top">Top edge.</param>
/// <param name="Right">Right edge, excluded.</param>
/// <param name="Bottom">Bottom edge, excluded.</param>
public sealed record ScreenRectangle(double Left, double Top, double Right, double Bottom);
