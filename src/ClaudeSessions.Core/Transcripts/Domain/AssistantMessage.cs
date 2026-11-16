using System;
using System.Collections.Generic;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>One reply of the model: its text, its reasoning and the tools it called, in order.</summary>
/// <param name="Timestamp">When the reply started.</param>
/// <param name="Blocks">The parts of the reply.</param>
/// <param name="OutputTokens">How many tokens the model wrote for the reply, as far as the transcript tells.</param>
/// <param name="EndsTurn">True when the model stopped because it was done, not to call a tool.</param>
public sealed record AssistantMessage(DateTimeOffset? Timestamp, IReadOnlyList<AssistantBlock> Blocks, int OutputTokens = 0, bool EndsTurn = false) : ConversationEntry(Timestamp);
