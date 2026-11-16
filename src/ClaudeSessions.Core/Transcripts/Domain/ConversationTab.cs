using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A tab of an open ClaudeSessions window, as the conversation viewer lists it.</summary>
/// <param name="Id">The tab.</param>
/// <param name="Title">The tab's title, or its folder name when it has none.</param>
/// <param name="Cwd">The WSL directory of its session.</param>
/// <param name="Session">The session it runs now.</param>
/// <param name="WindowId">Its window.</param>
/// <param name="Window">The position of its window among the open ones, from 1.</param>
public sealed record ConversationTab(TabId Id, string Title, string Cwd, SessionId Session, WindowId WindowId, int Window);
