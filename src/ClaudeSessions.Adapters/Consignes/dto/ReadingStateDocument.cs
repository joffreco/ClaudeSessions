using System;
using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Dto;

/// <summary>Serialized reading state, the content of state.json.</summary>
public sealed record ReadingStateDocument
{
    /// <summary>Gets the update time seen for each consigne, by key.</summary>
    public Dictionary<string, DateTimeOffset> Seen { get; init; } = [];

    /// <summary>Gets the ticked task indexes of each consigne, by key.</summary>
    public Dictionary<string, List<int>> Ticked { get; init; } = [];
}
