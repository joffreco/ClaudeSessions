using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Reads a status line as a status line command printed it for the terminal: its colors (ESC [ … m, the 16
/// colors, the 256-color palette and true color) and its faint text are kept, its other terminal codes are
/// removed, it is split into segments at each vertical bar between spaces, and each percentage becomes a gauge.
/// </summary>
public static partial class StatusLineText
{
    // The 16 colors of the Campbell theme of Windows Terminal, as the terminal shows them in dark mode.
    private static readonly StatusColor[] BasicColors =
    [
        Rgb(0x0C0C0C), Rgb(0xC50F1F), Rgb(0x13A10E), Rgb(0xC19C00), Rgb(0x0037DA), Rgb(0x881798), Rgb(0x3A96DD), Rgb(0xCCCCCC),
        Rgb(0x767676), Rgb(0xE74856), Rgb(0x16C60C), Rgb(0xF9F1A5), Rgb(0x3B78FF), Rgb(0xB4009E), Rgb(0x61D6D6), Rgb(0xF2F2F2),
    ];

    /// <summary>Reads a status line.</summary>
    /// <param name="text">What the status line command printed.</param>
    /// <param name="sessionName">Name of the session; the segment that is exactly this name is left out. Null to keep every segment.</param>
    /// <returns>The status line; null when nothing visible remains.</returns>
    public static StatusLine? Parse(string text, string? sessionName)
    {
        string? omitted = string.IsNullOrWhiteSpace(sessionName) ? null : sessionName.Trim();
        List<StatusRow> rows = [];
        foreach (List<StyledChar> line in Lines(Decode(text)))
        {
            List<StatusSegment> segments = [];
            foreach (List<StyledChar> piece in Segments(line))
            {
                List<StyledChar> trimmed = Trim(piece);
                if (trimmed.Count == 0 || (omitted is not null && PlainOf(trimmed) == omitted))
                {
                    continue;
                }
                segments.Add(new StatusSegment(Parts(trimmed)));
            }
            if (segments.Count > 0)
            {
                rows.Add(new StatusRow(segments));
            }
        }
        return rows.Count == 0 ? null : new StatusLine(rows);
    }

    private static List<StyledChar> Decode(string text)
    {
        List<StyledChar> chars = [];
        Style style = default;
        int index = 0;
        while (index < text.Length)
        {
            char c = text[index];
            if (c == '\u001b')
            {
                index = SkipEscape(text, index, ref style);
                continue;
            }
            if (c is '\n' or '\t' || c >= ' ')
            {
                chars.Add(new StyledChar(c == '\t' ? ' ' : c, style));
            }
            index++;
        }
        return chars;
    }

    // Returns the index after the escape sequence starting at start, applying it when it sets a style.
    private static int SkipEscape(string text, int start, ref Style style)
    {
        int index = start + 1;
        if (index >= text.Length)
        {
            return index;
        }
        if (text[index] == '[')
        {
            int end = index + 1;
            while (end < text.Length && text[end] is < '@' or > '~')
            {
                end++;
            }
            if (end < text.Length && text[end] == 'm')
            {
                style = Apply(style, text[(index + 1)..end]);
            }
            return end + 1;
        }
        if (text[index] == ']')
        {
            int end = index + 1;
            while (end < text.Length && text[end] != '\u0007' && !(text[end] == '\u001b' && end + 1 < text.Length && text[end + 1] == '\\'))
            {
                end++;
            }
            return end < text.Length && text[end] == '\u001b' ? end + 2 : end + 1;
        }
        return index + 1;
    }

    private static Style Apply(Style style, string parameters)
    {
        int[] codes = [.. parameters.Split(';').Select(code => int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : 0)];
        for (int index = 0; index < codes.Length; index++)
        {
            int code = codes[index];
            switch (code)
            {
                case 0:
                    style = default;
                    break;
                case 2:
                    style = style with { Dim = true };
                    break;
                case 22:
                    style = style with { Dim = false };
                    break;
                case >= 30 and <= 37:
                    style = style with { Color = BasicColors[code - 30] };
                    break;
                case >= 90 and <= 97:
                    style = style with { Color = BasicColors[code - 90 + 8] };
                    break;
                case 39:
                    style = style with { Color = null };
                    break;
                case 38 or 48:
                    (StatusColor? color, int used) = Extended(codes, index + 1);
                    if (code == 38 && used > 0)
                    {
                        style = style with { Color = color };
                    }
                    index += used;
                    break;
            }
        }
        return style;
    }

    // Reads 5;n or 2;r;g;b at start; returns the color and how many codes it took.
    private static (StatusColor? Color, int Used) Extended(int[] codes, int start)
    {
        if (start < codes.Length && codes[start] == 5 && start + 1 < codes.Length)
        {
            return (Palette(codes[start + 1]), 2);
        }
        if (start < codes.Length && codes[start] == 2 && start + 3 < codes.Length)
        {
            return (new StatusColor(Byte(codes[start + 1]), Byte(codes[start + 2]), Byte(codes[start + 3])), 4);
        }
        return (null, 0);
    }

    private static StatusColor Palette(int number)
    {
        if (number is < 0 or > 255)
        {
            return BasicColors[7];
        }
        if (number < 16)
        {
            return BasicColors[number];
        }
        if (number >= 232)
        {
            byte gray = (byte)(8 + (10 * (number - 232)));
            return new StatusColor(gray, gray, gray);
        }
        int cube = number - 16;
        return new StatusColor(Level(cube / 36), Level(cube / 6 % 6), Level(cube % 6));
    }

    private static byte Level(int step)
    {
        return step == 0 ? (byte)0 : (byte)(55 + (40 * step));
    }

    private static byte Byte(int value)
    {
        return (byte)Math.Clamp(value, 0, 255);
    }

    private static StatusColor Rgb(uint rgb)
    {
        return new StatusColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    private static IEnumerable<List<StyledChar>> Lines(List<StyledChar> chars)
    {
        List<StyledChar> line = [];
        foreach (StyledChar styled in chars)
        {
            if (styled.Char == '\n')
            {
                yield return line;
                line = [];
                continue;
            }
            line.Add(styled);
        }
        yield return line;
    }

    // Splits a line at each vertical bar that has a space, or the line's end, on both sides.
    private static IEnumerable<List<StyledChar>> Segments(List<StyledChar> line)
    {
        List<StyledChar> segment = [];
        for (int index = 0; index < line.Count; index++)
        {
            bool spaceBefore = index == 0 || line[index - 1].Char == ' ';
            bool spaceAfter = index == line.Count - 1 || line[index + 1].Char == ' ';
            if (line[index].Char == '|' && spaceBefore && spaceAfter)
            {
                yield return segment;
                segment = [];
                continue;
            }
            segment.Add(line[index]);
        }
        yield return segment;
    }

    private static List<StyledChar> Trim(List<StyledChar> chars)
    {
        int start = 0;
        int end = chars.Count;
        while (start < end && char.IsWhiteSpace(chars[start].Char))
        {
            start++;
        }
        while (end > start && char.IsWhiteSpace(chars[end - 1].Char))
        {
            end--;
        }
        return chars.GetRange(start, end - start);
    }

    private static string PlainOf(List<StyledChar> chars)
    {
        return new string([.. chars.Select(styled => styled.Char)]);
    }

    private static List<StatusPart> Parts(List<StyledChar> chars)
    {
        List<StatusPart> parts = [];
        bool wordSeen = false;
        bool gaugeSeen = false;
        int start = 0;
        while (start < chars.Count)
        {
            Style style = chars[start].Style;
            int end = start;
            while (end < chars.Count && chars[end].Style == style)
            {
                end++;
            }
            string run = PlainOf(chars.GetRange(start, end - start));
            int position = 0;
            foreach (Match match in Percentage().Matches(run))
            {
                AddText(parts, run[position..match.Index], style, ref wordSeen);
                int percent = (int)Math.Round(Math.Clamp(double.Parse(match.Groups["value"].Value.Replace(',', '.'), CultureInfo.InvariantCulture), 0, 100), MidpointRounding.AwayFromZero);
                parts.Add(new StatusGauge(percent, match.Value, style.Color, wordSeen && !gaugeSeen));
                gaugeSeen = true;
                position = match.Index + match.Length;
            }
            AddText(parts, run[position..], style, ref wordSeen);
            start = end;
        }
        return parts;
    }

    private static void AddText(List<StatusPart> parts, string text, Style style, ref bool wordSeen)
    {
        if (text.Length == 0)
        {
            return;
        }
        wordSeen |= !string.IsNullOrWhiteSpace(text);
        parts.Add(new StatusText(text, style.Color, style.Dim));
    }

    [GeneratedRegex(@"\(?(?<value>\d{1,3}(?:[.,]\d+)?) ?%\)?")]
    private static partial Regex Percentage();

    private readonly record struct Style(StatusColor? Color, bool Dim);

    private readonly record struct StyledChar(char Char, Style Style);
}
