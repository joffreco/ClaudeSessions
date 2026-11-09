using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClaudeSessions.Core.Consignes.Application;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;
using ClaudeSessions.Consignes.Interfaces;

namespace ClaudeSessions.Consignes.Hosting;

/// <summary>
/// Reads the consignes folder on a timer, off the UI thread, and keeps the view and the reading state in step
/// with it and with what the user does.
/// </summary>
internal sealed class BoardController
{
    private readonly IConsigneSource _source;
    private readonly ITabDirectory _tabs;
    private readonly IReadingStateStore _store;
    private readonly DispatcherTimer _timer;
    private ReadingState _state = new();
    private IReadOnlyList<Consigne> _consignes = [];
    private IReadOnlyDictionary<string, TabInfo>? _tabInfos;
    private IBoardView? _view;
    private bool _reading;

    /// <summary>Creates the controller.</summary>
    /// <param name="source">The consignes folder.</param>
    /// <param name="tabs">ClaudeSessions' tabs.</param>
    /// <param name="store">Where the reading state is kept.</param>
    /// <param name="interval">How often the folder is read.</param>
    public BoardController(IConsigneSource source, ITabDirectory tabs, IReadingStateStore store, TimeSpan interval)
    {
        _source = source;
        _tabs = tabs;
        _store = store;
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += async (_, _) => await RefreshAsync().ConfigureAwait(true);
    }

    /// <summary>Loads the reading state, shows the folder and starts reading it on the timer.</summary>
    /// <param name="view">The view to keep up to date.</param>
    /// <returns>When the first read is shown.</returns>
    public async Task StartAsync(IBoardView view)
    {
        _view = view;
        _state = _store.Load();
        await RefreshAsync().ConfigureAwait(true);
        _timer.Start();
    }

    /// <summary>Reads the folder again now.</summary>
    /// <returns>When the result is shown; at once when a read is already under way.</returns>
    public async Task RefreshAsync()
    {
        if (_reading)
        {
            return;
        }
        _reading = true;
        try
        {
            (IReadOnlyList<Consigne> consignes, IReadOnlyDictionary<string, TabInfo>? tabs) = await Task.Run(() => (_source.ReadAll(), _tabs.Read())).ConfigureAwait(true);
            _consignes = consignes;
            _tabInfos = tabs;
            ForgetGone();
            Show();
        }
        finally
        {
            _reading = false;
        }
    }

    /// <summary>Tells whether a task of a consigne is ticked.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="index">The task's position.</param>
    /// <returns>True when ticked.</returns>
    public bool IsTicked(ConsigneKey key, int index)
    {
        return _state.IsTicked(key, index);
    }

    /// <summary>The result last sent from a consigne's field.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="name">The field's name.</param>
    /// <returns>The text sent; empty when none was.</returns>
    public string ResultOf(ConsigneKey key, string name)
    {
        return _state.ResultOf(key, name);
    }

    /// <summary>Hands the session a result the user pasted, then keeps it in the field.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="name">The field's name.</param>
    /// <param name="text">What the user pasted.</param>
    /// <returns>Null when sent, else why the session could not get it.</returns>
    public string? SendResult(ConsigneKey key, string name, string text)
    {
        try
        {
            _source.SendResult(key, new PastedResult(name, text, DateTimeOffset.Now));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
        _state.SetResult(key, name, text);
        Save();
        return null;
    }

    /// <summary>Records that the user opened a consigne.</summary>
    /// <param name="consigne">The consigne.</param>
    public void Open(Consigne consigne)
    {
        if (_state.MarkSeen(consigne))
        {
            Save();
            Show();
        }
    }

    /// <summary>Ticks or unticks a task; ticking a step the session marked tells the session first.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="index">The task's position.</param>
    /// <param name="ticked">The new state.</param>
    /// <param name="callbackText">The step's text when the session marked it for a callback; else null.</param>
    /// <returns>Null when done, else why the session could not be told; the task then stays unticked.</returns>
    public string? Tick(ConsigneKey key, int index, bool ticked, string? callbackText)
    {
        if (ticked && callbackText is not null)
        {
            try
            {
                _source.SendTick(key, new StepTick(index, callbackText, DateTimeOffset.Now));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return exception.Message;
            }
        }
        _state.SetTicked(key, index, ticked);
        Save();
        return null;
    }

    /// <summary>Archives a consigne and reads the folder again.</summary>
    /// <param name="key">The consigne.</param>
    /// <returns>Null when archived, else why not.</returns>
    public async Task<string?> ArchiveAsync(ConsigneKey key)
    {
        try
        {
            _source.Archive(key);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
        await RefreshAsync().ConfigureAwait(true);
        return null;
    }

    /// <summary>Opens a session folder, or the consignes folder itself, in Explorer.</summary>
    /// <param name="folder">The session folder; empty for the consignes folder.</param>
    public void OpenFolder(string folder)
    {
        string path = _source.PathOf(folder);
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = false })?.Dispose();
        }
    }

    private void ForgetGone()
    {
        if (_consignes.Count > 0 && _state.Retain(_consignes.Select(consigne => consigne.Key.Text).ToHashSet()))
        {
            Save();
        }
    }

    private void Show()
    {
        _view?.ShowGroups(ConsigneBoard.Build(_consignes, _tabInfos, _state));
    }

    private void Save()
    {
        try
        {
            _store.Save(_state);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
