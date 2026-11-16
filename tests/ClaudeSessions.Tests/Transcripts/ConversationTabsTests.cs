using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClaudeSessions.Adapters.Transcripts;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Application;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>Listing the tabs of the open ClaudeSessions windows for the Conversations app.</summary>
public sealed class ConversationTabsTests
{
    private const string TabA = "11111111-0000-0000-0000-00000000000a";
    private const string TabB = "11111111-0000-0000-0000-00000000000b";
    private const string TabC = "11111111-0000-0000-0000-00000000000c";
    private const string SessionA = "aaaaaaaa-0000-0000-0000-00000000000a";
    private const string SessionB = "aaaaaaaa-0000-0000-0000-00000000000b";
    private const string SessionC = "aaaaaaaa-0000-0000-0000-00000000000c";
    private const string Window1 = "99999999-0000-0000-0000-000000000001";
    private const string Window2 = "99999999-0000-0000-0000-000000000002";
    private const string Window3 = "99999999-0000-0000-0000-000000000003";

    /// <summary>Only open windows count, numbered in order; a tab without title shows its folder.</summary>
    [Fact]
    public void Reads_the_tabs_of_the_open_windows()
    {
        using TempFolder folder = new();
        string file = folder.Write("workspace.json", $$"""
            {"version":1,"windows":[
              {"id":"{{Window1}}","status":"Open","tabs":[{"id":"{{TabA}}","cwd":"/home/joff/Projets/Chorale","sessionId":"{{SessionA}}","title":"Chorale"}]},
              {"id":"{{Window2}}","status":"Closed","tabs":[{"id":"{{TabB}}","cwd":"/home/joff","sessionId":"{{SessionB}}","title":"fermé"}]},
              {"id":"{{Window3}}","status":"Open","tabs":[{"id":"{{TabC}}","cwd":"/home/joff/Projets/ecole","sessionId":"{{SessionC}}","title":null},{"id":"pas un id","cwd":"/x","sessionId":"{{SessionC}}"}]},
              {"id":"pas un id","status":"Open","tabs":[{"id":"{{TabB}}","cwd":"/home/joff","sessionId":"{{SessionB}}","title":"sans fenêtre"}]}
            ],"closedTabs":[]}
            """);

        IReadOnlyList<ConversationTab>? tabs = new WorkspaceConversationTabSource(file).Read();

        Assert.NotNull(tabs);
        Assert.Equal(
            [new ConversationTab(Tab(TabA), "Chorale", "/home/joff/Projets/Chorale", Session(SessionA), WindowOf(Window1), 1),
             new ConversationTab(Tab(TabC), "ecole", "/home/joff/Projets/ecole", Session(SessionC), WindowOf(Window3), 2)],
            tabs);
    }

    /// <summary>A missing or broken file gives null and is left in place, unlike the store of the app.</summary>
    [Fact]
    public void Leaves_an_unreadable_file_alone()
    {
        using TempFolder folder = new();
        string file = folder.Write("workspace.json", "{pas du json");

        Assert.Null(new WorkspaceConversationTabSource(file).Read());
        Assert.Null(new WorkspaceConversationTabSource(folder.File("absent.json")).Read());
        Assert.True(File.Exists(file));
        Assert.False(File.Exists(file + ".bad"));
    }

    /// <summary>The session the hook just recorded wins over the one ClaudeSessions last saved.</summary>
    [Fact]
    public async Task Prefers_the_session_recorded_by_the_hook()
    {
        FakeTabSessionSource sessions = new();
        sessions.Answer(Tab(TabA), Session(SessionB));
        sessions.Answer(Tab(TabC), null);
        ConversationTabs tabs = new(
            new StubTabSource([
                new ConversationTab(Tab(TabA), "a", "/a", Session(SessionA), WindowOf(Window1), 1),
                new ConversationTab(Tab(TabC), "c", "/c", Session(SessionC), WindowOf(Window1), 1)]),
            sessions);

        IReadOnlyList<ConversationTab>? read = await tabs.ReadAsync();

        Assert.NotNull(read);
        Assert.Equal([Session(SessionB), Session(SessionC)], [read[0].Session, read[1].Session]);
    }

    /// <summary>An unreadable tab list stays unreadable.</summary>
    [Fact]
    public async Task Reads_nothing_when_the_tab_list_is_unreadable()
    {
        ConversationTabs tabs = new(new StubTabSource(null), new FakeTabSessionSource());

        Assert.Null(await tabs.ReadAsync());
    }

    private static TabId Tab(string id)
    {
        return new TabId(Guid.Parse(id));
    }

    private static SessionId Session(string id)
    {
        return new SessionId(Guid.Parse(id));
    }

    private static WindowId WindowOf(string id)
    {
        return new WindowId(Guid.Parse(id));
    }

    private sealed class StubTabSource(IReadOnlyList<ConversationTab>? tabs) : IConversationTabSource
    {
        public IReadOnlyList<ConversationTab>? Read()
        {
            return tabs;
        }
    }
}
