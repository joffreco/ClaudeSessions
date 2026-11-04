namespace ClaudeSessions.Tests.Consignes.Fakes;

/// <summary>Builds consigne files as the consigne script writes them.</summary>
public static class ConsigneFiles
{
    /// <summary>A consigne file.</summary>
    /// <param name="title">Its title.</param>
    /// <param name="body">Its Markdown.</param>
    /// <param name="updated">Its update time, ISO 8601.</param>
    /// <param name="tabId">Its tab id.</param>
    /// <param name="status">Its status as written by the script.</param>
    /// <returns>The file's content.</returns>
    public static string Of(string title, string body = "Étape 1", string updated = "2026-11-04T10:00:00-05:00", string tabId = "", string status = "prete")
    {
        return "---\n"
            + $"title: \"{title}\"\n"
            + $"status: \"{status}\"\n"
            + $"tab_id: \"{tabId}\"\n"
            + "tab_title: \"682 runner1\"\n"
            + "session_id: \"sess\"\n"
            + "cwd: \"/home/joff/Sources/comunikator\"\n"
            + "created: \"2026-11-04T09:00:00-05:00\"\n"
            + $"updated: \"{updated}\"\n"
            + "---\n"
            + body + "\n";
    }
}
