using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A quotation.</summary>
/// <param name="Blocks">What it holds.</param>
public sealed record QuoteBlock(IReadOnlyList<MarkdownBlock> Blocks) : MarkdownBlock;
