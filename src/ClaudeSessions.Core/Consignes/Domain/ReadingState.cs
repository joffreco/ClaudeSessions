using System;
using System.Collections.Generic;

namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>What the user did with the consignes: which version of each was seen, which steps were ticked.</summary>
public sealed class ReadingState
{
    private readonly Dictionary<string, DateTimeOffset> _seen;
    private readonly Dictionary<string, HashSet<int>> _ticked;

    /// <summary>Creates an empty state.</summary>
    public ReadingState()
        : this(new Dictionary<string, DateTimeOffset>(), new Dictionary<string, HashSet<int>>())
    {
    }

    /// <summary>Creates a state from saved values.</summary>
    /// <param name="seen">The update time seen for each consigne, by <see cref="ConsigneKey.Text"/>.</param>
    /// <param name="ticked">The ticked task indexes of each consigne, by <see cref="ConsigneKey.Text"/>.</param>
    public ReadingState(Dictionary<string, DateTimeOffset> seen, Dictionary<string, HashSet<int>> ticked)
    {
        _seen = seen;
        _ticked = ticked;
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
        int before = _seen.Count + _ticked.Count;
        foreach (string key in new List<string>(_seen.Keys))
        {
            if (!existing.Contains(key))
            {
                _seen.Remove(key);
            }
        }
        foreach (string key in new List<string>(_ticked.Keys))
        {
            if (!existing.Contains(key))
            {
                _ticked.Remove(key);
            }
        }
        return _seen.Count + _ticked.Count != before;
    }
}
