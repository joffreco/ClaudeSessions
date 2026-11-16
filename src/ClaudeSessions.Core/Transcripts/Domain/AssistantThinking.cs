namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A summary of the model's reasoning, when Claude Code kept one.</summary>
/// <param name="Text">The summary.</param>
public sealed record AssistantThinking(string Text) : AssistantBlock;
