using System;
using System.Collections.Generic;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Adapters.Transcripts.Dto;

/// <summary>One linked line of a Claude Code transcript, reduced to what the conversation view needs.</summary>
/// <param name="Kind">The line's type: user, assistant and system carry the conversation; others, such as attachment, only link it.</param>
/// <param name="Uuid">The line's identifier.</param>
/// <param name="ParentUuid">The line it follows; null at the start of the transcript and after a compaction.</param>
/// <param name="LogicalParentUuid">For a compaction boundary, the last line before the compaction.</param>
/// <param name="IsSidechain">True for the lines of a subagent.</param>
/// <param name="IsMeta">True for a message Claude Code added for the model, not shown to the user.</param>
/// <param name="IsCompactSummary">True for the summary that replaces the history after a compaction.</param>
/// <param name="Subtype">For a system line, what it reports; empty otherwise.</param>
/// <param name="MessageId">For an assistant line, the model reply it belongs to; a reply spans one line per block.</param>
/// <param name="Timestamp">When the line was written.</param>
/// <param name="Text">The content when it is plain text: a typed prompt, a command tag, a system message.</param>
/// <param name="Blocks">The content when it is a list of blocks.</param>
/// <param name="AttachmentType">For an attachment line, what it attaches, such as skill_listing; empty otherwise.</param>
/// <param name="SkillNames">For a skill_listing attachment, the skills it lists; their descriptions are in <paramref name="Text"/>.</param>
/// <param name="IsInitialListing">For a skill_listing attachment, true when it lists every skill rather than the new ones.</param>
/// <param name="Patch">For a tool result line of Edit or Write, the file change Claude Code recorded with it; null otherwise.</param>
/// <param name="StopReason">For an assistant line, why the model stopped, such as end_turn or tool_use; empty otherwise.</param>
/// <param name="OutputTokens">For an assistant line, the tokens the model wrote for its reply so far; 0 otherwise.</param>
/// <param name="DurationMs">For a turn_duration system line, how long the turn took, in milliseconds; null otherwise.</param>
public sealed record TranscriptRecord(
    string Kind,
    string Uuid,
    string? ParentUuid,
    string? LogicalParentUuid,
    bool IsSidechain,
    bool IsMeta,
    bool IsCompactSummary,
    string Subtype,
    string? MessageId,
    DateTimeOffset? Timestamp,
    string? Text,
    IReadOnlyList<TranscriptBlock> Blocks,
    string AttachmentType = "",
    IReadOnlyList<string>? SkillNames = null,
    bool IsInitialListing = false,
    IReadOnlyList<DiffHunk>? Patch = null,
    string StopReason = "",
    int OutputTokens = 0,
    long? DurationMs = null);
