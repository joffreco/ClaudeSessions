using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>Workspace store kept in memory, counting its saves.</summary>
public sealed class InMemoryWorkspaceStore : IWorkspaceStore
{
    /// <summary>Creates a store holding a workspace.</summary>
    /// <param name="initial">The workspace returned by the first load.</param>
    public InMemoryWorkspaceStore(Workspace initial)
    {
        Saved = initial;
    }

    /// <summary>The last workspace saved, or the initial one.</summary>
    public Workspace Saved { get; private set; }

    /// <summary>Number of saves.</summary>
    public int SaveCount { get; private set; }

    /// <summary>Sets the save counter back to zero.</summary>
    public void ResetCount()
    {
        SaveCount = 0;
    }

    /// <inheritdoc />
    public Workspace Load()
    {
        return Saved;
    }

    /// <inheritdoc />
    public void Save(Workspace workspace)
    {
        Saved = workspace;
        SaveCount++;
    }
}
