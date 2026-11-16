using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Core.Transcripts.Application;

/// <summary>
/// Lists the tabs to browse with the session each one runs right now. ClaudeSessions saves a
/// tab's session every 30 seconds, while the SessionStart hook records it as soon as /clear,
/// /resume or a fork changes it, so the hook's record wins over the saved session.
/// </summary>
public sealed class ConversationTabs
{
    private readonly IConversationTabSource _tabs;
    private readonly ITabSessionSource _sessions;

    /// <summary>Creates the list.</summary>
    /// <param name="tabs">The tabs saved by ClaudeSessions.</param>
    /// <param name="sessions">The sessions recorded by the hook.</param>
    public ConversationTabs(IConversationTabSource tabs, ITabSessionSource sessions)
    {
        _tabs = tabs;
        _sessions = sessions;
    }

    /// <summary>Reads the tabs and their current session.</summary>
    /// <returns>The tabs, window by window; null when ClaudeSessions' file cannot be read.</returns>
    public async Task<IReadOnlyList<ConversationTab>?> ReadAsync()
    {
        IReadOnlyList<ConversationTab>? saved = _tabs.Read();
        if (saved is null)
        {
            return null;
        }
        List<ConversationTab> current = [];
        foreach (ConversationTab tab in saved)
        {
            SessionId? recorded = await _sessions.ReadAsync(tab.Id).ConfigureAwait(false);
            current.Add(recorded is SessionId session ? tab with { Session = session } : tab);
        }
        return current;
    }
}
