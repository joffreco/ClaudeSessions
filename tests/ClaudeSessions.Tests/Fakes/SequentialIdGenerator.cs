using System;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>Id generator returning 00000000-0000-0000-0000-000000000001, …02, and so on.</summary>
public sealed class SequentialIdGenerator : IIdGenerator
{
    private int _next;

    /// <summary>Builds the n-th id this generator returns.</summary>
    /// <param name="index">1 for the first id.</param>
    /// <returns>The id.</returns>
    public static Guid IdAt(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:D12}");
    }

    /// <inheritdoc />
    public Guid NewId()
    {
        _next++;
        return IdAt(_next);
    }
}
