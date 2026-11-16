using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ClaudeSessions.Adapters.Consignes.Markdown;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Ui;

namespace ClaudeSessions.Conversations.Views;

/// <summary>
/// Draws a conversation as a FlowDocument in the Fluent theme, with the Markdown rendering of Consignes:
/// the user's messages in shaded blocks, the replies in Markdown with a « Copier » button on each code block,
/// the reasoning and the tool calls folded, command output as code.
/// </summary>
internal static class ConversationDocumentBuilder
{
    /// <summary>Most recent entries drawn; older ones stay in the session's terminal.</summary>
    public const int MaxEntries = 300;

    /// <summary>Longest tool result drawn inside a folded call.</summary>
    private const int MaxOutputLength = 4_000;

    /// <summary>Longest one-line summary of a tool call's arguments.</summary>
    private const int MaxSummaryLength = 160;

    private static readonly string[] SummaryKeys = ["command", "file_path", "path", "pattern", "url", "query", "description", "prompt"];
    private static readonly IMarkdownActions ReadOnly = new ReadOnlyActions();

    /// <summary>Creates an empty conversation document in the body font and colors.</summary>
    /// <returns>The document.</returns>
    public static FlowDocument NewDocument()
    {
        return MarkdownDocumentBuilder.NewDocument();
    }

    /// <summary>Width of the column the conversation is read in, centered in the window as in the Claude app.</summary>
    public const double ColumnWidth = 780;

    /// <summary>Widest a message of the user grows before its text wraps.</summary>
    public const double BubbleMaxWidth = 600;

    /// <summary>Tag of the frame of a user's message, by which the window finds the last one to pin.</summary>
    public const string BubbleTag = "user-message";

    /// <summary>Margins that center a column of <see cref="ColumnWidth"/> in a viewer of the given width.</summary>
    /// <param name="viewerWidth">Width of the viewer, its scroll bar included.</param>
    /// <returns>The page padding of the document.</returns>
    public static Thickness CenteredPadding(double viewerWidth)
    {
        const double ScrollBar = 18;
        double side = Math.Max(24, (viewerWidth - ScrollBar - ColumnWidth) / 2);
        return new Thickness(side, 16, side, 24);
    }

    /// <summary>Draws a line of secondary text, such as a notice that the conversation is empty.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The block.</returns>
    public static Paragraph NoticeLine(string text)
    {
        return Secondary(text);
    }

    /// <summary>
    /// Tells whether an entry would draw the same as when its signature was taken: the window redraws only
    /// the entries whose signature changed, which keeps the scroll position and what the user unfolded.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="final">True when the entry is the final response of its turn, see <see cref="IsFinal"/>.</param>
    /// <returns>Its signature.</returns>
    public static string Signature(ConversationEntry entry, bool final)
    {
        string content = entry switch
        {
            UserPrompt prompt => prompt.Text,
            AssistantMessage reply => string.Join("|", reply.Blocks.Select(block => block switch
            {
                AssistantText text => "t" + text.Text.Length,
                AssistantThinking thinking => "r" + thinking.Text.Length,
                ToolCall call => "c" + call.Id + (call.Result is null ? "" : "+"),
                _ => "?",
            })),
            SlashCommand command => command.Name + " " + command.Arguments,
            ShellCommand shell => shell.Command,
            CommandOutput output => output.Text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Notice notice => notice.Text,
            TurnEnd end => end.Interrupted + "|" + end.Duration?.Ticks,
            _ => "",
        };
        return $"{entry.GetType().Name}|{entry.Timestamp?.UtcTicks}|{(final ? "F" : "")}|{content}";
    }

