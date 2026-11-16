using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClaudeSessions.Adapters.Transcripts.Dto;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Knows what transcript messages mean. A model reply spans one line per block, all sharing
/// the reply's message id. A tool result is a user line answering a call by its id, and with
/// parallel calls each result hangs off its own call, outside the active chain, so results are
/// matched by id. Commands come as user lines wrapped in tags (command-name, bash-input,
/// local-command-stdout...).
/// </summary>
internal static partial class ConversationMapper
{
    private const string InterruptionMark = "[Request interrupted by user";

    /// <summary>Turns the active branch into conversation entries.</summary>
    /// <param name="branch">The active branch, oldest first.</param>
    /// <param name="all">Every message line, in file order, to find reply blocks and tool results off the chain.</param>
    /// <returns>The entries, oldest first.</returns>
    public static IReadOnlyList<ConversationEntry> Map(IReadOnlyList<TranscriptRecord> branch, IReadOnlyList<TranscriptRecord> all)
    {
        Dictionary<string, ToolResult> results = ToolResults(all);
        Dictionary<string, List<TranscriptRecord>> replies = ReplyLines(branch, all);
        HashSet<string> shown = new(StringComparer.Ordinal);
        List<ConversationEntry> entries = [];
        foreach (TranscriptRecord record in branch)
        {
            ConversationEntry? entry = record.Kind switch
            {
                TranscriptLineParser.Assistant => Reply(record, replies, results, shown),
                TranscriptLineParser.User => FromUser(record),
                TranscriptLineParser.System => FromSystem(record),
                _ => null,
            };
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }
        return entries;
    }

    /// <summary>
    /// Gathers the skills Claude Code listed to the model: a full listing at the first prompt, then the new
    /// ones as they appear. Each skill_listing attachment names its skills and gives "- name: description"
    /// lines, a description possibly running over several lines.
    /// </summary>
    /// <param name="all">Every line, in file order.</param>
    /// <returns>The skills, by name.</returns>
    public static IReadOnlyList<SkillInfo> Skills(IReadOnlyList<TranscriptRecord> all)
    {
        SortedDictionary<string, SkillInfo> skills = new(StringComparer.OrdinalIgnoreCase);
        foreach (TranscriptRecord record in all.Where(r => r.AttachmentType == "skill_listing" && !r.IsSidechain))
        {
            if (record.IsInitialListing)
            {
                skills.Clear();
            }
            IReadOnlyList<string> names = record.SkillNames ?? [];
            string content = record.Text ?? "";
            foreach (string name in names)
            {
                skills[name] = new SkillInfo(name, DescriptionOf(content, name, names));
            }
        }
        return [.. skills.Values];
    }

    // The text after "- name: ", up to the next listed skill's line.
    private static string DescriptionOf(string content, string name, IReadOnlyList<string> names)
    {
        string marker = "- " + name + ":";
        int start = content.StartsWith(marker, StringComparison.Ordinal) ? 0 : content.IndexOf("\n" + marker, StringComparison.Ordinal) + 1;
        if (start < 0 || (start == 0 && !content.StartsWith(marker, StringComparison.Ordinal)))
        {
            return "";
        }
        start += marker.Length;
        int end = content.Length;
        foreach (string other in names)
        {
            int next = content.IndexOf("\n- " + other + ":", start, StringComparison.Ordinal);
            if (next >= 0 && next < end)
            {
                end = next;
            }
        }
        return content[start..end].Trim();
    }

    private static Dictionary<string, ToolResult> ToolResults(IReadOnlyList<TranscriptRecord> all)
    {
        Dictionary<string, ToolResult> results = new(StringComparer.Ordinal);
        foreach (TranscriptRecord record in all.Where(r => r.Kind == TranscriptLineParser.User && !r.IsSidechain))
        {
            foreach (TranscriptBlock block in record.Blocks.Where(b => b.Type == "tool_result" && b.ToolUseId.Length > 0))
            {
                results[block.ToolUseId] = new ToolResult(Plain(block.Text), block.IsError, record.Patch);
            }
        }
        return results;
    }

    // Every line of each reply the branch passes through, even lines the chain skips.
    private static Dictionary<string, List<TranscriptRecord>> ReplyLines(IReadOnlyList<TranscriptRecord> branch, IReadOnlyList<TranscriptRecord> all)
    {
        HashSet<string> ids = new(
            branch.Where(r => r.Kind == TranscriptLineParser.Assistant && r.MessageId is not null).Select(r => r.MessageId!),
            StringComparer.Ordinal);
        Dictionary<string, List<TranscriptRecord>> lines = new(StringComparer.Ordinal);
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (TranscriptRecord record in all)
        {
            if (record.Kind != TranscriptLineParser.Assistant || record.IsSidechain || record.MessageId is null
                || !ids.Contains(record.MessageId) || !seen.Add(record.Uuid))
            {
                continue;
            }
            if (!lines.TryGetValue(record.MessageId, out List<TranscriptRecord>? reply))
            {
                reply = [];
                lines[record.MessageId] = reply;
            }
            reply.Add(record);
        }
        return lines;
    }

