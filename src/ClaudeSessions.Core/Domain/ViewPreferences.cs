namespace ClaudeSessions.Core.Domain;

/// <summary>What the user chose for the terminals, from the keyboard or the menu.</summary>
/// <param name="FontSize">Zoomed font size, in points, or null when the terminals are not zoomed.</param>
/// <param name="WheelLines">Lines scrolled per wheel notch, or null to follow the Windows setting.</param>
/// <param name="SuspendAfterHours">Hours of inactivity before a tab is put to sleep, or null when tabs never sleep.</param>
public sealed record ViewPreferences(short? FontSize, int? WheelLines, int? SuspendAfterHours = null)
{
    /// <summary>No preference: configured font size, Windows wheel setting, tabs never sleep.</summary>
    public static ViewPreferences None { get; } = new(null, null);
}
