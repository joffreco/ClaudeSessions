using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClaudeSessions.Adapters;

/// <summary>What the repository holds after its pull: its commit, its uncommitted files, and the changes the installed app lacks.</summary>
/// <param name="Head">Commit of the repository after the pull.</param>
/// <param name="UncommittedFiles">How many files <c>git status</c> lists as changed and not committed; the update installs them too.</param>
/// <param name="Changes">Subjects of the commits between the installed one and <paramref name="Head"/>, newest first.</param>
public sealed record UpdateCheck(string Head, int UncommittedFiles, IReadOnlyList<string> Changes)
{
    /// <summary>
    /// Reads the output of the check: the head commit on the first line, the number of uncommitted files on
    /// the second, then one subject per line.
    /// </summary>
    /// <param name="output">Standard output of the check.</param>
    /// <returns>The check; null when the first line is not a commit or the second not a count.</returns>
    public static UpdateCheck? Parse(string output)
    {
        string[] lines = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 1 && IsCommit(lines[0]) && int.TryParse(lines[1], NumberStyles.None, CultureInfo.InvariantCulture, out int uncommitted)
            ? new UpdateCheck(lines[0], uncommitted, lines[2..])
            : null;
    }

    /// <summary>True when the repository has changes not committed, which the installed app may lack.</summary>
    public bool HasUncommittedFiles => UncommittedFiles > 0;

    /// <summary>Extracts the commit the SDK appends to the informational version ("1.0.0+&lt;commit&gt;").</summary>
    /// <param name="informationalVersion">The informational version of the app, if any.</param>
    /// <returns>The commit; null when the version carries none.</returns>
    public static string? RevisionOf(string? informationalVersion)
    {
        int plus = informationalVersion?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        string? revision = plus < 0 ? null : informationalVersion![(plus + 1)..];
        return revision is not null && IsCommit(revision) ? revision : null;
    }

    /// <summary>True when the installed app was built from <see cref="Head"/> and the repository has nothing uncommitted.</summary>
    /// <param name="installedRevision">Commit of the installed app; null when unknown.</param>
    /// <returns>Whether there is nothing to update.</returns>
    public bool IsInstalled(string? installedRevision)
    {
        return !HasUncommittedFiles && string.Equals(Head, installedRevision, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCommit(string text)
    {
        return text.Length == 40 && text.All(char.IsAsciiHexDigit);
    }
}
