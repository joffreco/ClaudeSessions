using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;

namespace ClaudeSessions.Tests.Consignes.Fakes;

/// <summary>A reading state kept in memory.</summary>
/// <param name="state">The state it gives back.</param>
public sealed class InMemoryReadingStateStore(ReadingState state) : IReadingStateStore
{
    /// <inheritdoc/>
    public ReadingState Load()
    {
        return state;
    }

    /// <inheritdoc/>
    public void Save(ReadingState state)
    {
    }
}
