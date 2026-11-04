using System;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Tests.Consignes.Fakes;

/// <summary>Builds consignes for the core tests.</summary>
public static class Make
{
    /// <summary>A consigne.</summary>
    /// <param name="folder">Its session folder.</param>
    /// <param name="id">Its id.</param>
    /// <param name="minute">Its update time, in minutes after 10:00.</param>
    /// <param name="tabId">Its tab id; empty outside a tab.</param>
    /// <param name="tabTitle">The tab title recorded when it was deposited.</param>
    /// <param name="cwd">Its working directory.</param>
    /// <returns>The consigne.</returns>
    public static Consigne Consigne(string folder, string id, int minute = 0, string tabId = "t", string tabTitle = "", string cwd = "/src/repo")
    {
        DateTimeOffset updated = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero).AddMinutes(minute);
        return new Consigne(new ConsigneKey(folder, id), id, ConsigneStatus.Ready, tabId, tabTitle, "", cwd, updated, updated, "corps");
    }
}
