using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Interfaces;

/// <summary>Tells which Claude Code session a tab runs right now, as recorded by the SessionStart hook.</summary>
public interface ITabSessionSource
{
    /// <summary>Reads the session currently recorded for a tab. The read cannot be cancelled and may be slow.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>The recorded session, or null when no valid record exists.</returns>
    Task<SessionId?> ReadAsync(TabId tabId);

    /// <summary>Deletes the record of a tab that will never come back; failures are ignored.</summary>
    /// <param name="tabId">The tab.</param>
    void Forget(TabId tabId);
}
