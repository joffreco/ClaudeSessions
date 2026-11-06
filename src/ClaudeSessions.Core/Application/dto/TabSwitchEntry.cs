using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Application.Dto;

/// <summary>A tab as the quick switcher lists it.</summary>
/// <param name="TabId">The tab.</param>
/// <param name="Title">The text on the tab.</param>
/// <param name="Cwd">WSL working directory of its session.</param>
/// <param name="Attention">What it expects from the user.</param>
public sealed record TabSwitchEntry(TabId TabId, string Title, string Cwd, TabAttention Attention);