    /// <summary>
    /// Tells whether an entry is the final response of its turn: a reply that ends with text, after its last
    /// tool call, and that no other reply follows. The text written between tool calls are progress updates.
    /// While the session works, its last reply counts as final until another one follows it.
    /// </summary>
    /// <param name="entries">The conversation's entries.</param>
    /// <param name="index">The entry's position.</param>
    /// <returns>True for a final response.</returns>
    public static bool IsFinal(IReadOnlyList<ConversationEntry> entries, int index)
    {
        if (entries[index] is not AssistantMessage { Blocks: [.., AssistantText] })
        {
            return false;
        }
        for (int next = index + 1; next < entries.Count; next++)
        {
            switch (entries[next])
            {
                case AssistantMessage:
                    return false;
                case UserPrompt or SlashCommand or ShellCommand or CompactionMarker:
                    return true;
            }
        }
        return true;
    }

    /// <summary>Draws one entry.</summary>
    /// <param name="entry">The entry.</param>
    /// <param name="final">True when the entry is the final response of its turn, see <see cref="IsFinal"/>.</param>
    /// <param name="windowsPathOf">Turns the WSL path of an image named in a message into the path Windows reads.</param>
    /// <returns>Its blocks, in order.</returns>
    public static List<Block> Draw(ConversationEntry entry, bool final, Func<string, string> windowsPathOf)
    {
        return entry switch
        {
            UserPrompt prompt => Prompt(prompt.Text, windowsPathOf),
            AssistantMessage reply => Reply(reply, final),
            SlashCommand command => [Secondary((command.Name + " " + command.Arguments).Trim(), code: true)],
            ShellCommand shell => [Secondary("! " + shell.Command, code: true)],
            CommandOutput output => [MarkdownDocumentBuilder.CodeOf(output.Text)],
            CompactionMarker => [Secondary("— Conversation compactée —", centered: true)],
            Notice notice => [Secondary(notice.Text)],
            TurnEnd { Interrupted: true } => [Small("Interrompu", TextAlignment.Left)],
            TurnEnd { Duration: TimeSpan duration } => [Small("Terminé en " + ActivityText.Duration(duration), TextAlignment.Left)],
            TurnEnd => [],
            _ => [],
        };
    }

    // When an entry was written, as the transcript says: the hour today, the day and hour before.
    private static Paragraph Time(DateTimeOffset? timestamp, TextAlignment alignment)
    {
        string text = "";
        if (timestamp is DateTimeOffset moment)
        {
            DateTime local = moment.LocalDateTime;
            text = local.Date == DateTime.Today
                ? local.ToString("HH:mm", System.Globalization.CultureInfo.CurrentCulture)
                : local.ToString("d MMM HH:mm", System.Globalization.CultureInfo.CurrentCulture);
        }
        return Small(text, alignment);
    }

    // A line of small text in tertiary color, such as a time or the duration of a turn.
    private static Paragraph Small(string text, TextAlignment alignment)
    {
        Paragraph paragraph = new(new Run(text)) { Margin = new Thickness(0, 2, 0, 2), FontSize = 11, TextAlignment = alignment };
        paragraph.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorTertiaryBrush");
        return paragraph;
    }

