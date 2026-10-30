using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Windows.Terminal;
using ClaudeSessions.Windows.Views;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>
/// Drives the windows from the use cases: opens the views, applies what each action leads to, follows
/// the sessions every 30 seconds, and runs the ordered shutdown.
/// </summary>
internal sealed class WindowCoordinator
{
    private static readonly TimeSpan SynchronizationPeriod = TimeSpan.FromSeconds(30);
    private const int MaxSuggestedFolders = 40;

    private readonly WorkspaceService _service;
    private readonly WslCommandLine _commandLine;
    private readonly AppSettings _settings;
    private readonly ZoomService _zoom;
    private readonly WheelScrollService _wheel;
    private readonly Dictionary<WindowId, SessionWindowView> _views = [];
    private readonly DispatcherTimer _timer;
    private bool _synchronizing;

    /// <summary>Creates the coordinator.</summary>
    /// <param name="service">The use cases.</param>
    /// <param name="commandLine">Builds the command line of a tab.</param>
    /// <param name="settings">Settings of the application.</param>
    /// <param name="zoom">Font size of the terminals.</param>
    /// <param name="wheel">Lines per wheel notch.</param>
    public WindowCoordinator(WorkspaceService service, WslCommandLine commandLine, AppSettings settings, ZoomService zoom, WheelScrollService wheel)
    {
        _service = service;
        _commandLine = commandLine;
        _settings = settings;
        _zoom = zoom;
        _wheel = wheel;
        _timer = new DispatcherTimer { Interval = SynchronizationPeriod };
        _timer.Tick += async (_, _) => await SynchronizeAndSaveAsync().ConfigureAwait(true);
    }

    /// <summary>WSL directory of the first tab of a new window.</summary>
    public string DefaultCwd => _service.DefaultCwd;

    /// <summary>Opens the saved windows and starts following the sessions.</summary>
    public void Start()
    {
        foreach (SessionWindow window in _service.Load())
        {
            Show(window);
        }
        _timer.Start();
    }

    /// <summary>Brings the first open window to the front, for a second launch of the application.</summary>
    public void Activate()
    {
        _views.Values.FirstOrDefault()?.BringToFront();
    }

    /// <summary>Creates the terminal of a tab, with the configured font at the current zoom.</summary>
    /// <returns>A terminal that runs nothing yet.</returns>
    public TabTerminal CreateTerminal()
    {
        return new TabTerminal(_settings.FontFamily, _zoom.Size);
    }

    /// <summary>Lines scrolled per wheel notch, or null when the Windows setting applies.</summary>
    public int? WheelLines => _wheel.LinesPerNotch;

    /// <summary>Chooses the lines scrolled per wheel notch, in every window.</summary>
    /// <param name="lines">Lines per notch, or null to follow the Windows setting.</param>
    public void SetWheelLines(int? lines)
    {
        _wheel.SetLinesPerNotch(lines);
    }

    /// <summary>Zooms the terminals of every window, the closed ones included once reopened.</summary>
    /// <param name="command">ZoomIn, ZoomOut or ResetZoom.</param>
    public void Zoom(AppCommand command)
    {
        bool changed = command switch
        {
            AppCommand.ZoomIn => _zoom.ZoomIn(),
            AppCommand.ZoomOut => _zoom.ZoomOut(),
            _ => _zoom.Reset(),
        };
        if (!changed)
        {
            return;
        }
        foreach (SessionWindowView view in _views.Values)
        {
            view.SetFontSize(_zoom.Size);
        }
    }

    /// <summary>Builds the command line that runs the session of a tab.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>The command line.</returns>
    public string CommandLineFor(TabId tabId)
    {
        return _commandLine.Build(_service.LaunchRequestFor(tabId));
    }

    /// <summary>Windows put aside, for the menu.</summary>
    /// <returns>The closed windows.</returns>
    public IEnumerable<SessionWindow> ClosedWindows()
    {
        return _service.Workspace.ClosedWindows.ToList();
    }

