using System;
using System.Collections.Generic;
using System.Linq;

namespace ClaudeSessions.Adapters;

/// <summary>What the repository holds after its pull: its commit, and the changes the installed app lacks.</summary>
/// <param name="Head">Commit of the repository after the pull.</param>
/// <param name="Changes">Subjects of the commits between the installed one and <paramref name="Head"/>, newest first.</param>
public sealed record UpdateCheck(string Head, IReadOnlyList<string> Changes)
{
    /// <summary>Reads the output of the check: the head commit on the first line, then one subject per line.</summary>
    /// <param name="output">Standard output of the check.</param>
    /// <returns>The check; null when the first line is not a commit.</returns>
    public static UpdateCheck? Parse(string output)
    {
        string[] lines = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 && IsCommit(lines[0]) ? new UpdateCheck(lines[0], lines[1..]) : null;
    }

    /// <summary>Extracts the commit the SDK appends to the informational version ("1.0.0+&lt;commit&gt;").</summary>
    /// <param name="informationalVersion">The informational version of the app, if any.</param>
    /// <returns>The commit; null when the version carries none.</returns>
    public static string? RevisionOf(string? informationalVersion)
    {
        int plus = informationalVersion?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        string? revision = plus < 0 ? null : informationalVersion![(plus + 1)..];
        return revision is not null && IsCommit(revision) ? revision : null;
    }

    /// <summary>True when the installed app was built from <see cref="Head"/>.</summary>
    /// <param name="installedRevision">Commit of the installed app; null when unknown.</param>
    /// <returns>Whether there is nothing to update.</returns>
    public bool IsInstalled(string? installedRevision)
    {
        return string.Equals(Head, installedRevision, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCommit(string text)
    {
        return text.Length == 40 && text.All(char.IsAsciiHexDigit);
    }
}
