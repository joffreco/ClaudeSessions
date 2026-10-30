using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClaudeSessions.Core.Application.Dto;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Core.Application;

/// <summary>
/// Use cases of the application: windows and tabs opened, closed and reopened, sessions followed,
/// state saved. Once the shutdown has begun, nothing but the session synchronization and the final
/// save changes or saves the state, so every open window comes back at the next start.
/// </summary>
public sealed class WorkspaceService
{
    private readonly IWorkspaceStore _store;
    private readonly ITabSessionSource _sessions;
    private readonly SessionSynchronizer _synchronizer;
    private readonly IIdGenerator _ids;
    private readonly TimeProvider _clock;
    private readonly string _defaultCwd;
    private bool _finalSaveDone;

    /// <summary>Creates the service with an empty workspace; call <see cref="Load"/> next.</summary>
    /// <param name="store">Where the workspace is kept.</param>
    /// <param name="sessions">Where the hook records the session of each tab.</param>
    /// <param name="synchronizer">Reads the recorded sessions with a timeout.</param>
    /// <param name="ids">Source of new identifiers.</param>
    /// <param name="clock">Clock used to date closed tabs.</param>
    /// <param name="defaultCwd">WSL directory of the first tab of a new window.</param>
    public WorkspaceService(
        IWorkspaceStore store,
        ITabSessionSource sessions,
        SessionSynchronizer synchronizer,
        IIdGenerator ids,
        TimeProvider clock,
        string defaultCwd)
    {
        _store = store;
        _sessions = sessions;
        _synchronizer = synchronizer;
        _ids = ids;
        _clock = clock;
        _defaultCwd = defaultCwd;
    }

    /// <summary>The current workspace.</summary>
    public Workspace Workspace { get; private set; } = Workspace.Empty();

    /// <summary>WSL directory of the first tab of a new window.</summary>
    public string DefaultCwd => _defaultCwd;

    /// <summary>True once the application has begun to shut down.</summary>
    public bool IsShuttingDown { get; private set; }

    /// <summary>Loads the saved workspace; when no window is open, opens a new one in the default directory.</summary>
    /// <returns>The windows to show.</returns>
    public IReadOnlyList<SessionWindow> Load()
    {
        Workspace = _store.Load();
        if (!Workspace.OpenWindows.Any())
        {
            Workspace.AddWindow(CreateWindow(_defaultCwd, WindowBounds.Default));
            Save();
        }
        return Workspace.OpenWindows.ToList();
    }

    /// <summary>Opens a new window with one new tab.</summary>
    /// <param name="cwd">WSL directory of the tab.</param>
    /// <param name="bounds">Where to place the window.</param>
    /// <returns>The new window.</returns>
    public SessionWindow NewWindow(string cwd, WindowBounds bounds)
    {
        SessionWindow window = CreateWindow(cwd, bounds);
        Workspace.AddWindow(window);
        Save();
        return window;
    }

    /// <summary>Adds a new tab to a window and selects it.</summary>
    /// <param name="windowId">The window.</param>
    /// <param name="cwd">WSL directory of the new session.</param>
    /// <returns>The new tab.</returns>
    public SessionTab NewTab(WindowId windowId, string cwd)
    {
        SessionTab tab = CreateTab(cwd);
        RequireWindow(windowId).AddTab(tab);
        Save();
        return tab;
    }

    /// <summary>Closes a tab and keeps it for reopening; the tabs pushed out of the stack lose their hook record.</summary>
    /// <param name="windowId">The window of the tab.</param>
    /// <param name="tabId">The tab.</param>
    /// <returns>True when the tab was closed.</returns>
    public bool CloseTab(WindowId windowId, TabId tabId)
    {
        SessionTab? removed = RequireWindow(windowId).RemoveTab(tabId);
        if (removed is null)
        {
            return false;
        }
        IReadOnlyList<ClosedTab> evicted = Workspace.PushClosedTab(new ClosedTab(removed, windowId, _clock.GetUtcNow()));
        foreach (ClosedTab dropped in evicted)
        {
            _sessions.Forget(dropped.Tab.Id);
        }
        Save();
        return true;
    }

    /// <summary>Reopens the most recently closed tab in a window.</summary>
    /// <param name="windowId">The window that receives the tab.</param>
    /// <returns>The reopened tab, or null when no closed tab is kept.</returns>
    public SessionTab? ReopenClosedTab(WindowId windowId)
    {
        SessionWindow window = RequireWindow(windowId);
        ClosedTab? closed = Workspace.PopClosedTab();
        if (closed is null)
        {
            return null;
        }
        window.AddTab(closed.Tab);
        Save();
        return closed.Tab;
    }

    /// <summary>
    /// Closes a window. With other windows open, it is put aside (or dropped when it has no tab);
    /// when it is the last open window, the application quits and the window stays open for the next start.
    /// </summary>
    /// <param name="windowId">The window.</param>
    /// <returns>What the closing leads to.</returns>
    public WindowCloseOutcome CloseWindow(WindowId windowId)
    {
        if (IsShuttingDown)
        {
            return WindowCloseOutcome.Ignored;
        }
        SessionWindow window = RequireWindow(windowId);
        bool othersOpen = Workspace.OpenWindows.Any(other => other.Id != windowId);
        if (window.Tabs.Count == 0)
        {
            Workspace.RemoveWindow(windowId);
            Save();
            return othersOpen ? WindowCloseOutcome.Closed : WindowCloseOutcome.QuitApplication;
        }
        if (!othersOpen)
        {
            return WindowCloseOutcome.QuitApplication;
        }
        window.Status = WindowStatus.Closed;
        Save();
        return WindowCloseOutcome.Closed;
    }

