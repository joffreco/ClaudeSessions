namespace ClaudeSessions.Windows.Hosting;

/// <summary>Commands a window runs from the keyboard or its menu.</summary>
internal enum AppCommand
{
    /// <summary>New tab in the directory of the current one (Ctrl+Shift+T).</summary>
    NewTab,

    /// <summary>New tab in a chosen directory (Ctrl+Shift+O).</summary>
    NewTabIn,

    /// <summary>Close the current tab (Ctrl+Shift+W).</summary>
    CloseTab,

    /// <summary>Reopen the most recently closed tab (Ctrl+Shift+R).</summary>
    ReopenClosedTab,

    /// <summary>New window (Ctrl+Shift+N).</summary>
    NewWindow,

    /// <summary>Next tab (Ctrl+Tab).</summary>
    NextTab,

    /// <summary>Previous tab (Ctrl+Shift+Tab).</summary>
    PreviousTab,

    /// <summary>Copy the selection (Ctrl+Shift+C).</summary>
    Copy,

    /// <summary>Paste the clipboard (Ctrl+Shift+V).</summary>
    Paste,

    /// <summary>Resume a failed session (Enter, only while its banner is shown).</summary>
    ResumeFailed,

    /// <summary>Larger font in every terminal (Ctrl+= or Ctrl+wheel up).</summary>
    ZoomIn,

    /// <summary>Smaller font in every terminal (Ctrl+- or Ctrl+wheel down).</summary>
    ZoomOut,

    /// <summary>Configured font size in every terminal (Ctrl+0).</summary>
    ResetZoom,
}
