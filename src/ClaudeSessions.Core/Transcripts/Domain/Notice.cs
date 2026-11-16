using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A message Claude Code added to the conversation, such as the end of a background task.</summary>
/// <param name="Timestamp">When it was added.</param>
/// <param name="Text">The message.</param>
public sealed record Notice(DateTimeOffset? Timestamp, string Text) : ConversationEntry(Timestamp);
