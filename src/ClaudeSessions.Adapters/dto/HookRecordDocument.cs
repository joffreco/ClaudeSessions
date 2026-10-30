using System.Text.Json.Serialization;

namespace ClaudeSessions.Adapters.Dto;

/// <summary>Record written by claude-sessions-hook.sh for a tab, in ~/.claude/session-tabs/&lt;tab&gt;.json.</summary>
public sealed record HookRecordDocument
{
    /// <summary>Tab the record belongs to.</summary>
    [JsonPropertyName("tab_id")]
    public string? TabId { get; init; }

    /// <summary>Session the tab runs right now.</summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }
}
