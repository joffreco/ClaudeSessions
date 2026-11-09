using System;

namespace ClaudeSessions.Core.Domain;

/// <summary>A running tab, as the window sees it, offered to the idle suspension.</summary>
/// <param name="TabId">The tab.</param>
/// <param name="SessionId">Session the tab runs right now.</param>
/// <param name="WaitsForUser">True when the last title of the session carries the waiting mark.</param>
/// <param name="IsOnScreen">True when the tab is the shown tab of the active window.</param>
/// <param name="LastTouched">Last launch, keystroke or look of the user at the tab.</param>
public sealed record IdleCandidate(TabId TabId, SessionId SessionId, bool WaitsForUser, bool IsOnScreen, DateTimeOffset LastTouched);