    /// <summary>Reopens a closed window with its tabs.</summary>
    /// <param name="windowId">The window.</param>
    /// <returns>The reopened window.</returns>
    public SessionWindow ReopenWindow(WindowId windowId)
    {
        SessionWindow window = RequireWindow(windowId);
        window.Status = WindowStatus.Open;
        Save();
        return window;
    }

    /// <summary>Drops a closed window for good, with the hook records of its tabs.</summary>
    /// <param name="windowId">The window.</param>
    public void ForgetWindow(WindowId windowId)
    {
        SessionWindow? window = Workspace.RemoveWindow(windowId);
        if (window is null)
        {
            return;
        }
        foreach (SessionTab tab in window.Tabs)
        {
            _sessions.Forget(tab.Id);
        }
        Save();
    }

    /// <summary>Handles the end of the process of a tab.</summary>
    /// <param name="windowId">The window of the tab.</param>
    /// <param name="tabId">The tab.</param>
    /// <param name="exitCode">Exit code of the process.</param>
    /// <returns>What the end of the process leads to.</returns>
    public TabExitOutcome OnTabExited(WindowId windowId, TabId tabId, int exitCode)
    {
        if (IsShuttingDown || Workspace.FindWindow(windowId)?.Contains(tabId) != true)
        {
            return TabExitOutcome.Ignored;
        }
        if (exitCode != 0)
        {
            return TabExitOutcome.KeepWithError;
        }
        CloseTab(windowId, tabId);
        return TabExitOutcome.TabClosed;
    }

    /// <summary>Gives a tab a new session in the same directory, for « Nouvelle session ici ».</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>The tab with its new session.</returns>
    public SessionTab RenewSession(TabId tabId)
    {
        SessionTab tab = RequireTab(tabId);
        tab.SessionId = new SessionId(_ids.NewId());
        tab.Title = null;
        Save();
        return tab;
    }

    /// <summary>Selects a tab of a window.</summary>
    /// <param name="windowId">The window.</param>
    /// <param name="tabId">The tab.</param>
    public void SelectTab(WindowId windowId, TabId tabId)
    {
        RequireWindow(windowId).Select(tabId);
    }

    /// <summary>Moves a tab to another place in its window and saves the new order.</summary>
    /// <param name="windowId">The window.</param>
    /// <param name="tabId">The tab.</param>
    /// <param name="index">Its new place, counted from 0.</param>
    /// <returns>True when the order changed.</returns>
    public bool MoveTab(WindowId windowId, TabId tabId, int index)
    {
        if (!RequireWindow(windowId).MoveTab(tabId, index))
        {
            return false;
        }
        Save();
        return true;
    }

    /// <summary>Records where a window is; saved with the next save.</summary>
    /// <param name="windowId">The window.</param>
    /// <param name="bounds">Its position and size.</param>
    public void UpdateBounds(WindowId windowId, WindowBounds bounds)
    {
        if (!IsShuttingDown)
        {
            Workspace.FindWindow(windowId)?.Bounds = bounds;
        }
    }

    /// <summary>Records the title a session gave its tab; saved with the next save.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="title">The title.</param>
    public void UpdateTitle(TabId tabId, string title)
    {
        FindTab(tabId)?.Title = title;
    }

    /// <summary>Builds what a terminal needs to start the session of a tab.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>The launch request.</returns>
    public TabLaunchRequest LaunchRequestFor(TabId tabId)
    {
        SessionTab tab = RequireTab(tabId);
        return new TabLaunchRequest(tab.Id, tab.SessionId, tab.Cwd);
    }

    /// <summary>Follows the session changes (/clear, /resume, fork) the hook recorded for the given tabs.</summary>
    /// <param name="tabIds">The tabs whose session runs.</param>
    /// <returns>A task that completes once the records are read or the timeout is reached.</returns>
    public async Task SynchronizeAsync(IEnumerable<TabId> tabIds)
    {
        IReadOnlyDictionary<TabId, SessionId> sessions = await _synchronizer.ReadAsync(tabIds).ConfigureAwait(true);
        foreach ((TabId tabId, SessionId sessionId) in sessions)
        {
            FindTab(tabId)?.SessionId = sessionId;
        }
    }

    /// <summary>Saves the workspace, unless the shutdown has begun.</summary>
    public void Save()
    {
        if (!IsShuttingDown)
        {
            _store.Save(Workspace);
        }
    }

    /// <summary>Marks the start of the shutdown: from now on, window closes, process exits and saves change nothing.</summary>
    public void BeginShutdown()
    {
        IsShuttingDown = true;
    }

    /// <summary>Saves the workspace once, during the shutdown; later calls do nothing.</summary>
    public void SaveFinal()
    {
        if (_finalSaveDone)
        {
            return;
        }
        _finalSaveDone = true;
        _store.Save(Workspace);
    }

    private SessionWindow CreateWindow(string cwd, WindowBounds bounds)
    {
        SessionTab tab = CreateTab(cwd);
        return new SessionWindow(new WindowId(_ids.NewId()), WindowStatus.Open, bounds, [tab], tab.Id);
    }

    private SessionTab CreateTab(string cwd)
    {
        return new SessionTab(new TabId(_ids.NewId()), cwd, new SessionId(_ids.NewId()), null);
    }

    private SessionWindow RequireWindow(WindowId windowId)
    {
        return Workspace.FindWindow(windowId) ?? throw new InvalidOperationException($"Unknown window {windowId}.");
    }

    private SessionTab? FindTab(TabId tabId)
    {
        return Workspace.Windows.Select(window => window.FindTab(tabId)).FirstOrDefault(tab => tab is not null);
    }

    private SessionTab RequireTab(TabId tabId)
    {
        return FindTab(tabId) ?? throw new InvalidOperationException($"Unknown tab {tabId}.");
    }
}
