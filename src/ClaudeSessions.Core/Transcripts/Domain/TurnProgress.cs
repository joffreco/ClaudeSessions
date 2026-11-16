using System;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A turn under way: Claude is working on the last prompt.</summary>
/// <param name="Started">When the prompt was sent; null when the transcript part read does not reach it.</param>
/// <param name="OutputTokens">Tokens the model wrote since the prompt, as far as the transcript tells.</param>
/// <param name="RunningTool">Name of the tool whose result is awaited; null between two calls.</param>
public sealed record TurnProgress(DateTimeOffset? Started, int OutputTokens, string? RunningTool);
