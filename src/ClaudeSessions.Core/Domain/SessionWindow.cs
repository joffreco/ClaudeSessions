using System.Collections.Generic;
using System.Linq;

namespace ClaudeSessions.Core.Domain;

/// <summary>A window holding an ordered list of session tabs.</summary>
public sealed class SessionWindow
{
    private readonly List<SessionTab> _tabs;

    /// <summary>Creates a window.</summary>
    /// <param name="id">Identifier of the window.</param>
    /// <param name="status">Whether the window is open or closed.</param>
    /// <param name="bounds">Position and size of the window.</param>
    /// <param name="tabs">Tabs of the window, in display order.</param>
    /// <param name="selectedTabId">Tab shown in the window, if any.</param>
    public SessionWindow(WindowId id, WindowStatus status, WindowBounds bounds, IEnumerable<SessionTab> tabs, TabId? selectedTabId)
    {
        Id = id;
        Status = status;
        Bounds = bounds;
        _tabs = tabs.ToList();
        SelectedTabId = selectedTabId is TabId selected && Contains(selected) ? selected : FirstTabId();
    }

    /// <summary>Identifier of the window.</summary>
    public WindowId Id { get; }

    /// <summary>Whether the window is open or closed.</summary>
    public WindowStatus Status { get; set; }

    /// <summary>Position and size of the window.</summary>
    public WindowBounds Bounds { get; set; }

    /// <summary>Tabs of the window, in display order.</summary>
    public IReadOnlyList<SessionTab> Tabs => _tabs;

    /// <summary>Tab shown in the window; null only when the window has no tab.</summary>
    public TabId? SelectedTabId { get; private set; }

    /// <summary>Tells whether the window holds a tab.</summary>
    /// <param name="tabId">The tab to look for.</param>
    /// <returns>True when the tab belongs to this window.</returns>
    public bool Contains(TabId tabId)
    {
        return _tabs.Any(tab => tab.Id == tabId);
    }

    /// <summary>Finds a tab of the window.</summary>
    /// <param name="tabId">The tab to look for.</param>
    /// <returns>The tab, or null when it is not in this window.</returns>
    public SessionTab? FindTab(TabId tabId)
    {
        return _tabs.FirstOrDefault(tab => tab.Id == tabId);
    }

    /// <summary>Adds a tab after the selected one, or at the end, and selects it.</summary>
    /// <param name="tab">The tab to add.</param>
    public void AddTab(SessionTab tab)
    {
        int selectedIndex = SelectedTabId is TabId selected ? IndexOf(selected) : -1;
        _tabs.Insert(selectedIndex < 0 ? _tabs.Count : selectedIndex + 1, tab);
        SelectedTabId = tab.Id;
    }

    /// <summary>Removes a tab; when it was selected, its neighbour is selected instead.</summary>
    /// <param name="tabId">The tab to remove.</param>
    /// <returns>The removed tab, or null when it is not in this window.</returns>
    public SessionTab? RemoveTab(TabId tabId)
    {
        int index = IndexOf(tabId);
        if (index < 0)
        {
            return null;
        }
        SessionTab removed = _tabs[index];
        _tabs.RemoveAt(index);
        if (SelectedTabId == tabId)
        {
            SelectedTabId = _tabs.Count == 0 ? null : _tabs[System.Math.Min(index, _tabs.Count - 1)].Id;
        }
        return removed;
    }

    /// <summary>Moves a tab to another place in the window; the selection does not change.</summary>
    /// <param name="tabId">The tab to move.</param>
    /// <param name="index">Its new place, counted from 0; a place past either end means that end.</param>
    /// <returns>True when the order changed.</returns>
    public bool MoveTab(TabId tabId, int index)
    {
        int from = IndexOf(tabId);
        if (from < 0)
        {
            return false;
        }
        int to = System.Math.Clamp(index, 0, _tabs.Count - 1);
        if (from == to)
        {
            return false;
        }
        SessionTab tab = _tabs[from];
        _tabs.RemoveAt(from);
        _tabs.Insert(to, tab);
        return true;
    }

    /// <summary>Selects a tab of this window; a tab of another window is ignored.</summary>
    /// <param name="tabId">The tab to show.</param>
    public void Select(TabId tabId)
    {
        if (Contains(tabId))
        {
            SelectedTabId = tabId;
        }
    }

    /// <summary>Selects the tab next to the selected one, wrapping around.</summary>
    /// <param name="step">+1 for the next tab, -1 for the previous one.</param>
    public void SelectNeighbour(int step)
    {
        if (_tabs.Count == 0 || SelectedTabId is not TabId selected)
        {
            return;
        }
        int index = (IndexOf(selected) + step) % _tabs.Count;
        SelectedTabId = _tabs[index < 0 ? index + _tabs.Count : index].Id;
    }

    private int IndexOf(TabId tabId)
    {
        return _tabs.FindIndex(tab => tab.Id == tabId);
    }

    private TabId? FirstTabId()
    {
        return _tabs.Count == 0 ? null : _tabs[0].Id;
    }
}
