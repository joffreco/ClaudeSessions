namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>Text the model wrote to the user, in Markdown.</summary>
/// <param name="Text">The text.</param>
public sealed record AssistantText(string Text) : AssistantBlock;
