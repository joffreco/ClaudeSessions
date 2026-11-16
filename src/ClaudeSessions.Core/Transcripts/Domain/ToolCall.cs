namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A tool the model called, with its result once it came back.</summary>
/// <param name="Id">The call's identifier.</param>
/// <param name="Name">The tool, such as Bash or Edit.</param>
/// <param name="Input">The call's arguments, as JSON.</param>
/// <param name="Result">What the tool returned; null while it runs.</param>
public sealed record ToolCall(string Id, string Name, string Input, ToolResult? Result) : AssistantBlock;
