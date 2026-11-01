using System.IO;
using System.Text.Json;

namespace ClaudeSessions.Adapters;

/// <summary>Reads install-source.json, which install.ps1 writes next to the app it installs from a WSL repository.</summary>
public static class InstallSourceFile
{
    /// <summary>Name of the file in the installation folder.</summary>
    public const string FileName = "install-source.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Loads the source of the installation.</summary>
    /// <param name="path">Path of install-source.json.</param>
    /// <returns>The source; null when the file is missing, unreadable or incomplete.</returns>
    public static InstallSource? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            InstallSource? source = JsonSerializer.Deserialize<InstallSource>(File.ReadAllText(path), SerializerOptions);
            // A missing property comes back null despite the non-nullable type, hence the patterns.
            return source is { Distro: { Length: > 0 }, RepoPath: { } repo } && repo.StartsWith('/') ? source : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
