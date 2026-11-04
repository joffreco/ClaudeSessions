using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Adapters.Consignes;

/// <summary>
/// Reads a consigne file written by the consigne script: a header between two <c>---</c> lines, one
/// <c>key: "json string"</c> per line, then the Markdown body.
/// </summary>
public static class ConsigneFileParser
{
    /// <summary>The header line that opens and closes the header.</summary>
    public const string Fence = "---";

    /// <summary>The status value the script writes for a consigne under review.</summary>
    public const string InReviewValue = "en-revision";

    /// <summary>Reads a consigne.</summary>
    /// <param name="text">The file's content.</param>
    /// <param name="key">Where the file is.</param>
    /// <returns>The consigne; null when the file has no complete header or no title.</returns>
    public static Consigne? Parse(string text, ConsigneKey key)
    {
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length < 2 || lines[0] != Fence)
        {
            return null;
        }
        int close = Array.IndexOf(lines, Fence, 1);
        if (close < 0)
        {
            return null;
        }
        Dictionary<string, string> header = ReadHeader(lines.AsSpan(1, close - 1));
        string title = header.GetValueOrDefault("title", "");
        if (title.Length == 0)
        {
            return null;
        }
        DateTimeOffset updated = DateOf(header.GetValueOrDefault("updated", ""));
        DateTimeOffset created = header.TryGetValue("created", out string? createdText) ? DateOf(createdText) : updated;
        return new Consigne(
            key,
            title,
            header.GetValueOrDefault("status", "") == InReviewValue ? ConsigneStatus.InReview : ConsigneStatus.Ready,
            header.GetValueOrDefault("tab_id", ""),
            header.GetValueOrDefault("tab_title", ""),
            header.GetValueOrDefault("session_id", ""),
            header.GetValueOrDefault("cwd", ""),
            created,
            updated,
            string.Join('\n', lines[(close + 1)..]).TrimEnd('\n'));
    }

    private static Dictionary<string, string> ReadHeader(ReadOnlySpan<string> lines)
    {
        Dictionary<string, string> header = new(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            if (colon > 0 && ValueOf(line[(colon + 2)..]) is string value)
            {
                header[line[..colon]] = value;
            }
        }
        return header;
    }

    private static string? ValueOf(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.String ? document.RootElement.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateTimeOffset DateOf(string text)
    {
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset date) ? date : DateTimeOffset.MinValue;
    }
}
