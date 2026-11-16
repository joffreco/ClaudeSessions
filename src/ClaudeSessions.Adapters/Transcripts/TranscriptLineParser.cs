using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using ClaudeSessions.Adapters.Transcripts.Dto;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Knows the syntax of one transcript line: which JSON fields hold what. Every line with a uuid
/// is kept, whatever its type, because the chain of parentUuid runs through all of them
/// (attachment lines sit between a prompt and its reply). Lines without a uuid (titles, modes,
/// request logs, file history...) are dropped here, so a new kind of line changes nothing.
/// </summary>
internal static class TranscriptLineParser
{
    /// <summary>Longest text kept from a prompt, a block or a tool result; the rest is cut.</summary>
    public const int MaxTextLength = 20_000;

    /// <summary>Most lines kept from the file change of one tool result; the rest is left out.</summary>
    public const int MaxPatchLines = 2_000;

    /// <summary>A prompt, a command or a tool result. The other line types that carry the conversation follow.</summary>
    public const string User = "user";

    /// <summary>A model reply, one line per content block.</summary>
    public const string Assistant = "assistant";

    /// <summary>A message from Claude Code itself: command output, compaction, notices.</summary>
    public const string System = "system";

    private const string Ellipsis = "…";

    /// <summary>Reads one line of a transcript.</summary>
    /// <param name="line">The line's UTF-8 bytes, without its line break.</param>
    /// <returns>The message it holds, or null when it is not a message or not valid JSON.</returns>
    public static TranscriptRecord? Parse(ReadOnlyMemory<byte> line)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            return Read(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TranscriptRecord? Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        string kind = Text(root, "type");
        string uuid = Text(root, "uuid");
        if (uuid.Length == 0)
        {
            return null;
        }
        if (root.TryGetProperty("attachment", out JsonElement attachment) && attachment.ValueKind == JsonValueKind.Object)
        {
            return Attachment(root, kind, uuid, attachment);
        }
        JsonElement content = default;
        string? messageId = null;
        string stopReason = "";
        int outputTokens = 0;
        if (root.TryGetProperty("message", out JsonElement message) && message.ValueKind == JsonValueKind.Object)
        {
            message.TryGetProperty("content", out content);
            messageId = OptionalText(message, "id");
            stopReason = Text(message, "stop_reason");
            if (message.TryGetProperty("usage", out JsonElement usage) && usage.ValueKind == JsonValueKind.Object)
            {
                outputTokens = Number(usage, "output_tokens");
            }
        }
        else
        {
            root.TryGetProperty("content", out content);
        }
        return new TranscriptRecord(
            kind,
            uuid,
            OptionalText(root, "parentUuid"),
            OptionalText(root, "logicalParentUuid"),
            Flag(root, "isSidechain"),
            Flag(root, "isMeta"),
            Flag(root, "isCompactSummary"),
            Text(root, "subtype"),
            messageId,
            Timestamp(root),
            content.ValueKind == JsonValueKind.String ? Cap(content.GetString() ?? "") : null,
            content.ValueKind == JsonValueKind.Array ? Blocks(content) : [],
            Patch: Patch(root),
            StopReason: stopReason,
            OutputTokens: outputTokens,
            DurationMs: root.TryGetProperty("durationMs", out JsonElement duration) && duration.ValueKind == JsonValueKind.Number && duration.TryGetInt64(out long milliseconds) ? milliseconds : null);
    }

