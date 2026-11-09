using System.Linq;
using ClaudeSessions.Adapters.Consignes.Markdown;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Adapters;

/// <summary>Reading a consigne's Markdown into the blocks the window draws.</summary>
public sealed class MarkdownReaderTests
{
    /// <summary>A fenced block keeps its exact text and language, ready to copy.</summary>
    [Fact]
    public void CodeKeepsExactText()
    {
        MarkdownDocument document = MarkdownReader.Read("```bash\ndocker exec -u node app \\\n  npm test\n```");

        Assert.Equal(new CodeBlock("bash", "docker exec -u node app \\\n  npm test"), Assert.Single(document.Blocks));
    }

    /// <summary>Tasks are numbered in document order, nested ones included, and keep their source tick.</summary>
    [Fact]
    public void TasksAreNumberedInOrder()
    {
        MarkdownDocument document = MarkdownReader.Read("- [ ] un\n- [x] deux\n  - [ ] trois\n- pas une tâche");

        ListBlock list = Assert.IsType<ListBlock>(Assert.Single(document.Blocks));
        Assert.Equal(3, document.TaskCount);
        Assert.Equal((0, false), (list.Items[0].TaskIndex, list.Items[0].TickedInSource));
        Assert.Equal((1, true), (list.Items[1].TaskIndex, list.Items[1].TickedInSource));
        ListBlock nested = Assert.IsType<ListBlock>(list.Items[1].Blocks[1]);
        Assert.Equal(2, nested.Items[0].TaskIndex);
        Assert.Null(list.Items[2].TaskIndex);
    }

    /// <summary>A task's text does not repeat its box.</summary>
    [Fact]
    public void TaskTextHasNoBox()
    {
        ListBlock list = Assert.IsType<ListBlock>(Assert.Single(MarkdownReader.Read("- [ ] lancer").Blocks));

        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(list.Items[0].Blocks));
        Assert.Equal("lancer", string.Concat(paragraph.Inlines.Select(inline => inline.Text)).Trim());
    }

    /// <summary>The callback mark on a task flags it, gives its plain text, and is not shown, wherever it sits in the text.</summary>
    [Theory]
    [InlineData("- [ ] Lancer **la** migration <!-- callback -->")]
    [InlineData("- [ ] <!--callback--> Lancer **la** migration")]
    public void CallbackMarkFlagsTheTask(string markdown)
    {
        ListItemBlock item = Assert.Single(Assert.IsType<ListBlock>(Assert.Single(MarkdownReader.Read(markdown).Blocks)).Items);

        Assert.True(item.Callback);
        Assert.Equal("Lancer la migration", item.Text);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(item.Blocks));
        Assert.DoesNotContain(paragraph.Inlines, inline => inline.Text.Contains("callback", System.StringComparison.Ordinal));
    }

    /// <summary>A task without the mark, or a plain item with it, does not wake the session; another HTML comment stays visible.</summary>
    [Fact]
    public void OnlyMarkedTasksCallBack()
    {
        ListBlock list = Assert.IsType<ListBlock>(Assert.Single(MarkdownReader.Read("- [ ] un <!-- note -->\n- deux <!-- callback -->").Blocks));

        Assert.Equal([false, false], list.Items.Select(item => item.Callback));
        ParagraphBlock first = Assert.IsType<ParagraphBlock>(Assert.Single(list.Items[0].Blocks));
        Assert.Contains(new MarkdownInline("<!-- note -->", InlineStyle.Code), first.Inlines);
    }

    /// <summary>A named result mark on its own line becomes a field with that name.</summary>
    [Fact]
    public void NamedResultMarkBecomesAField()
    {
        MarkdownDocument document = MarkdownReader.Read("<!-- result: comptes -->\n");

        Assert.Equal(new ResultFieldBlock("comptes"), Assert.Single(document.Blocks));
    }

    /// <summary>An unnamed result mark inside a step becomes a field numbered by its place among the result marks.</summary>
    [Fact]
    public void UnnamedResultMarkInsideAStepIsNumbered()
    {
        MarkdownDocument document = MarkdownReader.Read("<!-- result: comptes -->\n\n- [ ] Lancer la requête\n\n  <!-- result -->\n");

        ListItemBlock step = Assert.Single(Assert.IsType<ListBlock>(document.Blocks[1]).Items);
        Assert.Equal(new ResultFieldBlock("resultat-2"), step.Blocks[1]);
    }

    /// <summary>Another HTML comment on its own line stays visible as its source.</summary>
    [Fact]
    public void OtherHtmlBlockStaysSource()
    {
        Assert.Equal(new CodeBlock("", "<!-- note -->"), Assert.Single(MarkdownReader.Read("<!-- note -->").Blocks));
    }

    /// <summary>A numbered list keeps its first number.</summary>
    [Fact]
    public void OrderedListKeepsStart()
    {
        ListBlock list = Assert.IsType<ListBlock>(Assert.Single(MarkdownReader.Read("3. trois\n4. quatre").Blocks));

        Assert.Equal((true, 3, 2), (list.Ordered, list.Start, list.Items.Count));
    }

    /// <summary>Emphasis, inline code and links carry their style.</summary>
    [Fact]
    public void InlineStyles()
    {
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(MarkdownReader.Read("a **gras** *ital* `code` [lien](https://x.y)").Blocks));

        Assert.Contains(new MarkdownInline("gras", InlineStyle.Bold), paragraph.Inlines);
        Assert.Contains(new MarkdownInline("ital", InlineStyle.Italic), paragraph.Inlines);
        Assert.Contains(new MarkdownInline("code", InlineStyle.Code), paragraph.Inlines);
        Assert.Contains(new MarkdownInline("lien", InlineStyle.Link, "https://x.y"), paragraph.Inlines);
    }

    /// <summary>Headings, quotations and rules are kept apart.</summary>
    [Fact]
    public void HeadingQuoteRule()
    {
        MarkdownDocument document = MarkdownReader.Read("## Titre\n\n> attention\n\n---\n");

        Assert.Equal(2, Assert.IsType<HeadingBlock>(document.Blocks[0]).Level);
        Assert.IsType<ParagraphBlock>(Assert.Single(Assert.IsType<QuoteBlock>(document.Blocks[1]).Blocks));
        Assert.IsType<RuleBlock>(document.Blocks[2]);
    }

    /// <summary>A table is shown as its source text, so nothing is lost.</summary>
    [Fact]
    public void TableFallsBackToSource()
    {
        MarkdownDocument document = MarkdownReader.Read("| a | b |\n| - | - |\n| 1 | 2 |");

        Assert.Equal(new CodeBlock("", "| a | b |\n| - | - |\n| 1 | 2 |"), Assert.Single(document.Blocks));
    }
}
