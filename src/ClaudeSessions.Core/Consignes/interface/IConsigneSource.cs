using System.Collections.Generic;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Core.Consignes.Interfaces;

/// <summary>Where the sessions deposit their consignes.</summary>
public interface IConsigneSource
{
    /// <summary>Reads every consigne not archived.</summary>
    /// <returns>The consignes, in no particular order.</returns>
    IReadOnlyList<Consigne> ReadAll();

    /// <summary>Tells the session that deposited a consigne that the user ticked one of its marked steps.</summary>
    /// <param name="key">The consigne.</param>
    /// <param name="tick">The step ticked.</param>
    void SendTick(ConsigneKey key, StepTick tick);

    /// <summary>Moves a consigne, and what the user ticked in it, to its folder's archive.</summary>
    /// <param name="key">The consigne.</param>
    void Archive(ConsigneKey key);

    /// <summary>The Windows path of a session folder, to open it in Explorer.</summary>
    /// <param name="folder">The session folder.</param>
    /// <returns>Its path.</returns>
    string PathOf(string folder);
}