    // toolUseResult of an Edit or a Write: structuredPatch holds the hunks of a change; a Write that
    // creates a file has none and gives its content, shown as one hunk of added lines.
    private static List<DiffHunk>? Patch(JsonElement root)
    {
        if (!root.TryGetProperty("toolUseResult", out JsonElement result) || result.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        int budget = MaxPatchLines;
        List<DiffHunk> hunks = [];
        if (result.TryGetProperty("structuredPatch", out JsonElement patch) && patch.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement hunk in patch.EnumerateArray())
            {
                if (hunk.ValueKind != JsonValueKind.Object || !hunk.TryGetProperty("lines", out JsonElement lines) || lines.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                List<string> kept = [];
                foreach (JsonElement line in lines.EnumerateArray())
                {
                    if (budget-- <= 0)
                    {
                        break;
                    }
                    kept.Add(line.ValueKind == JsonValueKind.String ? line.GetString() ?? "" : "");
                }
                hunks.Add(new DiffHunk(Number(hunk, "oldStart"), Number(hunk, "newStart"), kept));
            }
        }
        if (hunks.Count == 0 && Text(result, "type") == "create" && result.TryGetProperty("content", out JsonElement created) && created.ValueKind == JsonValueKind.String)
        {
            string[] lines = (created.GetString() ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');
            hunks.Add(new DiffHunk(0, 1, [.. lines.Take(budget).Select(line => "+" + line)]));
        }
        return hunks.Count == 0 ? null : hunks;
    }

    private static int Number(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : 0;
    }

    // An attachment line links the chain; a skill_listing one also lists the skills, names apart and
    // "- name: description" lines in its content.
    private static TranscriptRecord Attachment(JsonElement root, string kind, string uuid, JsonElement attachment)
    {
        string type = Text(attachment, "type");
        List<string> names = [];
        if (type == "skill_listing" && attachment.TryGetProperty("names", out JsonElement listed) && listed.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement name in listed.EnumerateArray())
            {
                if (name.ValueKind == JsonValueKind.String && name.GetString() is { Length: > 0 } text)
                {
                    names.Add(text);
                }
            }
        }
        return new TranscriptRecord(
            kind,
            uuid,
            OptionalText(root, "parentUuid"),
            OptionalText(root, "logicalParentUuid"),
            Flag(root, "isSidechain"),
            Flag(root, "isMeta"),
            false,
            "",
            null,
            Timestamp(root),
            type == "skill_listing" ? Text(attachment, "content") : null,
            [],
            type,
            names,
            Flag(attachment, "isInitial"));
    }

    private static List<TranscriptBlock> Blocks(JsonElement content)
    {
        List<TranscriptBlock> blocks = [];
        foreach (JsonElement block in content.EnumerateArray())
        {
            if (block.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            string type = Text(block, "type");
            blocks.Add(type switch
            {
                "text" => new TranscriptBlock(type, Cap(Text(block, "text")), "", "", "", false),
                "thinking" => new TranscriptBlock(type, Cap(Text(block, "thinking")), "", "", "", false),
                "image" => new TranscriptBlock(type, "[image]", "", "", "", false),
                "tool_use" => new TranscriptBlock(type, "", Text(block, "id"), Text(block, "name"), Cap(Raw(block, "input")), false),
                "tool_result" => new TranscriptBlock(type, ResultText(block), Text(block, "tool_use_id"), "", "", Flag(block, "is_error")),
                _ => new TranscriptBlock(type, "", "", "", "", false),
            });
        }
        return blocks;
    }

    // A tool result holds a string, or a list of text and image parts.
    private static string ResultText(JsonElement block)
    {
        if (!block.TryGetProperty("content", out JsonElement content))
        {
            return "";
        }
        if (content.ValueKind == JsonValueKind.String)
        {
            return Cap(content.GetString() ?? "");
        }
        if (content.ValueKind != JsonValueKind.Array)
        {
            return "";
        }
        StringBuilder text = new();
        foreach (JsonElement part in content.EnumerateArray())
        {
            string piece = part.ValueKind != JsonValueKind.Object ? "" : Text(part, "type") switch
            {
                "text" => Text(part, "text"),
                "image" => "[image]",
                _ => "",
            };
            if (piece.Length > 0)
            {
                text.Append(text.Length > 0 ? "\n" : "").Append(piece);
            }
        }
        return Cap(text.ToString());
    }

    private static string Text(JsonElement element, string name)
    {
        return OptionalText(element, name) ?? "";
    }

    private static string? OptionalText(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string Raw(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) ? value.GetRawText() : "";
    }

    private static bool Flag(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;
    }

    private static DateTimeOffset? Timestamp(JsonElement root)
    {
        return DateTimeOffset.TryParse(OptionalText(root, "timestamp"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset value)
            ? value
            : null;
    }

    private static string Cap(string text)
    {
        return text.Length <= MaxTextLength ? text : string.Concat(text.AsSpan(0, MaxTextLength), Ellipsis);
    }
}
