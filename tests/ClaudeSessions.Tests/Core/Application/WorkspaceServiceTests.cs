using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Application.Dto;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ClaudeSessions.Tests.Core.Application;

/// <summary>Use cases: windows and tabs closed and reopened, sessions followed, ordered shutdown.</summary>
public sealed class WorkspaceServiceTests
{
    private const string DefaultCwd = "/home/joff/Sources";

    /// <summary>A first start, with nothing saved, opens one window with one tab in the default directory.</summary>
    [Fact]
    public void Opens_a_new_window_when_none_is_saved()
    {
        Harness harness = new(Workspace.Empty());

        IReadOnlyList<SessionWindow> shown = harness.Service.Load();

        SessionWindow window = Assert.Single(shown);
        Assert.Equal(DefaultCwd, Assert.Single(window.Tabs).Cwd);
        Assert.Equal(1, harness.Store.SaveCount);
    }

    /// <summary>Only the open windows are shown at start; the closed ones stay in the menu.</summary>
    [Fact]
    public void Shows_only_the_open_windows_at_start()
    {
        SessionWindow open = TestData.Window(WindowStatus.Open, TestData.Tab());
        SessionWindow closed = TestData.Window(WindowStatus.Closed, TestData.Tab());
        Harness harness = new(new Workspace([open, closed], []));

        IReadOnlyList<SessionWindow> shown = harness.Service.Load();

        Assert.Equal([open], shown);
    }

    /// <summary>A start with only closed windows opens a new one and keeps the closed ones.</summary>
    [Fact]
    public void Opens_a_new_window_when_every_saved_window_is_closed()
    {
        SessionWindow closed = TestData.Window(WindowStatus.Closed, TestData.Tab());
        Harness harness = new(new Workspace([closed], []));

        IReadOnlyList<SessionWindow> shown = harness.Service.Load();

        Assert.NotEqual(closed.Id, Assert.Single(shown).Id);
        Assert.Contains(closed, harness.Service.Workspace.ClosedWindows);
    }

    /// <summary>A new tab gets fresh tab and session ids, is selected and saved.</summary>
    [Fact]
    public void Adds_a_new_selected_tab()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(window);

        SessionTab tab = harness.Service.NewTab(window.Id, "/home/joff/Sources/comutils");

