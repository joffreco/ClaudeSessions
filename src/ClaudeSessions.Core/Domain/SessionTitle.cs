using System.Buffers;
using System.Globalization;
using System.Text;

namespace ClaudeSessions.Core.Domain;

/// <summary>
/// A title a session gives its tab, split into the state mark Claude Code puts in front of it
/// (✳ when it waits, a spinner while it works) and the title proper.
/// </summary>
/// <param name="Status">The state mark, or null when the title has none.</param>
/// <param name="Text">The title without its state mark.</param>
public sealed record SessionTitle(string? Status, string Text)
{
    /// <summary>The mark Claude Code shows while it waits for the user.</summary>
    public const string IdleMark = "✳";

    private const char VariationSelector = '️';

    /// <summary>True when the session waits for the user.</summary>
    public bool IsIdle => Status == IdleMark;

    /// <summary>Splits a raw title: a leading symbol followed by a space is the state mark.</summary>
    /// <param name="raw">The title as the session set it.</param>
    /// <returns>The state mark and the title proper.</returns>
    public static SessionTitle Parse(string raw)
    {
        string trimmed = raw.Trim();
        if (trimmed.Length == 0 || Rune.DecodeFromUtf16(trimmed, out Rune first, out int length) != OperationStatus.Done)
        {
            return new SessionTitle(null, trimmed);
        }
        int next = length < trimmed.Length && trimmed[length] == VariationSelector ? length + 1 : length;
        if (Rune.GetUnicodeCategory(first) != UnicodeCategory.OtherSymbol || next >= trimmed.Length || trimmed[next] != ' ')
        {
            return new SessionTitle(null, trimmed);
        }
        return new SessionTitle(first.ToString(), trimmed[next..].Trim());
    }
}
