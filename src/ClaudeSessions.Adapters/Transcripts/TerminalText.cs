using System;
using System.Text.RegularExpressions;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>Turns text written for a terminal into plain text, for a view that does not interpret its codes.</summary>
internal static partial class TerminalText
{
    /// <summary>Removes the terminal's color and style codes (ESC [ … m), its OSC sequences (titles, links) and lone escapes.</summary>
    /// <param name="text">The text, as a command printed it.</param>
    /// <returns>The text without its codes.</returns>
    public static string Plain(string text)
    {
        return text.Contains('\u001b', StringComparison.Ordinal) ? TerminalCode().Replace(text, "") : text;
    }

    [GeneratedRegex(@"\u001b(?:\[[0-?]*[ -/]*[@-~]|\][^\u0007\u001b]*(?:\u0007|\u001b\\)|[@-Z\\-_])")]
    private static partial Regex TerminalCode();
}
