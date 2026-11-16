namespace ClaudeSessions.Adapters.Transcripts.Dto;

/// <summary>One content block of a transcript message.</summary>
/// <param name="Type">text, thinking, image, tool_use or tool_result.</param>
/// <param name="Text">The text, the thinking summary or the tool result.</param>
/// <param name="ToolUseId">For tool_use, the call's id; for tool_result, the call it answers.</param>
/// <param name="Name">For tool_use, the tool.</param>
/// <param name="Input">For tool_use, the arguments as JSON.</param>
/// <param name="IsError">For tool_result, true when the call failed.</param>
public sealed record TranscriptBlock(string Type, string Text, string ToolUseId, string Name, string Input, bool IsError);
