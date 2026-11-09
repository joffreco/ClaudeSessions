using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>Activity probe whose answers the test sets per tab; an unset tab reads as unreadable.</summary>
public sealed class FakeTabActivityProbe : ITabActivityProbe
{
    private readonly Dictionary<TabId, TabActivity?> _answers = [];

    /// <summary>Sets what the probe answers for a tab.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="activity">The activity, or null for a failed read.</param>
    public void Answer(TabId tabId, TabActivity? activity)
    {
        _answers[tabId] = activity;
    }

    /// <inheritdoc />
    public Task<TabActivity?> ReadAsync(TabId tabId, SessionId sessionId)
    {
        return Task.FromResult(_answers.GetValueOrDefault(tabId));
    }
}
