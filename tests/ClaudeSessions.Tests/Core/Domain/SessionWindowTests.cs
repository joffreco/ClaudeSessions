using System.Linq;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>Tabs added, removed and selected in a window.</summary>
public sealed class SessionWindowTests
{
    /// <summary>A new window selects its first tab.</summary>
    [Fact]
    public void Selects_the_first_tab_by_default()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();

        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        Assert.Equal(first.Id, window.SelectedTabId);
    }

    /// <summary>A saved selection pointing to a missing tab falls back to the first tab.</summary>
    [Fact]
    public void Ignores_a_selection_outside_the_window()
    {
        SessionTab first = TestData.Tab();

        SessionWindow window = new(new WindowId(System.Guid.NewGuid()), WindowStatus.Open, WindowBounds.Default, [first], new TabId(System.Guid.NewGuid()));

        Assert.Equal(first.Id, window.SelectedTabId);
    }

    /// <summary>A new tab goes right after the selected one and becomes selected.</summary>
    [Fact]
    public void Adds_a_tab_after_the_selected_one()
    {
        SessionTab first = TestData.Tab();
        SessionTab last = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, last);
        SessionTab added = TestData.Tab();

        window.AddTab(added);

        Assert.Equal([first.Id, added.Id, last.Id], window.Tabs.Select(tab => tab.Id));
        Assert.Equal(added.Id, window.SelectedTabId);
    }

    /// <summary>Removing the selected tab selects the tab that took its place.</summary>
    [Fact]
    public void Selects_the_next_tab_after_removing_the_selected_one()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        SessionTab? removed = window.RemoveTab(first.Id);

        Assert.Same(first, removed);
        Assert.Equal(second.Id, window.SelectedTabId);
    }

    /// <summary>Removing the last tab of the strip selects the one before it.</summary>
    [Fact]
    public void Selects_the_previous_tab_after_removing_the_last_one()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);
        window.Select(second.Id);

        window.RemoveTab(second.Id);

        Assert.Equal(first.Id, window.SelectedTabId);
    }

    /// <summary>Removing the only tab leaves no selection.</summary>
    [Fact]
    public void Leaves_no_selection_when_the_window_is_empty()
    {
        SessionTab only = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, only);

        window.RemoveTab(only.Id);

        Assert.Empty(window.Tabs);
        Assert.Null(window.SelectedTabId);
    }

    /// <summary>Removing a tab of another window changes nothing.</summary>
    [Fact]
    public void Ignores_the_removal_of_a_foreign_tab()
    {
        SessionTab own = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, own);

        SessionTab? removed = window.RemoveTab(TestData.Tab().Id);

        Assert.Null(removed);
        Assert.Single(window.Tabs);
    }

    /// <summary>Selecting a tab of another window keeps the current selection.</summary>
    [Fact]
    public void Ignores_the_selection_of_a_foreign_tab()
    {
        SessionTab own = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, own);

        window.Select(TestData.Tab().Id);

        Assert.Equal(own.Id, window.SelectedTabId);
    }

    /// <summary>Moving to the next tab wraps from the last to the first.</summary>
    [Fact]
    public void Wraps_forward_from_the_last_tab()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);
        window.Select(second.Id);

        window.SelectNeighbour(1);

        Assert.Equal(first.Id, window.SelectedTabId);
    }

    /// <summary>Moving to the previous tab wraps from the first to the last.</summary>
    [Fact]
    public void Wraps_backward_from_the_first_tab()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        window.SelectNeighbour(-1);

        Assert.Equal(second.Id, window.SelectedTabId);
    }

    /// <summary>Moving between tabs of an empty window does nothing.</summary>
    [Fact]
    public void Does_nothing_when_moving_in_an_empty_window()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open);

        window.SelectNeighbour(1);

        Assert.Null(window.SelectedTabId);
    }

    /// <summary>A tab dragged to the right lands at the place it is dropped.</summary>
    [Fact]
    public void Moves_a_tab_to_the_right()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionTab third = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second, third);

        bool moved = window.MoveTab(first.Id, 2);

        Assert.True(moved);
        Assert.Equal([second.Id, third.Id, first.Id], window.Tabs.Select(tab => tab.Id));
    }

    /// <summary>A tab dragged to the left lands at the place it is dropped.</summary>
    [Fact]
    public void Moves_a_tab_to_the_left()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionTab third = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second, third);

        window.MoveTab(third.Id, 1);

        Assert.Equal([first.Id, third.Id, second.Id], window.Tabs.Select(tab => tab.Id));
    }

    /// <summary>Moving a tab keeps the selected tab selected.</summary>
    [Fact]
    public void Keeps_the_selection_when_moving_a_tab()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);
        window.Select(second.Id);

        window.MoveTab(first.Id, 1);

        Assert.Equal(second.Id, window.SelectedTabId);
    }

    /// <summary>A tab of another window is not moved.</summary>
    [Fact]
    public void Ignores_the_move_of_a_foreign_tab()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        bool moved = window.MoveTab(TestData.Tab().Id, 0);

        Assert.False(moved);
        Assert.Equal([first.Id, second.Id], window.Tabs.Select(tab => tab.Id));
    }

    /// <summary>A tab dropped where it already is reports no change.</summary>
    [Fact]
    public void Reports_no_change_when_dropped_in_place()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        bool moved = window.MoveTab(second.Id, 1);

        Assert.False(moved);
    }

    /// <summary>A place past the end puts the tab last.</summary>
    [Fact]
    public void Puts_a_tab_last_for_a_place_past_the_end()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        window.MoveTab(first.Id, int.MaxValue);

        Assert.Equal([second.Id, first.Id], window.Tabs.Select(tab => tab.Id));
    }

    /// <summary>A negative place puts the tab first.</summary>
    [Fact]
    public void Puts_a_tab_first_for_a_negative_place()
    {
        SessionTab first = TestData.Tab();
        SessionTab second = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, first, second);

        window.MoveTab(second.Id, int.MinValue);

        Assert.Equal([second.Id, first.Id], window.Tabs.Select(tab => tab.Id));
    }

    /// <summary>The only tab of a window has nowhere to go.</summary>
    [Fact]
    public void Leaves_a_lone_tab_in_place()
    {
        SessionTab only = TestData.Tab();
        SessionWindow window = TestData.Window(WindowStatus.Open, only);

        bool moved = window.MoveTab(only.Id, 3);

        Assert.False(moved);
        Assert.Equal([only.Id], window.Tabs.Select(tab => tab.Id));
    }

    /// <summary>Moving a tab in an empty window does nothing.</summary>
    [Fact]
    public void Ignores_a_move_in_an_empty_window()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open);

        bool moved = window.MoveTab(TestData.Tab().Id, 0);

        Assert.False(moved);
    }

    // category N/A: malicious — the window only holds ids and tabs built by the application.
}