    private static AssistantMessage? Reply(
        TranscriptRecord record,
        Dictionary<string, List<TranscriptRecord>> replies,
        Dictionary<string, ToolResult> results,
        HashSet<string> shown)
    {
        IReadOnlyList<TranscriptRecord> lines = [record];
        if (record.MessageId is not null)
        {
            if (!shown.Add(record.MessageId))
            {
                return null;
            }
            lines = replies.TryGetValue(record.MessageId, out List<TranscriptRecord>? reply) ? reply : lines;
        }
        List<AssistantBlock> blocks = [];
        foreach (TranscriptBlock block in lines.SelectMany(line => line.Blocks))
        {
            AssistantBlock? converted = block.Type switch
            {
                "text" when block.Text.Trim().Length > 0 => new AssistantText(block.Text),
                "thinking" when block.Text.Trim().Length > 0 => new AssistantThinking(block.Text),
                "tool_use" => new ToolCall(block.ToolUseId, block.Name, block.Input, results.GetValueOrDefault(block.ToolUseId)),
                _ => null,
            };
            if (converted is not null)
            {
                blocks.Add(converted);
            }
        }
        // Every line of a reply repeats its usage as it stood when the line was written: the largest is the latest.
        int outputTokens = lines.Max(line => line.OutputTokens);
        bool endsTurn = lines.Any(line => line.StopReason == "end_turn");
        return blocks.Count > 0 ? new AssistantMessage(record.Timestamp, blocks, outputTokens, endsTurn) : null;
    }

    private static ConversationEntry? FromUser(TranscriptRecord record)
    {
        if (record.IsMeta || record.IsCompactSummary)
        {
            return null;
        }
        if (record.Text is not null)
        {
            return Classify(record.Timestamp, record.Text);
        }
        string[] parts = [.. record.Blocks.Where(b => b.Type is "text" or "image").Select(b => b.Text).Where(t => t.Length > 0)];
        return parts.Length > 0 ? Classify(record.Timestamp, string.Join("\n", parts)) : null;
    }

    private static ConversationEntry? FromSystem(TranscriptRecord record)
    {
        string text = record.Text ?? "";
        return record.Subtype switch
        {
            "compact_boundary" => new CompactionMarker(record.Timestamp),
            "turn_duration" => new TurnEnd(record.Timestamp, record.DurationMs is long milliseconds ? TimeSpan.FromMilliseconds(milliseconds) : null, false),
            "local_command" => new CommandOutput(record.Timestamp, Plain(Tags(text).GetValueOrDefault("local-command-stdout", StripTags(text)))),
            "informational" when text.Trim().Length > 0 => new Notice(record.Timestamp, text),
            _ => null,
        };
    }

    // A user message is a typed prompt, or a command wrapped in tags.
    private static ConversationEntry? Classify(DateTimeOffset? timestamp, string text)
    {
        // Claude Code records the Escape that stops a turn as a user line, "[Request interrupted by user]" or
        // "[Request interrupted by user for tool use]".
        if (text.StartsWith(InterruptionMark, StringComparison.Ordinal))
        {
            return new TurnEnd(timestamp, null, true);
        }
        Match leading = LeadingTag().Match(text);
        if (!leading.Success)
        {
            return new UserPrompt(timestamp, text);
        }
        Dictionary<string, string> tags = Tags(text);
        return leading.Groups["tag"].Value switch
        {
            "command-name" or "command-message" => new SlashCommand(
                timestamp,
                tags.GetValueOrDefault("command-name", "/" + tags.GetValueOrDefault("command-message", "")),
                tags.GetValueOrDefault("command-args", "")),
            "bash-input" => new ShellCommand(timestamp, tags.GetValueOrDefault("bash-input", "")),
            "bash-stdout" or "bash-stderr" => new CommandOutput(
                timestamp,
                Plain(string.Join("\n", new[] { tags.GetValueOrDefault("bash-stdout", ""), tags.GetValueOrDefault("bash-stderr", "") }.Where(s => s.Length > 0)))),
            "local-command-stdout" => new CommandOutput(timestamp, Plain(tags.GetValueOrDefault("local-command-stdout", ""))),
            "local-command-caveat" or "system-reminder" => null,
            _ => new Notice(timestamp, StripTags(text)),
        };
    }

    private static Dictionary<string, string> Tags(string text)
    {
        Dictionary<string, string> tags = new(StringComparer.Ordinal);
        foreach (Match match in TaggedSection().Matches(text))
        {
            tags.TryAdd(match.Groups["tag"].Value, match.Groups["body"].Value.Trim());
        }
        return tags;
    }

    private static string StripTags(string text)
    {
        return AnyTag().Replace(text, "").Trim();
    }

    // Command output keeps the terminal's color and style codes; a view shows plain text.
    private static string Plain(string text)
    {
        return TerminalText.Plain(text);
    }

    [GeneratedRegex(@"^\s*<(?<tag>[a-z][a-z-]*)>")]
    private static partial Regex LeadingTag();

    [GeneratedRegex(@"<(?<tag>[a-z][a-z-]*)>(?<body>.*?)</\k<tag>>", RegexOptions.Singleline)]
    private static partial Regex TaggedSection();

    [GeneratedRegex(@"</?[a-z][a-z-]*>")]
    private static partial Regex AnyTag();
}
