using System.Collections.Generic;
using System.Linq;

namespace ClaudeSessions.Core.Domain;

/// <summary>Every window of the application, open or closed, and the tabs recently closed.</summary>
public sealed class Workspace
{
    /// <summary>Number of closed tabs kept for reopening.</summary>
    public const int ClosedTabCapacity = 10;

    private readonly List<SessionWindow> _windows;
    private readonly List<ClosedTab> _closedTabs;

    /// <summary>Creates a workspace.</summary>
    /// <param name="windows">Windows, open and closed, in creation order.</param>
    /// <param name="closedTabs">Closed tabs, most recent last; only the last <see cref="ClosedTabCapacity"/> are kept.</param>
    public Workspace(IEnumerable<SessionWindow> windows, IEnumerable<ClosedTab> closedTabs)
    {
        _windows = windows.ToList();
        _closedTabs = closedTabs.TakeLast(ClosedTabCapacity).ToList();
    }

    /// <summary>An empty workspace.</summary>
    /// <returns>A workspace without windows or closed tabs.</returns>
    public static Workspace Empty()
    {
        return new Workspace([], []);
    }

    /// <summary>Windows, open and closed, in creation order.</summary>
    public IReadOnlyList<SessionWindow> Windows => _windows;

    /// <summary>Closed tabs, most recent last.</summary>
    public IReadOnlyList<ClosedTab> ClosedTabs => _closedTabs;

    /// <summary>Windows currently on screen.</summary>
    public IEnumerable<SessionWindow> OpenWindows => _windows.Where(window => window.Status == WindowStatus.Open);

    /// <summary>Windows put aside, listed for reopening.</summary>
    public IEnumerable<SessionWindow> ClosedWindows => _windows.Where(window => window.Status == WindowStatus.Closed);

    /// <summary>Finds a window.</summary>
    /// <param name="windowId">The window to look for.</param>
    /// <returns>The window, or null when it does not exist.</returns>
    public SessionWindow? FindWindow(WindowId windowId)
    {
        return _windows.FirstOrDefault(window => window.Id == windowId);
    }

    /// <summary>Adds a window.</summary>
    /// <param name="window">The window to add.</param>
    public void AddWindow(SessionWindow window)
    {
        _windows.Add(window);
    }

    /// <summary>Removes a window for good.</summary>
    /// <param name="windowId">The window to remove.</param>
    /// <returns>The removed window, or null when it does not exist.</returns>
    public SessionWindow? RemoveWindow(WindowId windowId)
    {
        SessionWindow? window = FindWindow(windowId);
        if (window is not null)
        {
            _windows.Remove(window);
        }
        return window;
    }

    /// <summary>Keeps a closed tab for reopening, dropping the oldest ones beyond the capacity.</summary>
    /// <param name="closedTab">The tab just closed.</param>
    /// <returns>The tabs dropped to make room, oldest first.</returns>
    public IReadOnlyList<ClosedTab> PushClosedTab(ClosedTab closedTab)
    {
        _closedTabs.Add(closedTab);
        int overflow = _closedTabs.Count - ClosedTabCapacity;
        if (overflow <= 0)
        {
            return [];
        }
        List<ClosedTab> evicted = _closedTabs.GetRange(0, overflow);
        _closedTabs.RemoveRange(0, overflow);
        return evicted;
    }

    /// <summary>Takes back the most recently closed tab.</summary>
    /// <returns>The tab, or null when none is kept.</returns>
    public ClosedTab? PopClosedTab()
    {
        if (_closedTabs.Count == 0)
        {
            return null;
        }
        ClosedTab last = _closedTabs[^1];
        _closedTabs.RemoveAt(_closedTabs.Count - 1);
        return last;
    }
}
