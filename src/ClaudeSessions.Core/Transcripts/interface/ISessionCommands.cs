using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Core.Transcripts.Interfaces;

/// <summary>
/// What Conversations asks of ClaudeSessions, which runs the sessions: Conversations sends these
/// commands, ClaudeSessions carries them out.
/// </summary>
public interface ISessionCommands
{
    /// <summary>Types a message into a tab's session and submits it, as if the user typed it.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="text">The message; it may span several lines.</param>
    /// <returns>What became of it.</returns>
    Task<PromptOutcome> SendPromptAsync(TabId tabId, string text);

    /// <summary>Shows a tab in front in ClaudeSessions, to answer a menu or a question of Claude Code in its terminal.</summary>
    /// <param name="tabId">The tab.</param>
    /// <returns>Done, or NoSuchWindow when no open window holds the tab.</returns>
    Task<WindowOutcome> ShowTabAsync(TabId tabId);

    /// <summary>Opens a new window with one tab in the default directory.</summary>
    /// <returns>What became of the command.</returns>
    Task<WindowOutcome> OpenWindowAsync();

    /// <summary>Opens a new tab in a window, in the directory of its selected tab.</summary>
    /// <param name="windowId">The window.</param>
    /// <returns>What became of the command.</returns>
    Task<WindowOutcome> OpenTabAsync(WindowId windowId);
}
