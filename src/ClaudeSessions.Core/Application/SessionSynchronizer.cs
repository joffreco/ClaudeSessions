using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Core.Application;

/// <summary>
/// Reads the sessions recorded for several tabs at once, waiting at most a fixed delay in total.
/// A read that does not return in time is never started again while it is still pending.
/// </summary>
public sealed class SessionSynchronizer
{
    /// <summary>Longest wait for one synchronization.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    private readonly ITabSessionSource _source;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _timeout;
    private readonly Dictionary<TabId, Task<SessionId?>> _pending = [];

    /// <summary>Creates a synchronizer.</summary>
    /// <param name="source">Where the sessions are recorded.</param>
    /// <param name="clock">Clock used for the timeout.</param>
    /// <param name="timeout">Longest wait for one synchronization.</param>
    public SessionSynchronizer(ITabSessionSource source, TimeProvider clock, TimeSpan timeout)
    {
        _source = source;
        _clock = clock;
        _timeout = timeout;
    }

    /// <summary>Reads the recorded session of each tab, in parallel.</summary>
    /// <param name="tabIds">The tabs to read.</param>
    /// <returns>The sessions read in time, by tab; tabs whose read failed or is late are missing.</returns>
    public async Task<IReadOnlyDictionary<TabId, SessionId>> ReadAsync(IEnumerable<TabId> tabIds)
    {
        Dictionary<TabId, Task<SessionId?>> reads = tabIds.Distinct().ToDictionary(tabId => tabId, PendingOrNewRead);
        if (reads.Count == 0)
        {
            return new Dictionary<TabId, SessionId>();
        }
        await Task.WhenAny(Task.WhenAll(reads.Values), Task.Delay(_timeout, _clock)).ConfigureAwait(true);
        return Harvest(reads);
    }

    private Task<SessionId?> PendingOrNewRead(TabId tabId)
    {
        if (_pending.TryGetValue(tabId, out Task<SessionId?>? pending) && !pending.IsCompleted)
        {
            return pending;
        }
        Task<SessionId?> read = _source.ReadAsync(tabId);
        _pending[tabId] = read;
        return read;
    }

    private Dictionary<TabId, SessionId> Harvest(Dictionary<TabId, Task<SessionId?>> reads)
    {
        Dictionary<TabId, SessionId> sessions = [];
        foreach ((TabId tabId, Task<SessionId?> read) in reads)
        {
            if (!read.IsCompleted)
            {
                continue;
            }
            _pending.Remove(tabId);
            if (read.IsCompletedSuccessfully && read.Result is SessionId session)
            {
                sessions[tabId] = session;
            }
        }
        return sessions;
    }
}
