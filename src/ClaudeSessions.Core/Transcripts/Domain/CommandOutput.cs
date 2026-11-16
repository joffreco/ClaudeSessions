using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>What a slash command or a ! shell command printed.</summary>
/// <param name="Timestamp">When it was printed.</param>
/// <param name="Text">The output.</param>
public sealed record CommandOutput(DateTimeOffset? Timestamp, string Text) : ConversationEntry(Timestamp);
