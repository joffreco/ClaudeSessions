namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>What became of a message sent from Conversations to a tab's session.</summary>
public enum PromptOutcome
{
    /// <summary>The message was typed into the session and submitted.</summary>
    Sent,

    /// <summary>The command was typed into the session and opens a menu of Claude Code, which ClaudeSessions shows in front.</summary>
    SentToMenu,

    /// <summary>ClaudeSessions is not running, or did not answer.</summary>
    Unreachable,

    /// <summary>No open window of ClaudeSessions has this tab.</summary>
    NoSuchTab,

    /// <summary>The tab's session is not running: asleep, ended, or never shown since the start.</summary>
    NotRunning,

    /// <summary>The session waits for an approval; Enter would answer it, so nothing was sent.</summary>
    AwaitingApproval,
}
