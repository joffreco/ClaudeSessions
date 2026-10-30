namespace ClaudeSessions.Core.Application;

/// <summary>What closing a window leads to.</summary>
public enum WindowCloseOutcome
{
    /// <summary>The window is put aside and listed for reopening, or dropped when it had no tab.</summary>
    Closed,

    /// <summary>It was the last open window: the application quits and the window comes back next time.</summary>
    QuitApplication,

    /// <summary>The application is shutting down: the window keeps its state.</summary>
    Ignored,
}
