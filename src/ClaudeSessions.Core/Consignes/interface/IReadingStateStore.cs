using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Core.Consignes.Interfaces;

/// <summary>Keeps what the user did with the consignes across runs.</summary>
public interface IReadingStateStore
{
    /// <summary>Loads the saved state.</summary>
    /// <returns>The state; empty when none was saved or it cannot be read.</returns>
    ReadingState Load();

    /// <summary>Saves the state.</summary>
    /// <param name="state">The state.</param>
    void Save(ReadingState state);
}
