namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A block of code or commands, drawn with a copy button.</summary>
/// <param name="Language">Its language as written after the opening fence; empty when none.</param>
/// <param name="Text">Its exact text.</param>
public sealed record CodeBlock(string Language, string Text) : MarkdownBlock;
