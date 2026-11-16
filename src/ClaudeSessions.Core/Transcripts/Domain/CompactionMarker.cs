using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>The point where the conversation was compacted: the model only remembers a summary of what comes before.</summary>
/// <param name="Timestamp">When it was compacted.</param>
public sealed record CompactionMarker(DateTimeOffset? Timestamp) : ConversationEntry(Timestamp);
