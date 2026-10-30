using System;
using System.IO;

namespace ClaudeSessions.Tests.Fakes;

/// <summary>A folder owned by one test, deleted with its content afterwards.</summary>
public sealed class TempFolder : IDisposable
{
    /// <summary>Creates an empty folder under the system temporary directory.</summary>
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "claude-sessions-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Full path of the folder.</summary>
    public string Path { get; }

    /// <summary>Path of a file inside the folder.</summary>
    /// <param name="name">File name.</param>
    /// <returns>The full path.</returns>
    public string File(string name)
    {
        return System.IO.Path.Combine(Path, name);
    }

    /// <summary>Deletes the folder and its content.</summary>
    public void Dispose()
    {
        Directory.Delete(Path, true);
    }
}
