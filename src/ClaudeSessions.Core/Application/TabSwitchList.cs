using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClaudeSessions.Core.Application.Dto;

namespace ClaudeSessions.Core.Application;

/// <summary>Filters and orders the tabs of the quick switcher.</summary>
public static class TabSwitchList
{
    private const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>Keeps the tabs that match and puts first those that expect something from the user.</summary>
    /// <param name="entries">The tabs, in the order to keep among equals.</param>
    /// <param name="filter">Words typed by the user; each must appear in the title or the directory, ignoring case and accents.</param>
    /// <returns>The approvals, then the finished sessions, then the others.</returns>
    public static IReadOnlyList<TabSwitchEntry> Arrange(IEnumerable<TabSwitchEntry> entries, string filter)
    {
        string[] words = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return entries
            .Where(entry => words.All(word => Contains(entry.Title, word) || Contains(entry.Cwd, word)))
            .OrderByDescending(entry => entry.Attention)
            .ToList();
    }

    private static bool Contains(string text, string word)
    {
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, word, Loose) >= 0;
    }
}
