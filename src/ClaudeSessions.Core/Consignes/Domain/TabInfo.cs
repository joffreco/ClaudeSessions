namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>What ClaudeSessions knows of a tab.</summary>
/// <param name="Title">The tab's title; null when it has none.</param>
/// <param name="IsOpen">True when the tab is in an open window, false when it was closed.</param>
public sealed record TabInfo(string? Title, bool IsOpen);
