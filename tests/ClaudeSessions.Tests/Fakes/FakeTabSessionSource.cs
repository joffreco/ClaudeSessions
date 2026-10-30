using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>Session source whose answers the test controls, recording reads and forgotten tabs.</summary>
public sealed class FakeTabSessionSource : ITabSessionSource
{
    private readonly Dictionary<TabId, Task<SessionId?>> _answers = [];

    /// <summary>Number of reads started, by tab.</summary>
    public Dictionary<TabId, int> ReadCounts { get; } = [];

    /// <summary>Tabs whose record was deleted, in order.</summary>
    public List<TabId> Forgotten { get; } = [];

    /// <summary>Answers a tab's reads at once.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="sessionId">The session to answer, or null for no record.</param>
    public void Answer(TabId tabId, SessionId? sessionId)
    {
        _answers[tabId] = Task.FromResult(sessionId);
    }

    /// <summary>Makes a tab's reads return a task the test completes later.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>The completion source of the read.</returns>
    public TaskCompletionSource<SessionId?> Hang(TabId tabId)
    {
        TaskCompletionSource<SessionId?> pending = new();
        _answers[tabId] = pending.Task;
        return pending;
    }

    /// <summary>Makes a tab's reads fail.</summary>
    /// <param name="tabId">The tab.</param>
    public void Fail(TabId tabId)
    {
        _answers[tabId] = Task.FromException<SessionId?>(new System.IO.IOException("unreachable"));
    }

    /// <inheritdoc />
    public Task<SessionId?> ReadAsync(TabId tabId)
    {
        ReadCounts[tabId] = ReadCounts.GetValueOrDefault(tabId) + 1;
        return _answers.GetValueOrDefault(tabId) ?? Task.FromResult<SessionId?>(null);
    }

    /// <inheritdoc />
    public void Forget(TabId tabId)
    {
        Forgotten.Add(tabId);
    }
}
