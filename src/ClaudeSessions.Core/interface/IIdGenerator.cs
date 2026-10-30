using System;

namespace ClaudeSessions.Core.Interfaces;

/// <summary>Source of new unique identifiers.</summary>
public interface IIdGenerator
{
    /// <summary>Creates a new identifier.</summary>
    /// <returns>A UUID never returned before.</returns>
    Guid NewId();
}
