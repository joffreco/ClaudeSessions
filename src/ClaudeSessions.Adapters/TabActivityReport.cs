using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Adapters;

/// <summary>Reads what claude-sessions-activity prints: <c>transcript=&lt;epoch seconds or empty&gt;</c> and <c>children=&lt;count&gt;</c>.</summary>
public static class TabActivityReport
{
    private const string TranscriptKey = "transcript";
    private const string ChildrenKey = "children";
    private static readonly long LastUnixSecond = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    /// <summary>Parses the output of the probe.</summary>
    /// <param name="output">Everything the probe wrote on standard output.</param>
    /// <returns>The activity, or null when a line is missing or malformed.</returns>
    public static TabActivity? Parse(string output)
    {
        Dictionary<string, string> values = Lines(output);
        if (!values.TryGetValue(TranscriptKey, out string? transcript) || !values.TryGetValue(ChildrenKey, out string? children))
        {
            return null;
        }
        if (!int.TryParse(children, NumberStyles.None, CultureInfo.InvariantCulture, out int childCount))
        {
            return null;
        }
        if (transcript.Length == 0)
        {
            return new TabActivity(null, childCount);
        }
        return long.TryParse(transcript, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds) && seconds <= LastUnixSecond
            ? new TabActivity(DateTimeOffset.FromUnixTimeSeconds(seconds), childCount)
            : null;
    }

    private static Dictionary<string, string> Lines(string output)
    {
        return output.Split('\n')
            .Select(line => line.Trim().Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last()[1], StringComparer.Ordinal);
    }
}
