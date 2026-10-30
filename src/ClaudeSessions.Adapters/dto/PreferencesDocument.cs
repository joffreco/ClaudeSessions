namespace ClaudeSessions.Adapters.Dto;

/// <summary>Serialized view preferences.</summary>
public sealed record PreferencesDocument
{
    /// <summary>Zoomed font size, in points, or null when not zoomed.</summary>
    public short? FontSize { get; init; }

    /// <summary>Lines scrolled per wheel notch, or null to follow Windows.</summary>
    public int? WheelLines { get; init; }
}
