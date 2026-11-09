using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ClaudeSessions.Adapters.Consignes.Markdown;
using ClaudeSessions.Consignes.Interfaces;
using WpfList = System.Windows.Documents.List;
using WpfListItem = System.Windows.Documents.ListItem;

namespace ClaudeSessions.Consignes.Views;

/// <summary>
/// Draws a consigne's blocks as a FlowDocument in the Fluent theme: each code block with a « Copier » button,
/// each task with a box whose state the app keeps, and a bell after the box of a step whose tick wakes the session;
/// each result field with a box to paste in and a button that sends it to the session.
/// </summary>
internal sealed class ConsigneDocumentBuilder
{
    private const double BodySize = 14;
    private const double CodeSize = 13;
    private static readonly FontFamily CodeFont = new("Cascadia Mono, Consolas");

    private const string CallbackHint = "Cocher cette case prévient la session.";

    private readonly IConsigneActions _actions;

    private ConsigneDocumentBuilder(IConsigneActions actions)
    {
        _actions = actions;
    }

    /// <summary>Draws a body.</summary>
    /// <param name="document">The body, read.</param>
    /// <param name="actions">What ticking a task or sending a result does.</param>
    /// <returns>The document.</returns>
    public static FlowDocument Build(MarkdownDocument document, IConsigneActions actions)
    {
        ConsigneDocumentBuilder builder = new(actions);
        FlowDocument flow = new()
        {
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
            FontSize = BodySize,
            PagePadding = new Thickness(24, 16, 24, 24),
            TextAlignment = TextAlignment.Left,
        };
        flow.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");
        flow.Blocks.AddRange(builder.BlocksOf(document.Blocks));
        return flow;
    }

    private List<Block> BlocksOf(IEnumerable<MarkdownBlock> blocks)
    {
        List<Block> drawn = [];
        foreach (MarkdownBlock block in blocks)
        {
            drawn.Add(BlockOf(block));
        }
        return drawn;
    }

    private Block BlockOf(MarkdownBlock block)
    {
        return block switch
        {
            HeadingBlock heading => HeadingOf(heading),
            ParagraphBlock paragraph => ParagraphOf(paragraph.Inlines),
            CodeBlock code => CodeOf(code),
            ListBlock list => ListOf(list),
            QuoteBlock quote => QuoteOf(quote),
            ResultFieldBlock field => ResultFieldOf(field),
            _ => new BlockUIContainer(new Separator { Margin = new Thickness(0, 8, 0, 8) }),
        };
    }

    private static Paragraph HeadingOf(HeadingBlock heading)
    {
        Paragraph paragraph = ParagraphOf(heading.Inlines);
        paragraph.FontSize = heading.Level switch { 1 => 22, 2 => 18, _ => 15 };
        paragraph.FontWeight = FontWeights.SemiBold;
        paragraph.Margin = new Thickness(0, 16, 0, 6);
        return paragraph;
    }

