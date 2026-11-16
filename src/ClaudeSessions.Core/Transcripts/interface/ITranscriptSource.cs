using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Core.Transcripts.Interfaces;

/// <summary>Reads what a Claude Code session said and did, from wherever Claude Code keeps it.</summary>
public interface ITranscriptSource
{
    /// <summary>Reads the active branch of a session, as it stands now. Calling again reads only what was added since.</summary>
    /// <param name="session">The session.</param>
    /// <returns>The conversation, or null when the session has no transcript yet.</returns>
    Task<Conversation?> ReadAsync(SessionId session);

    /// <summary>
    /// Reads the last entries of a session only, cheaply enough to call for every tab every second, to tell
    /// whether Claude is working (see <see cref="Application.TurnActivity"/>). Calling again reads only what was added since.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns>The last entries, in file order, or null when the session has no transcript yet.</returns>
    Task<Conversation?> ReadRecentAsync(SessionId session);
}
