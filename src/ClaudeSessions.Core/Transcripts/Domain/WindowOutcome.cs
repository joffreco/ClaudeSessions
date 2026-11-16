namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>What became of a request from Conversations to open a window or a tab in ClaudeSessions.</summary>
public enum WindowOutcome
{
    /// <summary>The window or tab was opened.</summary>
    Done,

    /// <summary>ClaudeSessions is not running, or did not answer.</summary>
    Unreachable,

    /// <summary>No open window of ClaudeSessions has this id.</summary>
    NoSuchWindow,
}
