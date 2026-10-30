using System;

namespace ClaudeSessions.Adapters.Dto;

/// <summary>Serialized closed tab.</summary>
public sealed record ClosedTabDocument
{
    /// <summary>The tab as it was when it closed.</summary>
    public TabDocument Tab { get; init; } = new();

    /// <summary>Window the tab belonged to.</summary>
    public string WindowId { get; init; } = "";

    /// <summary>When the tab was closed.</summary>
    public DateTimeOffset ClosedAt { get; init; }
}
