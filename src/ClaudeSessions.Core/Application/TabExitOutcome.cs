namespace ClaudeSessions.Core.Application;

/// <summary>What the end of the process of a tab leads to.</summary>
public enum TabExitOutcome
{
    /// <summary>The session ended normally (/exit): the tab is closed and kept for reopening.</summary>
    TabClosed,

    /// <summary>The session failed: the tab stays, with a banner offering to resume, start anew or close.</summary>
    KeepWithError,

    /// <summary>The application is shutting down, or the tab is gone: nothing changes.</summary>
    Ignored,
}
