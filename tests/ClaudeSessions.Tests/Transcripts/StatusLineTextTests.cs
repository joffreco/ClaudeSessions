using System.Linq;
using ClaudeSessions.Adapters.Transcripts;
using ClaudeSessions.Core.Transcripts.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>Reading a status line as its command printed it for the terminal.</summary>
public sealed class StatusLineTextTests
{
    private const string Separator = " \u001b[2m|\u001b[22m ";

    /// <summary>The line splits into segments at each bar between spaces, and its percentages become gauges.</summary>
    [Fact]
    public void Splits_segments_and_turns_percentages_into_gauges()
    {
        string printed = "\u001b[38;5;46mSmart 7%\u001b[0m" + Separator + "\u001b[38;5;196m93%\u001b[39m" + Separator + "\u001b[38;5;118m45%\u001b[39m \u001b[38;5;34m(78%)\u001b[39m";

        StatusLine line = StatusLineText.Parse(printed, null)!;

        StatusSegment[] segments = [.. Assert.Single(line.Rows).Segments];
        Assert.Equal(3, segments.Length);
        Assert.Equal(new StatusText("Smart ", new StatusColor(0, 255, 0), false), segments[0].Parts[0]);
        Assert.Equal(new StatusGauge(7, "7%", new StatusColor(0, 255, 0), true), segments[0].Parts[1]);
        Assert.Equal(new StatusGauge(93, "93%", new StatusColor(255, 0, 0), false), Assert.Single(segments[1].Parts));
        StatusGauge[] gauges = [.. segments[2].Parts.OfType<StatusGauge>()];
        Assert.Equal([45, 78], gauges.Select(gauge => gauge.Percent));
        Assert.Equal("(78%)", gauges[1].Text);
        Assert.All(gauges, gauge => Assert.False(gauge.Labeled));
    }

    /// <summary>The 16 colors, true color, faint text and the reset are followed.</summary>
    [Fact]
    public void Follows_colors_and_faint_text()
    {
        StatusLine line = StatusLineText.Parse("\u001b[33mjaune\u001b[39m \u001b[38;2;1;2;3mrgb\u001b[0m \u001b[2mfaint\u001b[22m plain", null)!;

        StatusText[] texts = [.. Assert.Single(Assert.Single(line.Rows).Segments).Parts.Cast<StatusText>().Where(text => text.Text.Trim().Length > 0)];
        Assert.Equal(new StatusColor(0xC1, 0x9C, 0x00), texts[0].Color);
        Assert.Equal(new StatusColor(1, 2, 3), texts[1].Color);
        Assert.True(texts[2].Dim);
        Assert.Null(texts[2].Color);
        Assert.Equal(new StatusText(" plain", null, false), texts[3]);
    }

    /// <summary>Lines stay apart, links and titles are removed, and a bar inside a word does not split.</summary>
    [Fact]
    public void Keeps_lines_and_removes_other_codes()
    {
        StatusLine line = StatusLineText.Parse("\u001b]8;;https://x\u0007lien\u001b]8;;\u0007 a|b\r\n42,6 %\n", null)!;

        Assert.Equal(2, line.Rows.Count);
        Assert.Equal("lien a|b", string.Concat(line.Rows[0].Segments.Single().Parts.Cast<StatusText>().Select(text => text.Text)));
        Assert.Equal(43, Assert.IsType<StatusGauge>(Assert.Single(line.Rows[1].Segments.Single().Parts)).Percent);
    }

    /// <summary>A line with nothing visible is no status line.</summary>
    [Fact]
    public void Reads_nothing_from_an_empty_line()
    {
        Assert.Null(StatusLineText.Parse("\u001b[0m \n \u001b[2m\u001b[22m", null));
        Assert.Null(StatusLineText.Parse("claude sessions", "claude sessions"));
    }
}
