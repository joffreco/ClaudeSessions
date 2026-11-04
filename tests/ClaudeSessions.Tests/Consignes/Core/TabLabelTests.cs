using ClaudeSessions.Core.Consignes.Application;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Consignes.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Core;

/// <summary>Choosing the name a session folder is shown under.</summary>
public sealed class TabLabelTests
{
    /// <summary>The tab's current title comes first.</summary>
    [Fact]
    public void CurrentTitleFirst()
    {
        Assert.Equal("682 runner1", TabLabel.Of(Make.Consigne("t", "a", tabTitle: "ancien"), new TabInfo("682 runner1", true)));
    }

    /// <summary>An empty or generic current title falls back to the recorded one, then to the folder.</summary>
    /// <param name="current">The tab's current title.</param>
    /// <param name="recorded">The title recorded with the consigne.</param>
    /// <param name="expected">The label.</param>
    [Theory]
    [InlineData(null, "enregistré", "enregistré")]
    [InlineData("Claude Code", "enregistré", "enregistré")]
    [InlineData("", "", "repo")]
    [InlineData("Claude Code", "Claude Code", "repo")]
    public void FallsBack(string? current, string recorded, string expected)
    {
        Assert.Equal(expected, TabLabel.Of(Make.Consigne("t", "a", tabTitle: recorded), new TabInfo(current, true)));
    }

    /// <summary>A session outside a tab is named by its folder and says so.</summary>
    [Fact]
    public void OutsideTab()
    {
        Assert.Equal("repo (hors onglet)", TabLabel.Of(Make.Consigne("_sans-onglet/repo", "a", tabId: ""), null));
    }
}
