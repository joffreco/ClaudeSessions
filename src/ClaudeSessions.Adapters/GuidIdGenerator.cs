using System;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Adapters;

/// <summary>Creates random version 4 UUIDs.</summary>
public sealed class GuidIdGenerator : IIdGenerator
{
    /// <inheritdoc />
    public Guid NewId()
    {
        return Guid.NewGuid();
    }
}
