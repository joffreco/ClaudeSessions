using System.Linq;
using System.Collections.Generic;
using ClaudeSessions.Core.Consignes.Application;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Consignes.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Core;

/// <summary>Arranging the consignes into groups.</summary>
public sealed class ConsigneBoardTests
{
    /// <summary>One group per folder, newest consigne first, newest group first, closed tabs last.</summary>
    [Fact]
    public void GroupsAndOrders()
    {
        Consigne[] consignes =
        [
            Make.Consigne("open-old", "a", 1, tabId: "open-old"),
            Make.Consigne("closed", "b", 9, tabId: "closed"),
            Make.Consigne("open-new", "c", 2, tabId: "open-new"),
            Make.Consigne("open-new", "d", 5, tabId: "open-new"),
        ];
        Dictionary<string, TabInfo> tabs = new()
        {
            ["open-old"] = new TabInfo("vieux", true),
            ["open-new"] = new TabInfo("neuf", true),
            ["closed"] = new TabInfo("fermé", false),
        };

        IReadOnlyList<TabGroup> groups = ConsigneBoard.Build(consignes, tabs, new ReadingState());

        Assert.Equal(["neuf", "vieux", "fermé"], groups.Select(group => group.Label));
        Assert.Equal(["d", "c"], groups[0].Consignes.Select(consigne => consigne.Key.Id));
        Assert.True(groups[2].IsClosed);
    }

    /// <summary>A tab ClaudeSessions no longer knows is closed; unknown tabs are not when the file cannot be read.</summary>
    [Fact]
    public void ClosedOnlyWhenKnown()
    {
        Consigne[] consignes = [Make.Consigne("gone", "a", tabId: "gone"), Make.Consigne("_sans-onglet/x", "b", tabId: "")];

        IReadOnlyList<TabGroup> known = ConsigneBoard.Build(consignes, new Dictionary<string, TabInfo>(), new ReadingState());
        IReadOnlyList<TabGroup> unknown = ConsigneBoard.Build(consignes, null, new ReadingState());

        Assert.Equal([false, true], known.Select(group => group.IsClosed));
        Assert.All(unknown, group => Assert.False(group.IsClosed));
    }

    /// <summary>The unread count drops once a consigne is seen, and comes back when it is updated.</summary>
    [Fact]
    public void CountsUnread()
    {
        Consigne first = Make.Consigne("t", "a", 0);
        ReadingState state = new();

        Assert.Equal(1, ConsigneBoard.Build([first], null, state)[0].UnreadCount);
        state.MarkSeen(first);
        Assert.Equal(0, ConsigneBoard.Build([first], null, state)[0].UnreadCount);
        Assert.Equal(1, ConsigneBoard.Build([first with { Updated = first.Updated.AddMinutes(1) }], null, state)[0].UnreadCount);
    }
}
