using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>One step of a conversation: a prompt, a reply, a command or a notice.</summary>
/// <param name="Timestamp">When it was written; null when the transcript did not say.</param>
public abstract record ConversationEntry(DateTimeOffset? Timestamp);