        Assert.Equal(tab.Id, window.SelectedTabId);
        Assert.NotEqual(tab.Id.Value, tab.SessionId.Value);
        Assert.Equal(1, harness.Store.SaveCount);
    }

    /// <summary>A new tab order is saved at once.</summary>
    [Fact]
    public void Saves_a_new_tab_order()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);
        Harness harness = Harness.Loaded(window);

        bool moved = harness.Service.MoveTab(window.Id, first.Id, 1);

        Assert.True(moved);
        Assert.Equal([second.Id, first.Id], harness.Store.Saved.Windows[0].Tabs.Select(tab => tab.Id));
        Assert.Equal(1, harness.Store.SaveCount);
    }

    /// <summary>A drop that changes nothing writes nothing.</summary>
    [Fact]
    public void Does_not_save_an_unchanged_tab_order()
    {
        SessionTab first = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, TestData.Tab());
        Harness harness = Harness.Loaded(window);

        bool moved = harness.Service.MoveTab(window.Id, first.Id, 0);

        Assert.False(moved);
        Assert.Equal(0, harness.Store.SaveCount);
    }

    /// <summary>Closing a window while others are open puts it aside.</summary>
    [Fact]
    public void Puts_a_window_aside_when_others_are_open()
    {
        SessionWindow closing = TestData.Window(WindowStatus.Open, TestData.Tab());
        SessionWindow other = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(closing, other);

        WindowCloseOutcome outcome = harness.Service.CloseWindow(closing.Id);

        Assert.Equal(WindowCloseOutcome.Closed, outcome);
        Assert.Equal(WindowStatus.Closed, closing.Status);
        Assert.Equal(1, harness.Store.SaveCount);
    }

    /// <summary>Closing the last open window quits the application and keeps the window open for the next start.</summary>
    [Fact]
    public void Quits_when_the_last_open_window_closes()
    {
        SessionWindow last = TestData.Window(WindowStatus.Open, TestData.Tab());
        SessionWindow closed = TestData.Window(WindowStatus.Closed, TestData.Tab());
        Harness harness = Harness.Loaded(last, closed);

        WindowCloseOutcome outcome = harness.Service.CloseWindow(last.Id);

        Assert.Equal(WindowCloseOutcome.QuitApplication, outcome);
        Assert.Equal(WindowStatus.Open, last.Status);
    }

    /// <summary>A window left without tabs is dropped instead of being put aside.</summary>
    [Fact]
    public void Drops_a_window_without_tabs()
    {
        SessionWindow empty = TestData.Window(WindowStatus.Open);
        SessionWindow other = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(empty, other);

        WindowCloseOutcome outcome = harness.Service.CloseWindow(empty.Id);

        Assert.Equal(WindowCloseOutcome.Closed, outcome);
        Assert.Null(harness.Service.Workspace.FindWindow(empty.Id));
    }

    /// <summary>During the shutdown, WPF closing every window changes no status.</summary>
    [Fact]
    public void Keeps_windows_open_when_they_close_during_the_shutdown()
    {
        SessionWindow first = TestData.Window(WindowStatus.Open, TestData.Tab());
        SessionWindow second = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(first, second);
        harness.Service.BeginShutdown();

        WindowCloseOutcome outcome = harness.Service.CloseWindow(first.Id);

        Assert.Equal(WindowCloseOutcome.Ignored, outcome);
        Assert.Equal(WindowStatus.Open, first.Status);
        Assert.Equal(0, harness.Store.SaveCount);
    }

    /// <summary>A session ending with code 0 (/exit) closes its tab and keeps it for reopening.</summary>
    [Fact]
    public void Closes_a_tab_whose_session_exits_normally()
    {
        SessionTab tab = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, tab, TestData.Tab());
        Harness harness = Harness.Loaded(window);

        TabExitOutcome outcome = harness.Service.OnTabExited(window.Id, tab.Id, 0);

        Assert.Equal(TabExitOutcome.TabClosed, outcome);
        Assert.False(window.Contains(tab.Id));
        Assert.Same(tab, harness.Service.Workspace.ClosedTabs[^1].Tab);
    }

    /// <summary>A session that fails keeps its tab, for the banner.</summary>
    [Fact]
    public void Keeps_a_tab_whose_session_fails()
    {
        SessionTab tab = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, tab);
        Harness harness = Harness.Loaded(window);

        TabExitOutcome outcome = harness.Service.OnTabExited(window.Id, tab.Id, 1);

        Assert.Equal(TabExitOutcome.KeepWithError, outcome);
        Assert.True(window.Contains(tab.Id));
    }

    /// <summary>A process ending with code 0 during the shutdown keeps its tab.</summary>
    [Fact]
    public void Keeps_a_tab_whose_process_ends_during_the_shutdown()
    {
        SessionTab tab = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, tab);
        Harness harness = Harness.Loaded(window);
        harness.Service.BeginShutdown();

        TabExitOutcome outcome = harness.Service.OnTabExited(window.Id, tab.Id, 0);

        Assert.Equal(TabExitOutcome.Ignored, outcome);
        Assert.True(window.Contains(tab.Id));
    }

    /// <summary>The end of a process whose tab is already gone changes nothing.</summary>
    [Fact]
    public void Ignores_the_exit_of_a_tab_already_gone()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(window);

        TabExitOutcome outcome = harness.Service.OnTabExited(window.Id, TestData.Tab().Id, 0);

        Assert.Equal(TabExitOutcome.Ignored, outcome);
    }

    /// <summary>A tab pushed out of the closed-tab stack loses its hook record.</summary>
    [Fact]
    public void Forgets_the_record_of_a_tab_pushed_out_of_the_stack()
    {
        SessionTab[] tabs = Enumerable.Range(0, Workspace.ClosedTabCapacity + 1).Select(_ => TestData.Tab()).ToArray();
        SessionWindow window = TestData.Window(WindowStatus.Open, tabs);
        Harness harness = Harness.Loaded(window);

        foreach (SessionTab tab in tabs)
        {
            harness.Service.CloseTab(window.Id, tab.Id);
        }

        Assert.Equal([tabs[0].Id], harness.Source.Forgotten);
    }

    /// <summary>The last closed tab comes back in the window that asks for it.</summary>
    [Fact]
    public void Reopens_the_last_closed_tab()
    {
        SessionTab tab = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, tab, TestData.Tab());
        Harness harness = Harness.Loaded(window);
        harness.Service.CloseTab(window.Id, tab.Id);

        SessionTab? reopened = harness.Service.ReopenClosedTab(window.Id);

        Assert.Same(tab, reopened);
        Assert.Equal(tab.Id, window.SelectedTabId);
    }

    /// <summary>Asking for a closed tab when none is kept gives nothing.</summary>
    [Fact]
    public void Reopens_nothing_when_no_tab_was_closed()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(window);

        SessionTab? reopened = harness.Service.ReopenClosedTab(window.Id);

        Assert.Null(reopened);
    }

    /// <summary>A closed window reopens with its tabs, in their order.</summary>
    [Fact]
    public void Reopens_a_closed_window_with_its_tabs()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow closed = TestData.Window(WindowStatus.Closed, first, second);
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, TestData.Tab()), closed);

        SessionWindow reopened = harness.Service.ReopenWindow(closed.Id);

        Assert.Equal(WindowStatus.Open, reopened.Status);
        Assert.Equal([first.Id, second.Id], reopened.Tabs.Select(tab => tab.Id));
    }

    /// <summary>Forgetting a closed window drops it with the hook records of its tabs.</summary>
    [Fact]
    public void Forgets_a_closed_window_and_its_records()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow closed = TestData.Window(WindowStatus.Closed, first, second);
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, TestData.Tab()), closed);

        harness.Service.ForgetWindow(closed.Id);

        Assert.Null(harness.Service.Workspace.FindWindow(closed.Id));
        Assert.Equal([first.Id, second.Id], harness.Source.Forgotten);
    }

    /// <summary>« Nouvelle session ici » gives the tab a new session and drops its old title.</summary>
    [Fact]
    public void Renews_the_session_of_a_tab()
    {
        SessionTab tab = TestData.Tab(title: "old");
        SessionId previous = tab.SessionId;
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, tab));

        harness.Service.RenewSession(tab.Id);

        Assert.NotEqual(previous, tab.SessionId);
        Assert.Null(tab.Title);
    }

    /// <summary>The launch request carries the tab, its current session and its directory.</summary>
    [Fact]
    public void Builds_the_launch_request_of_a_tab()
    {
        SessionTab tab = TestData.Tab("/home/joff/Sources/ck-widget");
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, tab));

        TabLaunchRequest request = harness.Service.LaunchRequestFor(tab.Id);

        Assert.Equal(new TabLaunchRequest(tab.Id, tab.SessionId, "/home/joff/Sources/ck-widget"), request);
    }

    /// <summary>A launch request for an unknown tab is a programming error.</summary>
    [Fact]
    public void Refuses_a_launch_request_for_an_unknown_tab()
    {
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, TestData.Tab()));

        Assert.Throws<InvalidOperationException>(() => harness.Service.LaunchRequestFor(TestData.Tab().Id));
    }

    /// <summary>A session change recorded by the hook (/clear, /resume) is followed.</summary>
    [Fact]
    public async Task Follows_a_session_change_recorded_by_the_hook()
    {
        SessionTab tab = TestData.Tab();
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, tab));
        SessionId cleared = new(Guid.NewGuid());
        harness.Source.Answer(tab.Id, cleared);

        await harness.Service.SynchronizeAsync([tab.Id]);

        Assert.Equal(cleared, tab.SessionId);
    }

    /// <summary>A failed read keeps the last known session.</summary>
    [Fact]
    public async Task Keeps_the_last_known_session_when_the_read_fails()
    {
        SessionTab tab = TestData.Tab();
        SessionId known = tab.SessionId;
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, tab));
        harness.Source.Fail(tab.Id);

        await harness.Service.SynchronizeAsync([tab.Id]);

        Assert.Equal(known, tab.SessionId);
    }

    /// <summary>Once the shutdown has begun, ordinary saves do nothing.</summary>
    [Fact]
    public void Skips_ordinary_saves_during_the_shutdown()
    {
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, TestData.Tab()));
        harness.Service.BeginShutdown();

        harness.Service.Save();

        Assert.Equal(0, harness.Store.SaveCount);
    }

    /// <summary>The final save happens once, however many times it is asked for.</summary>
    [Fact]
    public void Saves_only_once_at_the_end()
    {
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, TestData.Tab()));
        harness.Service.BeginShutdown();

        harness.Service.SaveFinal();
        harness.Service.SaveFinal();

        Assert.Equal(1, harness.Store.SaveCount);
    }

    /// <summary>Window moves during the shutdown are not recorded.</summary>
    [Fact]
    public void Ignores_window_moves_during_the_shutdown()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open, TestData.Tab());
        Harness harness = Harness.Loaded(window);
        harness.Service.BeginShutdown();

        harness.Service.UpdateBounds(window.Id, new WindowBounds(0, 0, 10, 10, false));

        Assert.Equal(WindowBounds.Default, window.Bounds);
    }

    /// <summary>A title for a tab that no longer exists is ignored.</summary>
    [Fact]
    public void Ignores_a_title_for_an_unknown_tab()
    {
        SessionTab tab = TestData.Tab();
        Harness harness = Harness.Loaded(TestData.Window(WindowStatus.Open, tab));

        harness.Service.UpdateTitle(TestData.Tab().Id, "stray");

        Assert.Null(tab.Title);
    }

    // category N/A: malicious — inputs come from the application's own objects; hostile file contents are tested in the adapters.

    /// <summary>A service wired to in-memory fakes.</summary>
    private sealed class Harness
    {
        /// <summary>Creates the harness around a saved workspace.</summary>
        /// <param name="saved">The workspace the store returns.</param>
        public Harness(Workspace saved)
        {
            Store = new InMemoryWorkspaceStore(saved);
            Source = new FakeTabSessionSource();
            FakeTimeProvider clock = new(TestData.Now);
            Service = new WorkspaceService(
                Store,
                Source,
                new SessionSynchronizer(Source, clock, SessionSynchronizer.DefaultTimeout),
                new SequentialIdGenerator(),
                clock,
                DefaultCwd);
        }

        /// <summary>The store.</summary>
        public InMemoryWorkspaceStore Store { get; }

        /// <summary>The session source.</summary>
        public FakeTabSessionSource Source { get; }

        /// <summary>The service under test.</summary>
        public WorkspaceService Service { get; }

        /// <summary>Creates a harness whose service has loaded the given windows, with the save counter reset.</summary>
        /// <param name="windows">The saved windows; at least one is open.</param>
        /// <returns>The harness.</returns>
        public static Harness Loaded(params SessionWindow[] windows)
        {
            Harness harness = new(new Workspace(windows, []));
            harness.Service.Load();
            harness.Store.ResetCount();
            return harness;
        }
    }
}
