using System;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Core.Consignes.Application;

/// <summary>Chooses the name a session folder is shown under.</summary>
public static class TabLabel
{
    /// <summary>The title ClaudeSessions gives a tab before the session names it.</summary>
    public const string GenericTitle = "Claude Code";

    /// <summary>The suffix of a session that ran outside a ClaudeSessions tab.</summary>
    public const string OutsideTabSuffix = " (hors onglet)";

    /// <summary>
    /// The tab's current title in ClaudeSessions, else the title it had when the consigne was deposited,
    /// else the last folder of the session's working directory.
    /// </summary>
    /// <param name="latest">The folder's most recent consigne.</param>
    /// <param name="tab">What ClaudeSessions knows of the tab; null when nothing.</param>
    /// <returns>The label.</returns>
    public static string Of(Consigne latest, TabInfo? tab)
    {
        if (latest.TabId.Length == 0)
        {
            return FolderLabel.Of(latest.Cwd) + OutsideTabSuffix;
        }
        if (IsMeaningful(tab?.Title))
        {
            return tab!.Title!;
        }
        return IsMeaningful(latest.TabTitle) ? latest.TabTitle : FolderLabel.Of(latest.Cwd);
    }

    /// <summary>Tells whether a title names the session, rather than being empty or ClaudeSessions' default.</summary>
    /// <param name="title">The title.</param>
    /// <returns>True when it is worth showing.</returns>
    public static bool IsMeaningful(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) && !string.Equals(title, GenericTitle, StringComparison.Ordinal);
    }
}
