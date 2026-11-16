using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>The end of a turn: Claude Code finished answering a prompt, or the user interrupted it.</summary>
/// <param name="Timestamp">When the turn ended.</param>
/// <param name="Duration">How long the turn took; null when Claude Code did not say, as for an interruption.</param>
/// <param name="Interrupted">True when the user stopped the turn.</param>
public sealed record TurnEnd(DateTimeOffset? Timestamp, TimeSpan? Duration, bool Interrupted) : ConversationEntry(Timestamp);
