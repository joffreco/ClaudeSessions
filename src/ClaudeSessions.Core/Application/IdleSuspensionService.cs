using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Core.Application;

/// <summary>
/// Picks the tabs to put to sleep: a tab the user has neither looked at nor typed in for the chosen delay,
/// whose session waits for the user, has written no transcript in that delay (its own or a subagent's)
/// and runs no child process. The delay is saved; none means the tabs never sleep.
/// </summary>
public sealed class IdleSuspensionService
{
    /// <summary>Longest delay that can be chosen, in hours.</summary>
    public const int MaxHours = 168;

    private readonly IPreferencesStore _store;
    private readonly ITabActivityProbe _probe;
    private readonly TimeProvider _clock;

    /// <summary>Creates the service with the saved delay.</summary>
    /// <param name="store">Keeps the delay among the preferences.</param>
    /// <param name="probe">Reads what a session did lately.</param>
    /// <param name="clock">The clock.</param>
    public IdleSuspensionService(IPreferencesStore store, ITabActivityProbe probe, TimeProvider clock)
    {
        _store = store;
        _probe = probe;
        _clock = clock;
        HoursBeforeSleep = Valid(store.Load().SuspendAfterHours);
    }

    /// <summary>Hours of inactivity before a tab sleeps, or null when tabs never sleep.</summary>
    public int? HoursBeforeSleep { get; private set; }

    /// <summary>Chooses the delay before a tab sleeps and saves the choice.</summary>
    /// <param name="hours">Hours, from 1 to <see cref="MaxHours"/>, or null so that tabs never sleep.</param>
    /// <returns>True when the choice changed.</returns>
    public bool SetHoursBeforeSleep(int? hours)
    {
        int? next = Valid(hours);
        if (next == HoursBeforeSleep)
        {
            return false;
        }
        HoursBeforeSleep = next;
        _store.Save(_store.Load() with { SuspendAfterHours = next });
        return true;
    }

    /// <summary>Picks, among running tabs, those to put to sleep now.</summary>
    /// <param name="tabs">The running tabs.</param>
    /// <returns>The tabs to put to sleep; none when tabs never sleep.</returns>
    public async Task<IReadOnlyList<IdleCandidate>> SelectAsync(IReadOnlyCollection<IdleCandidate> tabs)
    {
        if (HoursBeforeSleep is not int hours)
        {
            return [];
        }
        DateTimeOffset threshold = _clock.GetUtcNow() - TimeSpan.FromHours(hours);
        List<IdleCandidate> untouched = tabs.Where(tab => IsUntouchedSince(tab, threshold)).ToList();
        TabActivity?[] activities = await Task.WhenAll(untouched.Select(tab => _probe.ReadAsync(tab.TabId, tab.SessionId))).ConfigureAwait(true);
        return untouched.Where((_, index) => IsQuietSince(activities[index], threshold)).ToList();
    }

    private static bool IsUntouchedSince(IdleCandidate tab, DateTimeOffset threshold)
    {
        return tab.WaitsForUser && !tab.IsOnScreen && tab.LastTouched <= threshold;
    }

    private static bool IsQuietSince(TabActivity? activity, DateTimeOffset threshold)
    {
        return activity is not null && activity.ChildProcesses == 0 && (activity.LastTranscriptWrite is not DateTimeOffset written || written <= threshold);
    }

    private static int? Valid(int? hours)
    {
        return hours is >= 1 and <= MaxHours ? hours : null;
    }
}
