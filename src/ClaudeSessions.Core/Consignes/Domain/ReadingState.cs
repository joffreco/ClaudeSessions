using System;
using System.Collections.Generic;

namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>What the user did with the consignes: which version of each was seen, which steps were ticked.</summary>
public sealed class ReadingState
{
    private readonly Dictionary<string, DateTimeOffset> _seen;
    private readonly Dictionary<string, HashSet<int>> _ticked;
    private readonly Dictionary<string, Dictionary<string, string>> _results;

    /// <summary>Creates an empty state.</summary>
    public ReadingState()
        : this(new Dictionary<string, DateTimeOffset>(), new Dictionary<string, HashSet<int>>(), new Dictionary<string, Dictionary<string, string>>())
    {
    }

    /// <summary>Creates a state from saved values.</summary>
    /// <param name="seen">The update time seen for each consigne, by <see cref="ConsigneKey.Text"/>.</param>
    /// <param name="ticked">The ticked task indexes of each consigne, by <see cref="ConsigneKey.Text"/>.</param>
    /// <param name="results">The results sent from each consigne's fields, by <see cref="ConsigneKey.Text"/>, then by field name.</param>
    public ReadingState(Dictionary<string, DateTimeOffset> seen, Dictionary<string, HashSet<int>> ticked, Dictionary<string, Dictionary<string, string>> results)
    {
        _seen = seen;
        _ticked = ticked;
        _results = results;
    }

    /// <summary>Gets the results sent from each consigne's fields, by field name.</summary>
    public IReadOnlyDictionary<string, Dictionary<string, string>> Results => _results;

    /// <summary>The result last sent from a consigne's field.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="name">The field's name.</param>
    /// <returns>The text sent; empty when none was.</returns>
    public string ResultOf(ConsigneKey key, string name)
    {
        return _results.TryGetValue(key.Text, out Dictionary<string, string>? fields) && fields.TryGetValue(name, out string? text) ? text : "";
    }

    /// <summary>Records the result sent from a consigne's field.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="name">The field's name.</param>
    /// <param name="text">The text sent.</param>
    public void SetResult(ConsigneKey key, string name, string text)
    {
        if (!_results.TryGetValue(key.Text, out Dictionary<string, string>? fields))
        {
            fields = new Dictionary<string, string>(StringComparer.Ordinal);
            _results[key.Text] = fields;
        }
        fields[name] = text;
    }

    /// <summary>Gets the update time seen for each consigne.</summary>
    public IReadOnlyDictionary<string, DateTimeOffset> Seen => _seen;

    /// <summary>Gets the ticked task indexes of each consigne.</summary>
    public IReadOnlyDictionary<string, HashSet<int>> Ticked => _ticked;

    /// <summary>Tells whether a consigne changed since the user last opened it.</summary>
    /// <param name="consigne">The consigne.</param>
    /// <returns>True when it was never opened, or updated since.</returns>
    public bool IsUnread(Consigne consigne)
    {
        return !_seen.TryGetValue(consigne.Key.Text, out DateTimeOffset seen) || seen < consigne.Updated;
    }

    /// <summary>Records that the user opened a consigne in its current version.</summary>
    /// <param name="consigne">The consigne.</param>
    /// <returns>True when this changed the state.</returns>
    public bool MarkSeen(Consigne consigne)
    {
        if (!IsUnread(consigne))
        {
            return false;
        }
        _seen[consigne.Key.Text] = consigne.Updated;
        return true;
    }

    /// <summary>Tells whether a task of a consigne is ticked.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="index">The task's position among the consigne's tasks, from 0.</param>
    /// <returns>True when ticked.</returns>
    public bool IsTicked(ConsigneKey key, int index)
    {
        return _ticked.TryGetValue(key.Text, out HashSet<int>? indexes) && indexes.Contains(index);
    }

    /// <summary>Ticks or unticks a task of a consigne.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="index">The task's position among the consigne's tasks, from 0.</param>
    /// <param name="ticked">The new state.</param>
    public void SetTicked(ConsigneKey key, int index, bool ticked)
    {
        if (!_ticked.TryGetValue(key.Text, out HashSet<int>? indexes))
        {
            indexes = [];
            _ticked[key.Text] = indexes;
        }
        if (ticked)
        {
            indexes.Add(index);
        }
        else
        {
            indexes.Remove(index);
        }
        if (indexes.Count == 0)
        {
            _ticked.Remove(key.Text);
        }
    }

    /// <summary>Forgets the consignes that no longer exist.</summary>
    /// <param name="existing">The keys of the consignes still present.</param>
    /// <returns>True when something was forgotten.</returns>
    public bool Retain(IReadOnlySet<string> existing)
    {
        return (RemoveAbsent(_seen, existing) | RemoveAbsent(_ticked, existing) | RemoveAbsent(_results, existing)) > 0;
    }

    private static int RemoveAbsent<TValue>(Dictionary<string, TValue> byKey, IReadOnlySet<string> existing)
    {
        int removed = 0;
        foreach (string key in new List<string>(byKey.Keys))
        {
            if (!existing.Contains(key))
            {
                byKey.Remove(key);
                removed++;
            }
        }
        return removed;
    }
}
