using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Core.Transcripts.Interfaces;

/// <summary>The status line Claude Code last showed under a session's input.</summary>
public interface IStatusLineSource
{
    /// <summary>Reads the last status line of a session, without the segment naming the session.</summary>
    /// <param name="session">The session.</param>
    /// <returns>The status line with its colors and percentages; null when none was kept.</returns>
    Task<StatusLine?> ReadAsync(SessionId session);
}
