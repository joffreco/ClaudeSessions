using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Interfaces;

/// <summary>Keeps the workspace between two runs of the application.</summary>
public interface IWorkspaceStore
{
    /// <summary>Reads the saved workspace.</summary>
    /// <returns>The saved workspace, or an empty one when nothing usable is saved.</returns>
    Workspace Load();

    /// <summary>Saves the workspace, replacing the previous state in one step.</summary>
    /// <param name="workspace">The workspace to save.</param>
    void Save(Workspace workspace);
}
