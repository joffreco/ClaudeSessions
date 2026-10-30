namespace ClaudeSessions.Core.Domain;

/// <summary>A tab running one Claude Code session in a WSL working directory.</summary>
public sealed class SessionTab
{
    /// <summary>Creates a tab.</summary>
    /// <param name="id">Identifier of the tab.</param>
    /// <param name="cwd">WSL working directory of the session.</param>
    /// <param name="sessionId">Claude Code session the tab runs.</param>
    /// <param name="title">Last title the session gave the tab, if any.</param>
    public SessionTab(TabId id, string cwd, SessionId sessionId, string? title)
    {
        Id = id;
        Cwd = cwd;
        SessionId = sessionId;
        Title = title;
    }

    /// <summary>Identifier of the tab.</summary>
    public TabId Id { get; }

    /// <summary>WSL working directory of the session.</summary>
    public string Cwd { get; }

    /// <summary>Claude Code session the tab runs; it changes on /clear, /resume or a fork.</summary>
    public SessionId SessionId { get; set; }

    /// <summary>Last title the session gave the tab, if any.</summary>
    public string? Title { get; set; }

    /// <summary>Text shown on the tab: the session title, else the last folder of the working directory.</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? FolderName() : Title;

    private string FolderName()
    {
        string trimmed = Cwd.TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        string name = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
        return name.Length == 0 ? "/" : name;
    }
}
