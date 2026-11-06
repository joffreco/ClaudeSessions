using System.Collections.Generic;

namespace ClaudeSessions.Core.Domain;

/// <summary>
/// Follows the state marks of the sessions to tell which tabs wait for the user without being watched:
/// a session that stops working (spinner, then ✳) while its tab is not shown in the active window.
/// A tab stops waiting once the user looks at it, or when its session works again. A session that asks
/// for an approval waits until the user looks at it, whatever its marks do, since only the user can answer.
/// </summary>
public sealed class AttentionTracker
{
    private readonly HashSet<TabId> _working = [];
    private readonly HashSet<TabId> _waiting = [];
    private readonly HashSet<TabId> _approvals = [];

    /// <summary>Records the last title a session gave its tab.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="title">Its last title.</param>
    /// <param name="watched">True when the tab is shown in the active window.</param>
    /// <returns>True when the tab starts waiting for the user with this title.</returns>
    public bool Observe(TabId tabId, SessionTitle title, bool watched)
    {
        if (title.Status is null)
        {
            return false;
        }
        if (!title.IsIdle)
        {
            _working.Add(tabId);
            _waiting.Remove(tabId);
            return false;
        }
        bool finished = _working.Remove(tabId);
        return finished && !watched && _waiting.Add(tabId);
    }

    /// <summary>Records that the session of a tab asks the user for an approval.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="watched">True when the tab is shown in the active window, where the user sees the question.</param>
    /// <returns>True when the tab starts asking with this request.</returns>
    public bool RequestApproval(TabId tabId, bool watched)
    {
        return !watched && _approvals.Add(tabId);
    }

    /// <summary>Records that the user looks at a tab, which stops it waiting.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>True when the tab was waiting.</returns>
    public bool Seen(TabId tabId)
    {
        bool asked = _approvals.Remove(tabId);
        return _waiting.Remove(tabId) || asked;
    }

    /// <summary>Tells whether a tab waits for the user.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>True when its session finished working or asked for an approval while nobody watched it.</returns>
    public bool IsWaiting(TabId tabId)
    {
        return _waiting.Contains(tabId) || _approvals.Contains(tabId);
    }

    /// <summary>Tells whether the session of a tab asks the user for an approval.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>True when it asked while nobody watched it, and the user has not looked since.</returns>
    public bool AsksApproval(TabId tabId)
    {
        return _approvals.Contains(tabId);
    }

    /// <summary>Tells what a tab expects from the user.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>The most pressing of what it expects.</returns>
    public TabAttention AttentionOf(TabId tabId)
    {
        if (_approvals.Contains(tabId))
        {
            return TabAttention.Approval;
        }
        return _waiting.Contains(tabId) ? TabAttention.Finished : TabAttention.None;
    }

    /// <summary>Forgets a tab whose session ended or was replaced.</summary>
    /// <param name="tabId">The tab.</param>
    public void Forget(TabId tabId)
    {
        _working.Remove(tabId);
        _waiting.Remove(tabId);
        _approvals.Remove(tabId);
    }
}
