namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A run of text with one style.</summary>
/// <param name="Text">The text.</param>
/// <param name="Style">How it is drawn.</param>
/// <param name="Url">The link's address when <see cref="InlineStyle.Link"/> is set; else null.</param>
public sealed record MarkdownInline(string Text, InlineStyle Style, string? Url = null);
