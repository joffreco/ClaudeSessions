using System;

namespace ClaudeSessions.Core.Domain;

/// <summary>A tab closed by the user, kept so that it can be reopened.</summary>
/// <param name="Tab">The tab as it was when it closed.</param>
/// <param name="WindowId">Window the tab belonged to.</param>
/// <param name="ClosedAt">When the tab was closed.</param>
public sealed record ClosedTab(SessionTab Tab, WindowId WindowId, DateTimeOffset ClosedAt);
