using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A shell command the user ran directly, with the ! prefix.</summary>
/// <param name="Timestamp">When it was run.</param>
/// <param name="Command">The command, without the prefix.</param>
public sealed record ShellCommand(DateTimeOffset? Timestamp, string Command) : ConversationEntry(Timestamp);
