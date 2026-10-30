using System.Linq;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>Windows and the stack of closed tabs of the workspace.</summary>
public sealed class WorkspaceTests
{
    /// <summary>Open and closed windows are listed apart.</summary>
    [Fact]
    public void Lists_open_and_closed_windows_apart()
    {
        SessionWindow open = TestData.Window(WindowStatus.Open, TestData.Tab());
        SessionWindow closed = TestData.Window(WindowStatus.Closed, TestData.Tab());

        Workspace workspace = new([open, closed], []);

        Assert.Equal([open], workspace.OpenWindows);
        Assert.Equal([closed], workspace.ClosedWindows);
    }

    /// <summary>The most recently closed tab comes back first.</summary>
    [Fact]
    public void Pops_the_most_recently_closed_tab()
    {
        Workspace workspace = Workspace.Empty();
        WindowId windowId = new(System.Guid.NewGuid());
        ClosedTab older = new(TestData.Tab(), windowId, TestData.Now);
        ClosedTab newer = new(TestData.Tab(), windowId, TestData.Now);
        workspace.PushClosedTab(older);
        workspace.PushClosedTab(newer);

        ClosedTab? popped = workspace.PopClosedTab();

        Assert.Same(newer, popped);
        Assert.Equal([older], workspace.ClosedTabs);
    }

    /// <summary>An empty stack gives nothing back.</summary>
    [Fact]
    public void Pops_nothing_from_an_empty_stack()
    {
        Workspace workspace = Workspace.Empty();

        ClosedTab? popped = workspace.PopClosedTab();

        Assert.Null(popped);
    }

    /// <summary>The stack keeps ten tabs and hands back the oldest one it drops.</summary>
    [Fact]
    public void Drops_the_oldest_tab_beyond_the_capacity()
    {
        Workspace workspace = Workspace.Empty();
        WindowId windowId = new(System.Guid.NewGuid());
        ClosedTab[] closed = Enumerable.Range(0, Workspace.ClosedTabCapacity + 1).Select(_ => new ClosedTab(TestData.Tab(), windowId, TestData.Now)).ToArray();
        foreach (ClosedTab tab in closed.Take(Workspace.ClosedTabCapacity))
        {
            workspace.PushClosedTab(tab);
        }

        System.Collections.Generic.IReadOnlyList<ClosedTab> evicted = workspace.PushClosedTab(closed[^1]);

        Assert.Equal([closed[0]], evicted);
        Assert.Equal(Workspace.ClosedTabCapacity, workspace.ClosedTabs.Count);
    }

    /// <summary>A saved stack longer than the capacity keeps only its most recent tabs.</summary>
    [Fact]
    public void Trims_a_loaded_stack_to_the_capacity()
    {
        WindowId windowId = new(System.Guid.NewGuid());
        ClosedTab[] closed = Enumerable.Range(0, Workspace.ClosedTabCapacity + 3).Select(_ => new ClosedTab(TestData.Tab(), windowId, TestData.Now)).ToArray();

        Workspace workspace = new([], closed);

        Assert.Equal(closed.TakeLast(Workspace.ClosedTabCapacity), workspace.ClosedTabs);
    }

    /// <summary>Removing a window that does not exist gives nothing back.</summary>
    [Fact]
    public void Removes_nothing_for_an_unknown_window()
    {
        Workspace workspace = Workspace.Empty();

        SessionWindow? removed = workspace.RemoveWindow(new WindowId(System.Guid.NewGuid()));

        Assert.Null(removed);
    }

    // category N/A: malicious — the workspace only holds objects built by the application or validated by the mapper.
}
