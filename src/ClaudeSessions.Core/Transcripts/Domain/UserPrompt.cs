using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A message the user typed.</summary>
/// <param name="Timestamp">When it was sent.</param>
/// <param name="Text">What the user wrote; an attached image appears as [image].</param>
public sealed record UserPrompt(DateTimeOffset? Timestamp, string Text) : ConversationEntry(Timestamp);
