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

    /// <summary>Writes a file under the folder, creating its directories.</summary>
    /// <param name="relative">The file's path inside the folder, with forward slashes.</param>
    /// <param name="content">Its content.</param>
    /// <returns>Its full path.</returns>
    public string Write(string relative, string content)
    {
        string file = System.IO.Path.Combine([Path, .. relative.Split('/')]);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
        System.IO.File.WriteAllText(file, content);
        return file;
    }

    /// <summary>Deletes the folder and its content.</summary>
    public void Dispose()
    {
        Directory.Delete(Path, true);
    }
}
