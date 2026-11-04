using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>A consigne's body, read into blocks.</summary>
/// <param name="Blocks">Its blocks, in order.</param>
/// <param name="TaskCount">How many tasks it holds.</param>
public sealed record MarkdownDocument(IReadOnlyList<MarkdownBlock> Blocks, int TaskCount);
