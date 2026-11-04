using System.Collections.Generic;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;

namespace ClaudeSessions.Tests.Consignes.Fakes;

/// <summary>A consignes folder held in memory.</summary>
/// <param name="consignes">The consignes it holds.</param>
public sealed class FakeConsigneSource(IReadOnlyList<Consigne> consignes) : IConsigneSource
{
    /// <inheritdoc/>
    public IReadOnlyList<Consigne> ReadAll()
    {
        return consignes;
    }

    /// <inheritdoc/>
    public void SendTick(ConsigneKey key, StepTick tick)
    {
    }

    /// <inheritdoc/>
    public void Archive(ConsigneKey key)
    {
    }

    /// <inheritdoc/>
    public string PathOf(string folder)
    {
        return folder;
    }
}
