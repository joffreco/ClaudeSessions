using ClaudeSessions.Adapters;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Window titles picked out of a terminal output stream.</summary>
public sealed class OscTitleParserTests
{
    /// <summary>An OSC 0 title ended by BEL is found.</summary>
    [Fact]
    public void Finds_a_title_ended_by_bel()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("before\u001b]0;✳ Fix the hook\u0007after");

        Assert.Equal("✳ Fix the hook", title);
    }

    /// <summary>An OSC 2 title ended by ST (ESC \) is found.</summary>
    [Fact]
    public void Finds_a_title_ended_by_string_terminator()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b]2;comutils\u001b\\");

        Assert.Equal("comutils", title);
    }

    /// <summary>A title split across two chunks is found when the second one completes it.</summary>
    [Fact]
    public void Finds_a_title_split_across_chunks()
    {
        OscTitleParser parser = new();

        string? first = parser.Feed("text\u001b]0;Hal");
        string? second = parser.Feed("f title\u0007");

        Assert.Null(first);
        Assert.Equal("Half title", second);
    }

    /// <summary>A lone ESC at the end of a chunk is kept for the next one.</summary>
    [Fact]
    public void Keeps_a_trailing_escape_for_the_next_chunk()
    {
        OscTitleParser parser = new();

        parser.Feed("text\u001b");
        string? title = parser.Feed("]0;Next\u0007");

        Assert.Equal("Next", title);
    }

    /// <summary>The last of several titles in one chunk wins.</summary>
    [Fact]
    public void Returns_the_last_title_of_a_chunk()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b]0;one\u0007\u001b]0;two\u0007");

        Assert.Equal("two", title);
    }

    /// <summary>Other OSC sequences (hyperlinks, colors) are not titles.</summary>
    [Fact]
    public void Ignores_other_osc_sequences()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b]8;;https://example.com\u0007link\u001b]8;;\u0007\u001b]10;?\u0007");

        Assert.Null(title);
    }

    /// <summary>Ordinary escape sequences are not titles.</summary>
    [Fact]
    public void Ignores_csi_sequences()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b[2J\u001b[1;1H\u001b[?2004h");

        Assert.Null(title);
    }

    /// <summary>Control characters inside a title are removed.</summary>
    [Fact]
    public void Strips_control_characters_from_a_title()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b]0;evil\u0008\u0008\u000dtitle\u0007");

        Assert.Equal("eviltitle", title);
    }

    /// <summary>A very long title is cut.</summary>
    [Fact]
    public void Cuts_an_overlong_title()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b]0;" + new string('x', 5000) + "\u0007");

        Assert.Equal(OscTitleParser.MaxTitleLength, title?.Length);
    }

    /// <summary>An unterminated sequence longer than the limit is dropped, so memory stays bounded.</summary>
    [Fact]
    public void Drops_an_unterminated_sequence_beyond_the_limit()
    {
        OscTitleParser parser = new();
        parser.Feed("\u001b]0;" + new string('x', OscTitleParser.MaxPendingLength + 10));

        string? title = parser.Feed("\u0007\u001b]0;fresh\u0007");

        Assert.Equal("fresh", title);
    }

    /// <summary>An empty title is reported as empty, which the tab shows as its folder name.</summary>
    [Fact]
    public void Reports_an_empty_title()
    {
        OscTitleParser parser = new();

        string? title = parser.Feed("\u001b]0;\u0007");

        Assert.Equal("", title);
    }

    // category N/A: error — the parser has no failure path; every input yields a title or null.
}
