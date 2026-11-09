namespace ClaudeSessions.Adapters.Dto;

/// <summary>Serialized position and size of a window.</summary>
public sealed record BoundsDocument
{
    /// <summary>Distance from the left edge of the virtual screen.</summary>
    public double Left { get; init; }

    /// <summary>Distance from the top edge of the virtual screen.</summary>
    public double Top { get; init; }

    /// <summary>Width of the window when it is not maximized.</summary>
    public double Width { get; init; }

    /// <summary>Height of the window when it is not maximized.</summary>
    public double Height { get; init; }

    /// <summary>True when the window is maximized.</summary>
    public bool Maximized { get; init; }

    /// <summary>Unit of the coordinates, a <c>BoundsUnit</c> name; absent in files saved before it existed, which used device-independent pixels.</summary>
    public string? Unit { get; init; }
}
