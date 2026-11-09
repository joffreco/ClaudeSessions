namespace ClaudeSessions.Core.Domain;

/// <summary>Unit of the coordinates of a <see cref="WindowBounds"/>.</summary>
public enum BoundsUnit
{
    /// <summary>WPF device-independent pixels; bounds saved before positions were kept in device pixels, and the default bounds.</summary>
    DeviceIndependent,

    /// <summary>Physical screen pixels, as the window placement of Windows reports them.</summary>
    DevicePixels,
}
