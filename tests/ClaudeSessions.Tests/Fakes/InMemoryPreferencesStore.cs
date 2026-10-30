using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>Preferences store kept in memory, counting its saves.</summary>
public sealed class InMemoryPreferencesStore : IPreferencesStore
{
    /// <summary>Creates a store holding preferences.</summary>
    /// <param name="initial">The preferences returned by the first load.</param>
    public InMemoryPreferencesStore(ViewPreferences initial)
    {
        Saved = initial;
    }

    /// <summary>The last preferences saved, or the initial ones.</summary>
    public ViewPreferences Saved { get; private set; }

    /// <summary>Number of saves.</summary>
    public int SaveCount { get; private set; }

    /// <inheritdoc />
    public ViewPreferences Load()
    {
        return Saved;
    }

    /// <inheritdoc />
    public void Save(ViewPreferences preferences)
    {
        Saved = preferences;
        SaveCount++;
    }
}
