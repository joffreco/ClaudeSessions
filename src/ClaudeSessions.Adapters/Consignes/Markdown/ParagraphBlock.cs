using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A paragraph.</summary>
/// <param name="Inlines">Its text.</param>
public sealed record ParagraphBlock(IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;