    private static Paragraph ParagraphOf(IReadOnlyList<MarkdownInline> inlines)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0, 0, 0, 8) };
        foreach (MarkdownInline inline in inlines)
        {
            paragraph.Inlines.Add(InlineOf(inline));
        }
        return paragraph;
    }

    private static Inline InlineOf(MarkdownInline inline)
    {
        if (inline.Style.HasFlag(InlineStyle.LineBreak))
        {
            return new LineBreak();
        }
        Run run = new(inline.Text);
        if (inline.Style.HasFlag(InlineStyle.Bold))
        {
            run.FontWeight = FontWeights.SemiBold;
        }
        if (inline.Style.HasFlag(InlineStyle.Italic))
        {
            run.FontStyle = FontStyles.Italic;
        }
        if (inline.Style.HasFlag(InlineStyle.Code))
        {
            run.FontFamily = CodeFont;
            run.FontSize = CodeSize;
            run.SetResourceReference(TextElement.BackgroundProperty, "SubtleFillColorSecondaryBrush");
        }
        return inline.Style.HasFlag(InlineStyle.Link) && Uri.TryCreate(inline.Url, UriKind.Absolute, out Uri? address) ? LinkOf(run, address) : run;
    }

    private static Hyperlink LinkOf(Run run, Uri address)
    {
        Hyperlink link = new(run) { NavigateUri = address };
        link.RequestNavigate += (_, e) => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        return link;
    }

    private static BlockUIContainer CodeOf(CodeBlock code)
    {
        TextBox text = new()
        {
            Text = code.Text,
            IsReadOnly = true,
            FontFamily = CodeFont,
            FontSize = CodeSize,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(10, 8, 84, 8),
            BorderThickness = new Thickness(0),
        };
        text.SetResourceReference(Control.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
        Button copy = new()
        {
            Content = "Copier",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 4, 0),
            Padding = new Thickness(10, 2, 10, 2),
        };
        copy.Click += (_, _) => Copy(copy, code.Text);
        Grid grid = new();
        grid.Children.Add(text);
        grid.Children.Add(copy);
        Border border = new() { Child = grid, CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        border.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
        return new BlockUIContainer(border) { Margin = new Thickness(0, 4, 0, 12) };
    }

    private static void Copy(Button button, string text)
    {
        try
        {
            Clipboard.SetText(text);
            button.Content = "Copié ✓";
        }
        catch (COMException)
        {
            button.Content = "Presse-papiers occupé";
        }
    }

    private WpfList ListOf(ListBlock list)
    {
        WpfList drawn = new()
        {
            MarkerStyle = MarkerOf(list),
            StartIndex = list.Ordered ? Math.Max(list.Start, 1) : 1,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(24, 0, 0, 0),
        };
        foreach (ListItemBlock item in list.Items)
        {
            WpfListItem drawnItem = new();
            List<Block> blocks = BlocksOf(item.Blocks);
            if (item.TaskIndex is int index)
            {
                AddBox(blocks, item, index);
            }
            drawnItem.Blocks.AddRange(blocks);
            drawn.ListItems.Add(drawnItem);
        }
        return drawn;
    }

    private static TextMarkerStyle MarkerOf(ListBlock list)
    {
        if (list.Items.Count > 0 && list.Items.All(item => item.TaskIndex is not null))
        {
            return TextMarkerStyle.None;
        }
        return list.Ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc;
    }

    private void AddBox(List<Block> blocks, ListItemBlock item, int index)
    {
        CheckBox box = new()
        {
            IsChecked = item.TickedInSource || _actions.IsTicked(index),
            IsEnabled = !item.TickedInSource,
            Margin = new Thickness(0, 0, 6, -4),
            MinWidth = 0,
            Padding = new Thickness(0),
        };
        box.Click += (_, _) => Tick(box, item);
        List<Inline> head = [new InlineUIContainer(box) { BaselineAlignment = BaselineAlignment.Center }];
        if (item.Callback)
        {
            box.ToolTip = CallbackHint;
            head.Add(new Run("🔔 ") { ToolTip = CallbackHint });
        }
        Paragraph first = blocks.Count > 0 && blocks[0] is Paragraph paragraph ? paragraph : InsertParagraph(blocks);
        foreach (Inline inline in Enumerable.Reverse(head))
        {
            AddFirst(first, inline);
        }
    }

    private void Tick(CheckBox box, ListItemBlock item)
    {
        bool ticked = box.IsChecked == true;
        if (!_actions.Tick(item, ticked))
        {
            box.IsChecked = !ticked;
        }
    }

    private static Paragraph InsertParagraph(List<Block> blocks)
    {
        Paragraph paragraph = new();
        blocks.Insert(0, paragraph);
        return paragraph;
    }

    private static void AddFirst(Paragraph paragraph, Inline inline)
    {
        if (paragraph.Inlines.FirstInline is Inline first)
        {
            paragraph.Inlines.InsertBefore(first, inline);
        }
        else
        {
            paragraph.Inlines.Add(inline);
        }
    }

    private BlockUIContainer ResultFieldOf(ResultFieldBlock field)
    {
        TextBlock label = new() { Text = $"Résultat à coller : {field.Name}", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
        TextBox text = new()
        {
            Text = _actions.ResultOf(field.Name),
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = CodeFont,
            FontSize = CodeSize,
            TextWrapping = TextWrapping.NoWrap,
            MinHeight = 90,
            MaxHeight = 320,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        Button send = new() { Content = "Envoyer à la session", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0), Padding = new Thickness(12, 4, 12, 4) };
        send.IsEnabled = text.Text.Trim().Length > 0;
        text.TextChanged += (_, _) =>
        {
            send.IsEnabled = text.Text.Trim().Length > 0;
            send.Content = "Envoyer à la session";
        };
        send.Click += (_, _) =>
        {
            if (_actions.SendResult(field.Name, text.Text))
            {
                send.Content = $"Envoyé ✓ {DateTime.Now:HH:mm}";
            }
        };
        StackPanel panel = new();
        panel.Children.Add(label);
        panel.Children.Add(text);
        panel.Children.Add(send);
        Border border = new() { Child = panel, Padding = new Thickness(10), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        border.SetResourceReference(Border.BorderBrushProperty, "AccentFillColorDefaultBrush");
        return new BlockUIContainer(border) { Margin = new Thickness(0, 4, 0, 12) };
    }

    private Section QuoteOf(QuoteBlock quote)
    {
        Section section = new()
        {
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 0, 0, 0),
            Margin = new Thickness(0, 0, 0, 8),
        };
        section.SetResourceReference(Block.BorderBrushProperty, "AccentFillColorDefaultBrush");
        section.Blocks.AddRange(BlocksOf(quote.Blocks));
        return section;
    }
}
