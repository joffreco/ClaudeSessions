namespace ClaudeSessions.Core.Domain;

/// <summary>What the user changed in the look and feel of the terminals, from the keyboard or the menu.</summary>
/// <param name="FontSize">Zoomed font size, in points, or null when the terminals are not zoomed.</param>
/// <param name="WheelLines">Lines scrolled per wheel notch, or null to follow the Windows setting.</param>
public sealed record ViewPreferences(short? FontSize, int? WheelLines)
{
    /// <summary>No preference: configured font size, Windows wheel setting.</summary>
    public static ViewPreferences None { get; } = new(null, null);
}