    /// <summary>Opens a new tab in a window.</summary>
    /// <param name="view">The window.</param>
    /// <param name="cwd">WSL directory of the session.</param>
    public void NewTab(SessionWindowView view, string cwd)
    {
        view.ShowAddedTab(_service.NewTab(view.Model.Id, cwd));
    }

    /// <summary>Asks for a directory, then opens a new tab there.</summary>
    /// <param name="view">The window.</param>
    /// <param name="initial">Directory shown at first.</param>
    public void NewTabIn(SessionWindowView view, string initial)
    {
        string? cwd = FolderPrompt.Ask(view, SuggestedFolders(), initial);
        if (cwd is not null)
        {
            NewTab(view, cwd);
        }
    }

    /// <summary>Opens a new window with one tab in the default directory.</summary>
    public void NewWindow()
    {
        Show(_service.NewWindow(_service.DefaultCwd, WindowBounds.Default));
    }

    /// <summary>Selects a tab.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    public void SelectTab(SessionWindowView view, TabId tabId)
    {
        _service.SelectTab(view.Model.Id, tabId);
        view.Refresh();
    }

    /// <summary>Moves a tab to another place in its window.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    /// <param name="index">Its new place, counted from 0.</param>
    public void MoveTab(SessionWindowView view, TabId tabId, int index)
    {
        if (_service.MoveTab(view.Model.Id, tabId, index))
        {
            view.Refresh();
        }
    }

    /// <summary>Closes a tab; a window left without tabs closes too.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    public void CloseTab(SessionWindowView view, TabId tabId)
    {
        if (_service.CloseTab(view.Model.Id, tabId))
        {
            AfterTabRemoved(view, tabId);
        }
    }

    /// <summary>Reopens the most recently closed tab in a window.</summary>
    /// <param name="view">The window.</param>
    public void ReopenClosedTab(SessionWindowView view)
    {
        SessionTab? tab = _service.ReopenClosedTab(view.Model.Id);
        if (tab is not null)
        {
            view.ShowAddedTab(tab);
        }
    }

    /// <summary>Reopens a closed window.</summary>
    /// <param name="windowId">The window.</param>
    public void ReopenWindow(WindowId windowId)
    {
        Show(_service.ReopenWindow(windowId));
    }

    /// <summary>Drops a closed window for good.</summary>
    /// <param name="windowId">The window.</param>
    public void ForgetWindow(WindowId windowId)
    {
        _service.ForgetWindow(windowId);
    }

    /// <summary>Starts the session of a failed tab again.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    public void Relaunch(SessionWindowView view, TabId tabId)
    {
        view.LaunchTab(tabId, CommandLineFor(tabId));
    }

    /// <summary>Gives a failed tab a new session in the same directory.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    public void RenewSession(SessionWindowView view, TabId tabId)
    {
        _service.RenewSession(tabId);
        view.LaunchTab(tabId, CommandLineFor(tabId));
    }

    /// <summary>Records the title a session gave its tab.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    /// <param name="title">The title.</param>
    public void UpdateTitle(SessionWindowView view, TabId tabId, string title)
    {
        SessionTitle parsed = SessionTitle.Parse(title);
        if (parsed.Text.Length > 0)
        {
            _service.UpdateTitle(tabId, parsed.Text);
        }
        view.ShowStatus(tabId, parsed);
    }

    /// <summary>Records where a window is.</summary>
    /// <param name="view">The window.</param>
    /// <param name="bounds">Its position and size.</param>
    public void UpdateBounds(SessionWindowView view, WindowBounds bounds)
    {
        _service.UpdateBounds(view.Model.Id, bounds);
    }

