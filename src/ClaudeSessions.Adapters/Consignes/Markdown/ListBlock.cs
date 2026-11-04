using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A bulleted or numbered list.</summary>
/// <param name="Ordered">True when numbered.</param>
/// <param name="Start">The first number of a numbered list.</param>
/// <param name="Items">Its items.</param>
public sealed record ListBlock(bool Ordered, int Start, IReadOnlyList<ListItemBlock> Items) : MarkdownBlock;
