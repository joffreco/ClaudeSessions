using System.Collections.Generic;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Core.Transcripts.Interfaces;

/// <summary>The tabs of the open ClaudeSessions windows, read without disturbing the running app.</summary>
public interface IConversationTabSource
{
    /// <summary>Reads the tabs, window by window, in their order.</summary>
    /// <returns>The tabs, with the session each one last saved; null when ClaudeSessions' file cannot be read.</returns>
    IReadOnlyList<ConversationTab>? Read();
}
