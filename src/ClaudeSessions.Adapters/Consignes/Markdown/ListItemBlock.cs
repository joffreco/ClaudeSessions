using System.Collections.Generic;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>An item of a list.</summary>
/// <param name="TaskIndex">The item's position among the document's tasks when it is a task (<c>- [ ]</c>); else null.</param>
/// <param name="TickedInSource">True when the Markdown already ticks the task (<c>- [x]</c>).</param>
/// <param name="Blocks">What it holds.</param>
/// <param name="Callback">True when the task carries the <c>&lt;!-- callback --&gt;</c> mark: ticking it wakes the session.</param>
/// <param name="Text">The plain text of the item's first paragraph, without its box or mark.</param>
public sealed record ListItemBlock(int? TaskIndex, bool TickedInSource, IReadOnlyList<MarkdownBlock> Blocks, bool Callback = false, string Text = "");
