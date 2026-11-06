using System;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>What the files written by the hook mean.</summary>
public sealed class SessionRecordWatcherTests
{
    private const string TabText = "11111111-2222-3333-4444-555555555555";
    private static readonly TabId Tab = new(Guid.Parse(TabText));

    /// <summary>A .json file named after a tab is its session record.</summary>
    [Fact]
    public void Reads_a_session_record_name()
    {
        bool parsed = SessionRecordWatcher.TryParseName(TabText + ".json", out TabId tabId, out bool isApproval);

        Assert.True(parsed);
        Assert.Equal(Tab, tabId);
        Assert.False(isApproval);
    }

    /// <summary>An .approval file named after a tab is its approval signal.</summary>
    [Fact]
    public void Reads_an_approval_signal_name()
    {
        bool parsed = SessionRecordWatcher.TryParseName(TabText + ".approval", out TabId tabId, out bool isApproval);

        Assert.True(parsed);
        Assert.Equal(Tab, tabId);
        Assert.True(isApproval);
    }

    /// <summary>The temporary files of the hook, and anything else, mean nothing.</summary>
    [Theory]
    [InlineData(".tmp.a1b2c3")]
    [InlineData("flash-trace.log")]
    [InlineData("not-a-tab.json")]
    [InlineData("not-a-tab.approval")]
    [InlineData(TabText)]
    [InlineData("")]
    [InlineData(null)]
    public void Ignores_other_files(string? fileName)
    {
        Assert.False(SessionRecordWatcher.TryParseName(fileName, out _, out _));
    }
}
