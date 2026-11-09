using System;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Reading what claude-sessions-activity prints.</summary>
public sealed class TabActivityReportTests
{
    /// <summary>Both lines give the newest transcript write and the child count.</summary>
    [Fact]
    public void Reads_the_transcript_time_and_the_children()
    {
        TabActivity? activity = TabActivityReport.Parse("transcript=1793822139\nchildren=2\n");

        Assert.Equal(new TabActivity(DateTimeOffset.FromUnixTimeSeconds(1793822139), 2), activity);
    }

    /// <summary>An empty transcript value means the session has no transcript yet.</summary>
    [Fact]
    public void Reads_an_empty_transcript_as_none()
    {
        TabActivity? activity = TabActivityReport.Parse("transcript=\nchildren=0\n");

        Assert.Equal(new TabActivity(null, 0), activity);
    }

    /// <summary>Windows line endings from wsl.exe are accepted.</summary>
    [Fact]
    public void Accepts_windows_line_endings()
    {
        TabActivity? activity = TabActivityReport.Parse("transcript=100\r\nchildren=0\r\n");

        Assert.Equal(new TabActivity(DateTimeOffset.FromUnixTimeSeconds(100), 0), activity);
    }

    /// <summary>Output missing a line, or empty, cannot be read.</summary>
    /// <param name="output">The output.</param>
    [Theory]
    [InlineData("")]
    [InlineData("transcript=100\n")]
    [InlineData("children=0\n")]
    [InlineData("usage: claude-sessions-activity <tab_uuid> <session_uuid>\n")]
    public void Rejects_an_incomplete_output(string output)
    {
        Assert.Null(TabActivityReport.Parse(output));
    }

    /// <summary>Values that are not plain non-negative integers cannot be read.</summary>
    /// <param name="output">The output.</param>
    [Theory]
    [InlineData("transcript=abc\nchildren=0\n")]
    [InlineData("transcript=100\nchildren=many\n")]
    [InlineData("transcript=-5\nchildren=0\n")]
    [InlineData("transcript=100\nchildren=-1\n")]
    [InlineData("transcript=1793822139.52\nchildren=0\n")]
    [InlineData("transcript= 100\nchildren=0\n")]
    public void Rejects_a_malformed_value(string output)
    {
        Assert.Null(TabActivityReport.Parse(output));
    }

    /// <summary>Numbers too large for their type, or past the last representable date, cannot be read.</summary>
    /// <param name="output">The output.</param>
    [Theory]
    [InlineData("transcript=99999999999999999999999\nchildren=0\n")]
    [InlineData("transcript=100\nchildren=99999999999\n")]
    public void Rejects_an_overflowing_number(string output)
    {
        Assert.Null(TabActivityReport.Parse(output));
    }

    /// <summary>A transcript time beyond the last date .NET can hold cannot be read instead of throwing.</summary>
    [Fact]
    public void Rejects_a_transcript_time_beyond_the_last_date()
    {
        TabActivity? activity = TabActivityReport.Parse("transcript=999999999999999\nchildren=0\n");

        Assert.Null(activity);
    }

    /// <summary>Lines the probe never prints, such as a warning from a login shell, are ignored.</summary>
    [Fact]
    public void Ignores_unknown_lines()
    {
        TabActivity? activity = TabActivityReport.Parse("bash: warning: setlocale\ntranscript=100\nchildren=1\n");

        Assert.Equal(new TabActivity(DateTimeOffset.FromUnixTimeSeconds(100), 1), activity);
    }
}
