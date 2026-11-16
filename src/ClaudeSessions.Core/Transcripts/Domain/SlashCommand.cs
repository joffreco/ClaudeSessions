using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A slash command the user ran, such as /model or a skill.</summary>
/// <param name="Timestamp">When it was run.</param>
/// <param name="Name">The command, with its slash.</param>
/// <param name="Arguments">What followed the command; empty when nothing did.</param>
public sealed record SlashCommand(DateTimeOffset? Timestamp, string Name, string Arguments) : ConversationEntry(Timestamp);
