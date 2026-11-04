using System.Collections.Generic;
using System.Linq;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Core.Consignes.Application;

/// <summary>Arranges the consignes into the groups the list shows.</summary>
public static class ConsigneBoard
{
    /// <summary>
    /// One group per session folder: open tabs and sessions outside a tab first, then closed tabs; within each
    /// part, the group with the most recent consigne first.
    /// </summary>
    /// <param name="consignes">Every consigne.</param>
    /// <param name="tabs">The tabs ClaudeSessions knows of; null when unknown, and then no tab counts as closed.</param>
    /// <param name="state">What the user already read.</param>
    /// <returns>The groups.</returns>
    public static IReadOnlyList<TabGroup> Build(IReadOnlyList<Consigne> consignes, IReadOnlyDictionary<string, TabInfo>? tabs, ReadingState state)
    {
        return consignes
            .GroupBy(consigne => consigne.Key.Folder)
            .Select(group => GroupOf(group.OrderByDescending(consigne => consigne.Updated).ToList(), tabs, state))
            .OrderBy(group => group.IsClosed)
            .ThenByDescending(group => group.Consignes[0].Updated)
            .ToList();
    }

    private static TabGroup GroupOf(List<Consigne> ordered, IReadOnlyDictionary<string, TabInfo>? tabs, ReadingState state)
    {
        Consigne latest = ordered[0];
        TabInfo? tab = latest.TabId.Length > 0 && tabs is not null && tabs.TryGetValue(latest.TabId, out TabInfo? found) ? found : null;
        bool closed = latest.TabId.Length > 0 && tabs is not null && tab?.IsOpen != true;
        return new TabGroup(latest.Key.Folder, TabLabel.Of(latest, tab), closed, ordered, ordered.Where(state.IsUnread).Select(consigne => consigne.Key).ToHashSet());
    }
}