    /// <summary>Applies the end of the process of a tab.</summary>
    /// <param name="view">The window.</param>
    /// <param name="tabId">The tab.</param>
    /// <param name="exitCode">Exit code of the process.</param>
    public void OnTabExited(SessionWindowView view, TabId tabId, int exitCode)
    {
        switch (_service.OnTabExited(view.Model.Id, tabId, exitCode))
        {
            case TabExitOutcome.TabClosed:
                AfterTabRemoved(view, tabId);
                break;
            case TabExitOutcome.KeepWithError:
                view.ShowFailure(tabId, exitCode);
                break;
            case TabExitOutcome.Ignored:
                break;
        }
    }

    /// <summary>Decides whether a window may close now; the closing goes on asynchronously when it may not.</summary>
    /// <param name="view">The window the user is closing.</param>
    /// <returns>True to cancel this closing.</returns>
    public bool OnWindowClosing(SessionWindowView view)
    {
        if (_service.IsShuttingDown)
        {
            return false;
        }
        _ = CloseWindowAsync(view);
        return true;
    }

    /// <summary>Quits the application: follows the sessions one last time, saves, and closes every window.</summary>
    public void Quit()
    {
        _ = QuitAsync();
    }

    /// <summary>Saves at once when Windows ends the session; nothing is read from WSL, which is going down too.</summary>
    public void OnSessionEnding()
    {
        _service.BeginShutdown();
        _timer.Stop();
        _service.SaveFinal();
    }

    private async Task CloseWindowAsync(SessionWindowView view)
    {
        await _service.SynchronizeAsync(view.RunningTabIds.ToList()).ConfigureAwait(true);
        switch (_service.CloseWindow(view.Model.Id))
        {
            case WindowCloseOutcome.Closed:
                Remove(view);
                break;
            case WindowCloseOutcome.QuitApplication:
                await QuitAsync().ConfigureAwait(true);
                break;
            case WindowCloseOutcome.Ignored:
                break;
        }
    }

    private async Task QuitAsync()
    {
        if (_service.IsShuttingDown)
        {
            return;
        }
        _service.BeginShutdown();
        _timer.Stop();
        await _service.SynchronizeAsync(_views.Values.SelectMany(view => view.RunningTabIds).ToList()).ConfigureAwait(true);
        _service.SaveFinal();
        foreach (SessionWindowView view in _views.Values.ToList())
        {
            view.CloseNow();
        }
        Application.Current.Shutdown();
    }

    private async Task SynchronizeAndSaveAsync()
    {
        if (_synchronizing || _service.IsShuttingDown)
        {
            return;
        }
        _synchronizing = true;
        try
        {
            await _service.SynchronizeAsync(_views.Values.SelectMany(view => view.RunningTabIds).ToList()).ConfigureAwait(true);
            _service.Save();
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private void AfterTabRemoved(SessionWindowView view, TabId tabId)
    {
        view.DropTab(tabId);
        if (view.Model.Tabs.Count == 0)
        {
            _ = CloseWindowAsync(view);
        }
    }

    private void Show(SessionWindow window)
    {
        SessionWindowView view = new(this, window);
        _views[window.Id] = view;
        view.Show();
    }

    private void Remove(SessionWindowView view)
    {
        _views.Remove(view.Model.Id);
        view.CloseNow();
    }

    private List<string> SuggestedFolders()
    {
        IEnumerable<string> recent = _service.Workspace.Windows
            .SelectMany(window => window.Tabs)
            .Concat(_service.Workspace.ClosedTabs.Select(closed => closed.Tab))
            .Select(tab => tab.Cwd);
        return recent.Concat(FoldersUnder(_service.DefaultCwd)).Distinct().ToList();
    }

    private List<string> FoldersUnder(string wslFolder)
    {
        try
        {
            return Directory.EnumerateDirectories(_settings.WindowsPathOf(wslFolder))
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(name => !name.StartsWith('.'))
                .Order(StringComparer.OrdinalIgnoreCase)
                .Take(MaxSuggestedFolders)
                .Select(name => wslFolder.TrimEnd('/') + "/" + name)
                .ToList();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
