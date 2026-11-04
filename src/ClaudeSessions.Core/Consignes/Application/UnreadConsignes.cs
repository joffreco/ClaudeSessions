using System.Linq;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;

namespace ClaudeSessions.Core.Consignes.Application;

/// <summary>Counts the consignes the user has not opened since their last version, for the ClaudeSessions menu.</summary>
public static class UnreadConsignes
{
    /// <summary>Counts the unread consignes.</summary>
    /// <param name="source">Where the sessions deposit their consignes.</param>
    /// <param name="store">Where the Consignes app keeps what the user read.</param>
    /// <returns>How many consignes are unread.</returns>
    public static int Count(IConsigneSource source, IReadingStateStore store)
    {
        ReadingState state = store.Load();
        return source.ReadAll().Count(state.IsUnread);
    }
}
