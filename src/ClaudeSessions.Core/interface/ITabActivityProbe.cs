using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Interfaces;

/// <summary>Looks at what the session of a tab did lately, for the idle suspension.</summary>
public interface ITabActivityProbe
{
    /// <summary>Reads the recent activity of a tab's session. The read may be slow.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="sessionId">Session the tab runs.</param>
    /// <returns>The activity, or null when it could not be read.</returns>
    Task<TabActivity?> ReadAsync(TabId tabId, SessionId sessionId);
}
