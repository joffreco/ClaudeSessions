using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Dto;

/// <summary>Serialized window.</summary>
public sealed record WindowDocument
{
    /// <summary>Identifier of the window.</summary>
    public string Id { get; init; } = "";

    /// <summary>"Open" or "Closed".</summary>
    public string Status { get; init; } = "";

    /// <summary>Position and size of the window.</summary>
    public BoundsDocument Bounds { get; init; } = new();

    /// <summary>Tab shown in the window, if any.</summary>
    public string? SelectedTabId { get; init; }

    /// <summary>Tabs of the window, in display order.</summary>
    public List<TabDocument> Tabs { get; init; } = [];
}
