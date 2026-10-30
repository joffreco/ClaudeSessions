using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Application.Dto;

/// <summary>What a terminal needs to start the Claude Code session of a tab.</summary>
/// <param name="TabId">The tab, exported to the session so that the hook can record it.</param>
/// <param name="SessionId">The session to resume, or to create under this identifier when it has no transcript yet.</param>
/// <param name="Cwd">WSL working directory of the session.</param>
public sealed record TabLaunchRequest(TabId TabId, SessionId SessionId, string Cwd);
