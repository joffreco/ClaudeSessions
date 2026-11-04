using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.TaskLists;
using Md = Markdig.Syntax;
using MdInlines = Markdig.Syntax.Inlines;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>
/// Reads a consigne's Markdown with Markdig into the blocks the window draws. Only what a session writes is
/// kept apart: headings, paragraphs, lists and tasks, code, quotations, rules; anything else (a table, raw
/// HTML) is shown as its source text in a code block, so nothing is lost. The <c>&lt;!-- callback --&gt;</c>
/// mark is never shown: on a task, it asks that ticking the task wakes the session.
/// </summary>
public sealed partial class MarkdownReader
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseTaskLists().UsePipeTables().Build();

    private readonly string _source;
    private int _tasks;

    private MarkdownReader(string source)
    {
        _source = source;
    }

    /// <summary>Reads a body.</summary>
    /// <param name="markdown">The Markdown.</param>
    /// <returns>Its blocks and task count.</returns>
    public static MarkdownDocument Read(string markdown)
    {
        MarkdownReader reader = new(markdown);
        IReadOnlyList<MarkdownBlock> blocks = reader.BlocksOf(Markdig.Markdown.Parse(markdown, Pipeline));
        return new MarkdownDocument(blocks, reader._tasks);
    }

    private List<MarkdownBlock> BlocksOf(Md.ContainerBlock container)
    {
        List<MarkdownBlock> blocks = [];
        foreach (Md.Block block in container)
        {
            if (BlockOf(block) is MarkdownBlock converted)
            {
                blocks.Add(converted);
            }
        }
        return blocks;
    }

    private MarkdownBlock? BlockOf(Md.Block block)
    {
        return block switch
        {
            Md.HeadingBlock heading => new HeadingBlock(heading.Level, InlinesOf(heading.Inline)),
            Md.ParagraphBlock paragraph => new ParagraphBlock(InlinesOf(paragraph.Inline)),
            Md.FencedCodeBlock fenced => new CodeBlock(fenced.Info ?? "", fenced.Lines.ToString()),
            Md.CodeBlock code => new CodeBlock("", code.Lines.ToString()),
            Md.QuoteBlock quote => new QuoteBlock(BlocksOf(quote)),
            Md.ListBlock list => ListOf(list),
            Md.ThematicBreakBlock => new RuleBlock(),
            Md.LinkReferenceDefinitionGroup => null,
            Md.BlankLineBlock => null,
            _ => new CodeBlock("", SourceOf(block)),
        };
    }

    private ListBlock ListOf(Md.ListBlock list)
    {
        int start = int.TryParse(list.OrderedStart, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : 1;
        List<ListItemBlock> items = [];
        foreach (Md.ListItemBlock item in list.OfType<Md.ListItemBlock>())
        {
            TaskList? task = FirstTaskOf(item);
            int? index = task is null ? null : _tasks++;
            MdInlines.ContainerInline? first = FirstInlinesOf(item);
            bool callback = task is not null && first is not null && first.FindDescendants<MdInlines.HtmlInline>().Any(IsCallbackMark);
            items.Add(new ListItemBlock(index, task?.Checked ?? false, BlocksOf(item), callback, PlainTextOf(first)));
        }
        return new ListBlock(list.IsOrdered, start, items);
    }

    private static TaskList? FirstTaskOf(Md.ListItemBlock item)
    {
        return FirstInlinesOf(item)?.FirstChild as TaskList;
    }

    private static MdInlines.ContainerInline? FirstInlinesOf(Md.ListItemBlock item)
    {
        return item.FirstOrDefault() is Md.ParagraphBlock { Inline: { } inline } ? inline : null;
    }

    private static string PlainTextOf(MdInlines.ContainerInline? container)
    {
        return string.Concat(InlinesOf(container).Select(inline => inline.Style.HasFlag(InlineStyle.LineBreak) ? " " : inline.Text)).Trim();
    }

    private static bool IsCallbackMark(MdInlines.HtmlInline html)
    {
        return CallbackMark().IsMatch(html.Tag);
    }

    [GeneratedRegex(@"^<!--\s*callback\s*-->$")]
    private static partial Regex CallbackMark();

    private static List<MarkdownInline> InlinesOf(MdInlines.ContainerInline? container)
    {
        List<MarkdownInline> inlines = [];
        if (container is not null)
        {
            AddInlines(inlines, container, InlineStyle.None, null);
        }
        return inlines;
    }

    private static void AddInlines(List<MarkdownInline> inlines, MdInlines.ContainerInline container, InlineStyle style, string? url)
    {
        foreach (MdInlines.Inline inline in container)
        {
            AddInline(inlines, inline, style, url);
        }
    }

    private static void AddInline(List<MarkdownInline> inlines, MdInlines.Inline inline, InlineStyle style, string? url)
    {
        switch (inline)
        {
            case TaskList:
                return;
            case MdInlines.LiteralInline literal:
                inlines.Add(new MarkdownInline(literal.Content.ToString(), style, url));
                return;
            case MdInlines.CodeInline code:
                inlines.Add(new MarkdownInline(code.Content, style | InlineStyle.Code, url));
                return;
            case MdInlines.LineBreakInline lineBreak:
                inlines.Add(lineBreak.IsHard ? new MarkdownInline("", InlineStyle.LineBreak) : new MarkdownInline(" ", style, url));
                return;
            case MdInlines.AutolinkInline autolink:
                inlines.Add(new MarkdownInline(autolink.Url, style | InlineStyle.Link, autolink.Url));
                return;
            case MdInlines.HtmlEntityInline entity:
                inlines.Add(new MarkdownInline(entity.Transcoded.ToString(), style, url));
                return;
            case MdInlines.HtmlInline html when IsCallbackMark(html):
                return;
            case MdInlines.HtmlInline html:
                inlines.Add(new MarkdownInline(html.Tag, style | InlineStyle.Code, url));
                return;
            case MdInlines.EmphasisInline emphasis:
                AddInlines(inlines, emphasis, style | (emphasis.DelimiterCount >= 2 ? InlineStyle.Bold : InlineStyle.Italic), url);
                return;
            case MdInlines.LinkInline link:
                AddInlines(inlines, link, style | InlineStyle.Link, link.Url);
                return;
            case MdInlines.ContainerInline other:
                AddInlines(inlines, other, style, url);
                return;
            default:
                inlines.Add(new MarkdownInline(inline.ToString() ?? "", style, url));
                return;
        }
    }

    private string SourceOf(Md.Block block)
    {
        int start = System.Math.Clamp(block.Span.Start, 0, _source.Length);
        int end = System.Math.Clamp(block.Span.End + 1, start, _source.Length);
        return _source[start..end].TrimEnd('\n', '\r');
    }
}
