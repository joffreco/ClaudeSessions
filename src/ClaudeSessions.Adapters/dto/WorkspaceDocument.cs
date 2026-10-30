using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Dto;

/// <summary>Serialized workspace, the content of workspace.json.</summary>
public sealed record WorkspaceDocument
{
    /// <summary>Version of the format written by this application.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Version of the format of the document.</summary>
    public int Version { get; init; }

    /// <summary>Windows, open and closed.</summary>
    public List<WindowDocument> Windows { get; init; } = [];

    /// <summary>Closed tabs, most recent last.</summary>
    public List<ClosedTabDocument> ClosedTabs { get; init; } = [];
}
