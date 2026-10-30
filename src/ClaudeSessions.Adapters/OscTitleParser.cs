using System.Linq;

namespace ClaudeSessions.Adapters;

/// <summary>
/// Watches the output of a terminal for the window-title sequences (OSC 0 and OSC 2), including
/// sequences split across two chunks. The output itself is never changed.
/// </summary>
public sealed class OscTitleParser
{
    /// <summary>Longest unterminated sequence kept between two chunks; a longer one is dropped.</summary>
    public const int MaxPendingLength = 4096;

    /// <summary>Longest title kept; a longer title is cut.</summary>
    public const int MaxTitleLength = 200;

    private const char Escape = '\u001b';
    private const char Bell = '\u0007';

    private string _pending = "";

    /// <summary>Reads one chunk of output.</summary>
    /// <param name="chunk">The chunk, as sent to the terminal.</param>
    /// <returns>The last title the chunk completes, or null when it completes none.</returns>
    public string? Feed(string chunk)
    {
        string text = _pending + chunk;
        _pending = "";
        string? title = null;
        int index = 0;
        while (index < text.Length)
        {
            int start = text.IndexOf(Escape, index);
            if (start < 0)
            {
                break;
            }
            if (start == text.Length - 1)
            {
                _pending = text[start..];
                break;
            }
            if (text[start + 1] != ']')
            {
                index = start + 1;
                continue;
            }
            (int end, int terminatorLength) = FindTerminator(text, start + 2);
            if (end < 0)
            {
                KeepUnterminated(text, start);
                break;
            }
            title = TitleOf(text[(start + 2)..end]) ?? title;
            index = end + terminatorLength;
        }
        return title;
    }

    private void KeepUnterminated(string text, int start)
    {
        if (text.Length - start <= MaxPendingLength)
        {
            _pending = text[start..];
        }
    }

    private static (int End, int TerminatorLength) FindTerminator(string text, int from)
    {
        for (int position = from; position < text.Length; position++)
        {
            if (text[position] == Bell)
            {
                return (position, 1);
            }
            if (text[position] == Escape && position + 1 < text.Length && text[position + 1] == '\\')
            {
                return (position, 2);
            }
        }
        return (-1, 0);
    }

    private static string? TitleOf(string body)
    {
        int separator = body.IndexOf(';');
        if (separator < 0 || body[..separator] is not ("0" or "2"))
        {
            return null;
        }
        string title = new(body[(separator + 1)..].Where(character => !char.IsControl(character)).ToArray());
        title = title.Trim();
        return title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;
    }
}
