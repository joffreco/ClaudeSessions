namespace ClaudeSessions.Core.Domain;

/// <summary>Whether a window is on screen or put aside until it is reopened.</summary>
public enum WindowStatus
{
    /// <summary>The window is shown; it comes back when the application starts.</summary>
    Open,

    /// <summary>The window was closed while others stayed open; it is listed for reopening.</summary>
    Closed,
}
