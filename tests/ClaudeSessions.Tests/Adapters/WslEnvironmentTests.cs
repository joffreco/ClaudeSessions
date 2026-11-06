using ClaudeSessions.Adapters;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Passing the records folder on to WSL.</summary>
public sealed class WslEnvironmentTests
{
    /// <summary>With nothing forwarded yet, the entry stands alone and asks for a path translation.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Starts_the_list_when_unset(string? current)
    {
        Assert.Equal("CLAUDE_SESSIONS_RECORDS/p", WslEnvironment.ForwardRecords(current));
    }

    /// <summary>The entries of other programs are kept.</summary>
    [Fact]
    public void Keeps_the_other_entries()
    {
        Assert.Equal("USERPROFILE/p:FOO:CLAUDE_SESSIONS_RECORDS/p", WslEnvironment.ForwardRecords("USERPROFILE/p:FOO"));
    }

    /// <summary>An app started by the updater inherits the entry and must not add it twice.</summary>
    [Fact]
    public void Does_not_add_the_entry_twice()
    {
        Assert.Equal("FOO:CLAUDE_SESSIONS_RECORDS/p", WslEnvironment.ForwardRecords("FOO:CLAUDE_SESSIONS_RECORDS/p"));
    }
}
