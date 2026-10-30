using System;
using System.Collections.Generic;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>Builders of domain objects for the tests.</summary>
public static class TestData
{
    /// <summary>A fixed point in time.</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a tab.</summary>
    /// <param name="cwd">Its working directory.</param>
    /// <param name="title">Its title.</param>
    /// <returns>A tab with fresh random ids.</returns>
    public static SessionTab Tab(string cwd = "/home/joff/Sources", string? title = null)
    {
        return new SessionTab(new TabId(Guid.NewGuid()), cwd, new SessionId(Guid.NewGuid()), title);
    }

    /// <summary>Builds a window.</summary>
    /// <param name="status">Its status.</param>
    /// <param name="tabs">Its tabs.</param>
    /// <returns>A window with a fresh random id, its first tab selected.</returns>
    public static SessionWindow Window(WindowStatus status, params SessionTab[] tabs)
    {
        return new SessionWindow(new WindowId(Guid.NewGuid()), status, WindowBounds.Default, new List<SessionTab>(tabs), null);
    }
}
