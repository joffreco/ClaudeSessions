using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>Splitting a session title into its state mark and the title proper.</summary>
public sealed class SessionTitleTests
{
    /// <summary>The ✳ in front of a title is the mark of a session that waits.</summary>
    [Fact]
    public void Reads_the_idle_mark()
    {
        SessionTitle title = SessionTitle.Parse("✳ Fix the hook");

        Assert.Equal(new SessionTitle("✳", "Fix the hook"), title);
        Assert.True(title.IsIdle);
    }

    /// <summary>A spinner frame in front of a title is the mark of a session that works.</summary>
    [Fact]
    public void Reads_a_spinner_mark_as_working()
    {
        SessionTitle title = SessionTitle.Parse("◐ Fix the hook");

        Assert.Equal(new SessionTitle("◐", "Fix the hook"), title);
        Assert.False(title.IsIdle);
    }

    /// <summary>A title without a mark is kept whole.</summary>
    [Fact]
    public void Keeps_a_title_without_a_mark()
    {
        SessionTitle title = SessionTitle.Parse("Fix the hook");

        Assert.Equal(new SessionTitle(null, "Fix the hook"), title);
    }

    /// <summary>A leading word is part of the title, not a mark.</summary>
    [Fact]
    public void Does_not_take_a_leading_letter_for_a_mark()
    {
        SessionTitle title = SessionTitle.Parse("A title");

        Assert.Equal(new SessionTitle(null, "A title"), title);
    }

    /// <summary>Leading punctuation is part of the title, not a mark.</summary>
    [Fact]
    public void Does_not_take_leading_punctuation_for_a_mark()
    {
        SessionTitle title = SessionTitle.Parse("- notes");

        Assert.Equal(new SessionTitle(null, "- notes"), title);
    }

    /// <summary>An empty title has neither mark nor text.</summary>
    [Fact]
    public void Reads_an_empty_title()
    {
        SessionTitle title = SessionTitle.Parse("");

        Assert.Equal(new SessionTitle(null, ""), title);
    }

    /// <summary>A mark alone, with no title after it, stays the text.</summary>
    [Fact]
    public void Keeps_a_lone_mark_as_text()
    {
        SessionTitle title = SessionTitle.Parse("✳ ");

        Assert.Equal(new SessionTitle(null, "✳"), title);
    }

    /// <summary>The emoji variation selector after the mark does not hide it.</summary>
    [Fact]
    public void Reads_a_mark_followed_by_a_variation_selector()
    {
        SessionTitle title = SessionTitle.Parse("✳️ Fix the hook");

        Assert.Equal(new SessionTitle("✳", "Fix the hook"), title);
    }

    /// <summary>A mark outside the basic plane, written as a surrogate pair, is read whole.</summary>
    [Fact]
    public void Reads_a_mark_made_of_a_surrogate_pair()
    {
        SessionTitle title = SessionTitle.Parse("🤖 Fix the hook");

        Assert.Equal(new SessionTitle("🤖", "Fix the hook"), title);
    }

    /// <summary>A braille spinner frame, used by some terminals' spinners, is a mark.</summary>
    [Fact]
    public void Reads_a_braille_spinner_as_a_mark()
    {
        SessionTitle title = SessionTitle.Parse("⠂ Fix the hook");

        Assert.Equal(new SessionTitle("⠂", "Fix the hook"), title);
    }

    /// <summary>A broken surrogate sent by the session is kept as text instead of failing.</summary>
    [Fact]
    public void Keeps_a_lone_surrogate_as_text()
    {
        SessionTitle title = SessionTitle.Parse("\uD83D Fix the hook");

        Assert.Equal(new SessionTitle(null, "\uD83D Fix the hook"), title);
    }
}
