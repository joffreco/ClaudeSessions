using System;
using System.Collections.Generic;
using ClaudeSessions.Adapters.Transcripts.Dto;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Knows how transcript lines chain. Each line names the line it follows (parentUuid), and
/// Claude Code never removes a line: /rewind followed by a new prompt gives an earlier line a
/// second child, and the abandoned replies stay in the file. The active branch is the chain
/// that ends at the last line written. A compaction boundary has no parent but names the last
/// line before it (logicalParentUuid), so the history before a compaction stays reachable.
/// Subagent lines (sidechains) are left out.
/// </summary>
internal static class ActiveBranch
{
    /// <summary>Picks the active branch of a transcript.</summary>
    /// <param name="records">Every message line, in file order.</param>
    /// <returns>The lines of the active branch, oldest first.</returns>
    public static IReadOnlyList<TranscriptRecord> Resolve(IReadOnlyList<TranscriptRecord> records)
    {
        Dictionary<string, TranscriptRecord> byUuid = new(StringComparer.Ordinal);
        TranscriptRecord? leaf = null;
        foreach (TranscriptRecord record in records)
        {
            if (record.IsSidechain)
            {
                continue;
            }
            byUuid[record.Uuid] = record;
            leaf = record;
        }
        List<TranscriptRecord> branch = [];
        HashSet<string> visited = new(StringComparer.Ordinal);
        for (TranscriptRecord? current = leaf; current is not null && visited.Add(current.Uuid);)
        {
            branch.Add(current);
            string? previous = current.ParentUuid ?? current.LogicalParentUuid;
            current = previous is not null && byUuid.TryGetValue(previous, out TranscriptRecord? parent) ? parent : null;
        }
        branch.Reverse();
        return branch;
    }
}
