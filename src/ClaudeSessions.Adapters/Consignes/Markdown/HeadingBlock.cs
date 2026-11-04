using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A heading.</summary>
/// <param name="Level">Its level, from 1.</param>
/// <param name="Inlines">Its text.</param>
public sealed record HeadingBlock(int Level, IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;
