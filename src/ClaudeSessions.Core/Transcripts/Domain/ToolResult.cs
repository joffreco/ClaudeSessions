using System.Collections.Generic;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>What a tool returned to the model.</summary>
/// <param name="Text">The result; an image appears as [image].</param>
/// <param name="IsError">True when the tool failed or the call was refused.</param>
/// <param name="Patch">For a tool that changed a file (Edit, Write), the change as Claude Code showed it; null otherwise.</param>
public sealed record ToolResult(string Text, bool IsError, IReadOnlyList<DiffHunk>? Patch = null);

/// <summary>One block of a file change, in the unified diff form.</summary>
/// <param name="OldStart">Number of its first line in the file before the change; 0 for a new file.</param>
/// <param name="NewStart">Number of its first line in the file after the change.</param>
/// <param name="Lines">Its lines, each starting with <c>-</c> (removed), <c>+</c> (added) or a space (unchanged).</param>
public sealed record DiffHunk(int OldStart, int NewStart, IReadOnlyList<string> Lines);
