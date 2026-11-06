using System;
using System.Collections.Generic;
using System.Linq;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Application.Dto;
using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Core.Application;

/// <summary>Filtering and ordering the tabs of the quick switcher.</summary>
public sealed class TabSwitchListTests
{
    private static TabSwitchEntry Entry(string title, string cwd = "/home/joff/Projets", TabAttention attention = TabAttention.None)
    {
        return new TabSwitchEntry(new TabId(Guid.NewGuid()), title, cwd, attention);
    }

    private static List<string> Titles(IEnumerable<TabSwitchEntry> entries)
    {
        return entries.Select(entry => entry.Title).ToList();
    }

    /// <summary>What needs an answer comes first, then what is finished, then the rest.</summary>
    [Fact]
    public void Puts_what_expects_the_user_first()
    {
        TabSwitchEntry[] entries = [Entry("plain"), Entry("finished", attention: TabAttention.Finished), Entry("approval", attention: TabAttention.Approval)];

        IReadOnlyList<TabSwitchEntry> arranged = TabSwitchList.Arrange(entries, "");

        Assert.Equal(["approval", "finished", "plain"], Titles(arranged));
    }

    /// <summary>Tabs that expect the same thing stay in the given order.</summary>
    [Fact]
    public void Keeps_the_given_order_among_equals()
    {
        TabSwitchEntry[] entries = [Entry("b"), Entry("a"), Entry("c")];

        Assert.Equal(["b", "a", "c"], Titles(TabSwitchList.Arrange(entries, "")));
    }

    /// <summary>The filter looks at the title.</summary>
    [Fact]
    public void Filters_on_the_title()
    {
        TabSwitchEntry[] entries = [Entry("Fix the hook"), Entry("Write the docs")];

        Assert.Equal(["Fix the hook"], Titles(TabSwitchList.Arrange(entries, "hook")));
    }

    /// <summary>The filter looks at the directory too.</summary>
    [Fact]
    public void Filters_on_the_directory()
    {
        TabSwitchEntry[] entries = [Entry("one", "/home/joff/Projets/HomeBase"), Entry("two", "/home/joff/Projets/ecole")];

        Assert.Equal(["two"], Titles(TabSwitchList.Arrange(entries, "ecole")));
    }

    /// <summary>Case and accents do not matter, in either direction.</summary>
    [Theory]
    [InlineData("ecole", "L'école Branché")]
    [InlineData("ÉCOLE", "L'ecole branche")]
    public void Ignores_case_and_accents(string filter, string title)
    {
        Assert.Single(TabSwitchList.Arrange([Entry(title)], filter));
    }

    /// <summary>Every word must match, each in the title or the directory.</summary>
    [Fact]
    public void Requires_every_word()
    {
        TabSwitchEntry[] entries = [Entry("Fix the hook", "/p/ClaudeSessions"), Entry("Fix the page", "/p/HomeBase")];

        Assert.Equal(["Fix the hook"], Titles(TabSwitchList.Arrange(entries, "fix claude")));
    }

    /// <summary>Spaces alone filter nothing.</summary>
    [Fact]
    public void Treats_blanks_as_no_filter()
    {
        Assert.Single(TabSwitchList.Arrange([Entry("one")], "   "));
    }

    /// <summary>A filter that matches nothing gives an empty list.</summary>
    [Fact]
    public void Gives_nothing_when_nothing_matches()
    {
        Assert.Empty(TabSwitchList.Arrange([Entry("one")], "zzz"));
    }
}
