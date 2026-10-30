using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Interfaces;

/// <summary>Keeps the view preferences between two runs of the application.</summary>
public interface IPreferencesStore
{
    /// <summary>Reads the saved preferences.</summary>
    /// <returns>The saved preferences, or <see cref="ViewPreferences.None"/> when nothing usable is saved.</returns>
    ViewPreferences Load();

    /// <summary>Saves the preferences, replacing the previous ones.</summary>
    /// <param name="preferences">The preferences to save.</param>
    void Save(ViewPreferences preferences);
}
