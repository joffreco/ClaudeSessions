using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>The text shown on a tab.</summary>
public sealed class SessionTabTests
{
    /// <summary>The session title wins when there is one.</summary>
    [Fact]
    public void Shows_the_session_title()
    {
        SessionTab tab = TestData.Tab("/home/joff/Sources/comutils", "✳ Fix the hook");

        string shown = tab.DisplayTitle;

        Assert.Equal("✳ Fix the hook", shown);
    }

    /// <summary>Without a title, the last folder of the directory is shown.</summary>
    [Fact]
    public void Falls_back_to_the_folder_name()
    {
        SessionTab tab = TestData.Tab("/home/joff/Sources/comutils");

        string shown = tab.DisplayTitle;

        Assert.Equal("comutils", shown);
    }

    /// <summary>A blank title counts as no title.</summary>
    [Fact]
    public void Treats_a_blank_title_as_missing()
    {
        SessionTab tab = TestData.Tab("/home/joff/Sources/comutils", "   ");

        string shown = tab.DisplayTitle;

        Assert.Equal("comutils", shown);
    }

    /// <summary>A trailing slash does not hide the folder name.</summary>
    [Fact]
    public void Ignores_a_trailing_slash()
    {
        SessionTab tab = TestData.Tab("/home/joff/Sources/");

        string shown = tab.DisplayTitle;

        Assert.Equal("Sources", shown);
    }

    /// <summary>The root directory shows as a slash.</summary>
    [Fact]
    public void Shows_the_root_as_a_slash()
    {
        SessionTab tab = TestData.Tab("/");

        string shown = tab.DisplayTitle;

        Assert.Equal("/", shown);
    }

    // category N/A: error and malicious — DisplayTitle has no failure path; OscTitleParser strips control characters upstream.
}
