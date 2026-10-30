namespace ClaudeSessions.Adapters.Dto;

/// <summary>Serialized session tab.</summary>
public sealed record TabDocument
{
    /// <summary>Identifier of the tab.</summary>
    public string Id { get; init; } = "";

    /// <summary>WSL working directory of the session.</summary>
    public string Cwd { get; init; } = "";

    /// <summary>Claude Code session the tab runs.</summary>
    public string SessionId { get; init; } = "";

    /// <summary>Last title the session gave the tab, if any.</summary>
    public string? Title { get; init; }
}
