using System.Collections.Generic;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Consignes.Interfaces;

/// <summary>What the board controller shows.</summary>
public interface IBoardView
{
    /// <summary>Shows the groups as last read.</summary>
    /// <param name="groups">The groups.</param>
    void ShowGroups(IReadOnlyList<TabGroup> groups);
}