    /// <summary>
    /// Draws a message of the user as a rounded bubble against the right edge of the column, as wide as its
    /// text up to <see cref="BubbleMaxWidth"/>, its text selectable and its line breaks kept.
    /// </summary>
    /// <param name="text">The message.</param>
    /// <param name="pending">True for a message sent but not yet recorded, drawn in secondary color.</param>
    /// <returns>The block.</returns>
    public static BlockUIContainer Bubble(string text, bool pending = false)
    {
        TextBox content = Plain(text, code: false);
        content.SetResourceReference(Control.ForegroundProperty, pending ? "TextFillColorSecondaryBrush" : "TextFillColorPrimaryBrush");
        Border bubble = new()
        {
            Child = content,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = BubbleMaxWidth,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16, 10, 16, 10),
            Tag = BubbleTag,
        };
        bubble.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorSecondaryBrush");
        return new BlockUIContainer(bubble) { Margin = new Thickness(0, 16, 0, 10) };
    }

    // The user's message as a bubble. A line that is only the path of an image, as Conversations adds for a
    // pasted image, shows the image under the message.
    private static List<Block> Prompt(string text, Func<string, string> windowsPathOf)
    {
        List<string> lines = [];
        List<string> images = [];
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (IsImagePath(line))
            {
                images.Add(line.Trim());
                continue;
            }
            lines.Add(line);
        }
        BlockUIContainer bubble = Bubble(string.Join("\n", lines).Trim('\n'));
        if (images.Count > 0)
        {
            bubble.Margin = new Thickness(0, 16, 0, 4);
        }
        List<Block> blocks = [bubble];
        foreach (string image in images)
        {
            blocks.Add(ImageOf(image, windowsPathOf));
        }
        return blocks;
    }

    private static bool IsImagePath(string line)
    {
        string trimmed = line.Trim();
        return trimmed.StartsWith('/') && !trimmed.Contains(' ', StringComparison.Ordinal)
            && (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
    }

    // An image under the user's message, at most 320 pixels high; its path when it cannot be read.
    private static BlockUIContainer ImageOf(string wslPath, Func<string, string> windowsPathOf)
    {
        UIElement content;
        try
        {
            System.Windows.Media.Imaging.BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(windowsPathOf(wslPath));
            bitmap.DecodePixelHeight = 640;
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            content = new System.Windows.Controls.Image { Source = bitmap, MaxHeight = 320, HorizontalAlignment = HorizontalAlignment.Right, ToolTip = wslPath };
        }
        catch (Exception exception) when (exception is System.IO.IOException or NotSupportedException or UriFormatException)
        {
            content = new TextBlock { Text = wslPath, HorizontalAlignment = HorizontalAlignment.Right };
        }
        return new BlockUIContainer(content) { Margin = new Thickness(0, 0, 0, 10) };
    }

    // A reply: its text written between tool calls is a progress update, drawn discreet; in the final response,
    // the text after the last tool call is set apart, with the time the transcript gives it.
    private static List<Block> Reply(AssistantMessage reply, bool final)
    {
        int lastCall = -1;
        for (int index = 0; index < reply.Blocks.Count; index++)
        {
            if (reply.Blocks[index] is ToolCall)
            {
                lastCall = index;
            }
        }
        List<Block> blocks = [];
        List<ToolCall> calls = [];
        Section? answer = null;
        for (int index = 0; index < reply.Blocks.Count; index++)
        {
            AssistantBlock block = reply.Blocks[index];
            if (block is ToolCall call)
            {
                calls.Add(call);
                continue;
            }
            FlushCalls(blocks, calls);
            if (block is AssistantThinking thinking)
            {
                blocks.Add(Thinking(thinking.Text));
            }
            else if (block is AssistantText text && final && index > lastCall)
            {
                answer ??= FinalResponse(reply.Timestamp, blocks);
                answer.Blocks.AddRange(MarkdownDocumentBuilder.BlocksOf(MarkdownReader.Read(text.Text), ReadOnly));
            }
            else if (block is AssistantText progress)
            {
                blocks.Add(ProgressUpdate(progress.Text));
            }
        }
        FlushCalls(blocks, calls);
        return blocks;
    }

    // The final response: the time, then the answer in a card with an accent rule on the left.
    private static Section FinalResponse(DateTimeOffset? timestamp, List<Block> blocks)
    {
        Section answer = new()
        {
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(16, 10, 16, 6),
            Margin = new Thickness(0, 2, 0, 14),
        };
        answer.SetResourceReference(Block.BorderBrushProperty, "AccentFillColorDefaultBrush");
        answer.SetResourceReference(Block.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
        blocks.Add(Time(timestamp, TextAlignment.Left));
        blocks.Add(answer);
        return answer;
    }

    // A progress update, written between two tool calls: smaller and in secondary color.
    private static Section ProgressUpdate(string text)
    {
        Section section = new() { Margin = new Thickness(0, 2, 0, 2), FontSize = MarkdownDocumentBuilder.CodeSize };
        section.Blocks.AddRange(MarkdownDocumentBuilder.BlocksOf(MarkdownReader.Read(text), ReadOnly));
        section.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
        return section;
    }

    // The reasoning, shown in full: a label, then its Markdown in secondary color behind a rule on the left.
    private static Section Thinking(string text)
    {
        Paragraph label = new(new Run("Réflexion")) { Margin = new Thickness(0, 0, 0, 4), FontWeight = FontWeights.SemiBold, FontSize = MarkdownDocumentBuilder.CodeSize };
        Section section = new(label)
        {
            BorderThickness = new Thickness(2, 0, 0, 0),
            Padding = new Thickness(12, 2, 0, 2),
            Margin = new Thickness(0, 4, 0, 10),
        };
        section.Blocks.AddRange(MarkdownDocumentBuilder.BlocksOf(MarkdownReader.Read(text), ReadOnly));
        section.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
        section.SetResourceReference(Block.BorderBrushProperty, "TextFillColorTertiaryBrush");
        return section;
    }

    // Consecutive tool calls fold into one line, as in the Claude app.
    private static void FlushCalls(List<Block> blocks, List<ToolCall> calls)
    {
        if (calls.Count == 0)
        {
            return;
        }
        StackPanel details = new();
        foreach (ToolCall call in calls)
        {
            details.Children.Add(Call(call));
        }
        int running = calls.Count(call => call.Result is null);
        int failed = calls.Count(call => call.Result?.IsError == true);
        string header = (calls.Count == 1 ? "1 outil" : $"{calls.Count} outils")
            + " : " + string.Join(", ", calls.Select(call => call.Name).Distinct())
            + (running > 0 ? " · en cours" : "")
            + (failed > 0 ? $" · {failed} en échec" : "");
        blocks.Add(Folded(header, details));
        calls.Clear();
    }

    private static StackPanel Call(ToolCall call)
    {
        StackPanel panel = new() { Margin = new Thickness(0, 4, 0, 10) };
        TextBlock line = new() { Text = call.Name + "  " + Summary(call.Input), FontFamily = MarkdownDocumentBuilder.CodeFont, FontSize = MarkdownDocumentBuilder.CodeSize, TextWrapping = TextWrapping.Wrap };
        panel.Children.Add(line);
        if (call.Result is { IsError: false, Patch: { Count: > 0 } patch })
        {
            panel.Children.Add(Diff(patch));
        }
        else if (call.Result is ToolResult result)
        {
            TextBox output = Plain(Cut(result.Text, MaxOutputLength), code: true);
            output.Margin = new Thickness(0, 4, 0, 0);
            output.Padding = new Thickness(10, 6, 10, 6);
            output.SetResourceReference(Control.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            if (result.IsError)
            {
                output.SetResourceReference(Control.ForegroundProperty, "SystemFillColorCriticalBrush");
            }
            panel.Children.Add(output);
        }
        else
        {
            TextBlock running = new() { Text = "en cours…", Margin = new Thickness(0, 4, 0, 0) };
            running.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            panel.Children.Add(running);
        }
        return panel;
    }

    // A file change as the terminal shows it: the line number and its sign in red for a removed line and in
    // green for an added one, the removed text faint, the hunks apart, and a count of each on top.
    private static Border Diff(IReadOnlyList<DiffHunk> patch)
    {
        TextBlock text = new() { FontFamily = MarkdownDocumentBuilder.CodeFont, FontSize = MarkdownDocumentBuilder.CodeSize, TextWrapping = TextWrapping.Wrap };
        int added = patch.Sum(hunk => hunk.Lines.Count(line => line.StartsWith('+')));
        int removed = patch.Sum(hunk => hunk.Lines.Count(line => line.StartsWith('-')));
        text.Inlines.Add(Colored($"+{added}", "SystemFillColorSuccessBrush"));
        text.Inlines.Add(new Run("  "));
        text.Inlines.Add(Colored($"−{removed}", "SystemFillColorCriticalBrush"));
        int width = patch.Max(hunk => Math.Max(hunk.OldStart, hunk.NewStart) + hunk.Lines.Count).ToString(CultureInfo.InvariantCulture).Length;
        foreach (DiffHunk hunk in patch)
        {
            text.Inlines.Add(new LineBreak());
            int oldLine = hunk.OldStart;
            int newLine = hunk.NewStart;
            foreach (string line in hunk.Lines)
            {
                char sign = line.Length > 0 ? line[0] : ' ';
                string body = line.Length > 0 ? line[1..] : "";
                int number = sign == '-' ? oldLine++ : newLine++;
                if (sign == ' ')
                {
                    oldLine++;
                }
                string margin = number.ToString(CultureInfo.InvariantCulture).PadLeft(width) + (sign == ' ' ? "   " : $" {sign} ");
                text.Inlines.Add(new LineBreak());
                switch (sign)
                {
                    case '-':
                        text.Inlines.Add(Colored(margin, "SystemFillColorCriticalBrush"));
                        text.Inlines.Add(Colored(body, "TextFillColorTertiaryBrush"));
                        break;
                    case '+':
                        text.Inlines.Add(Colored(margin, "SystemFillColorSuccessBrush"));
                        text.Inlines.Add(new Run(body));
                        break;
                    default:
                        text.Inlines.Add(Colored(margin, "TextFillColorTertiaryBrush"));
                        text.Inlines.Add(Colored(body, "TextFillColorSecondaryBrush"));
                        break;
                }
            }
        }
        Border card = new() { Child = text, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(10, 6, 10, 6) };
        card.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
        return card;
    }

    private static Run Colored(string text, string brushKey)
    {
        Run run = new(text);
        run.SetResourceReference(TextElement.ForegroundProperty, brushKey);
        return run;
    }

    // The argument that says the most about a call, such as the command of Bash or the file of Edit.
    private static string Summary(string input)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(input);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (string key in SummaryKeys)
                {
                    if (document.RootElement.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                    {
                        return Cut(value.GetString() ?? "", MaxSummaryLength);
                    }
                }
            }
        }
        catch (JsonException)
        {
        }
        return Cut(input, MaxSummaryLength);
    }

    private static BlockUIContainer Folded(string header, UIElement content)
    {
        TextBlock title = new() { Text = header };
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        Expander expander = new() { Header = title, Content = content, IsExpanded = false };
        return new BlockUIContainer(expander) { Margin = new Thickness(0, 4, 0, 8) };
    }

    // Read-only, selectable text, for what sits inside a folded block.
    private static TextBox Plain(string text, bool code)
    {
        TextBox box = new()
        {
            Text = text,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent,
            FontFamily = code ? MarkdownDocumentBuilder.CodeFont : MarkdownDocumentBuilder.BodyFont,
            FontSize = code ? MarkdownDocumentBuilder.CodeSize : MarkdownDocumentBuilder.BodySize,
            Padding = new Thickness(0),
            MinHeight = 0,
        };
        if (!code)
        {
            box.SetResourceReference(Control.ForegroundProperty, "TextFillColorSecondaryBrush");
        }
        return box;
    }

    private static Paragraph Secondary(string text, bool code = false, bool centered = false)
    {
        Paragraph paragraph = new(new Run(text))
        {
            Margin = new Thickness(0, 6, 0, 6),
            TextAlignment = centered ? TextAlignment.Center : TextAlignment.Left,
        };
        if (code)
        {
            paragraph.FontFamily = MarkdownDocumentBuilder.CodeFont;
            paragraph.FontSize = MarkdownDocumentBuilder.CodeSize;
        }
        paragraph.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
        return paragraph;
    }

    private static string Cut(string text, int length)
    {
        return text.Length <= length ? text : string.Concat(text.AsSpan(0, length), "…");
    }

    // A reply's tasks and result fields are only shown: ticking or sending does nothing.
    private sealed class ReadOnlyActions : IMarkdownActions
    {
        public bool IsTicked(int index)
        {
            return false;
        }

        public bool Tick(ListItemBlock item, bool ticked)
        {
            return false;
        }

        public string ResultOf(string name)
        {
            return "";
        }

        public bool SendResult(string name, string text)
        {
            return false;
        }
    }
}
