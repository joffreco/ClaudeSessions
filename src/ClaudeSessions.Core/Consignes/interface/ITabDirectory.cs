using System.Collections.Generic;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Core.Consignes.Interfaces;

/// <summary>The tabs ClaudeSessions knows of.</summary>
public interface ITabDirectory
{
    /// <summary>Reads the tabs.</summary>
    /// <returns>The tabs by id; null when ClaudeSessions' file cannot be read.</returns>
    IReadOnlyDictionary<string, TabInfo>? Read();
}
