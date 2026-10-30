using System.IO;
using System.Text.Json;
using ClaudeSessions.Adapters.Dto;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Adapters;

/// <summary>
/// Keeps the workspace in a JSON file. Each save writes a temporary file and moves it over the
/// previous one; an unreadable file is set aside with a .bad suffix and an empty workspace is used.
/// </summary>
public sealed class JsonWorkspaceStore : IWorkspaceStore
{
    /// <summary>Suffix of an unreadable workspace file set aside.</summary>
    public const string BadFileSuffix = ".bad";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;

    /// <summary>Creates the store.</summary>
    /// <param name="path">Path of the workspace file; its folder is created on the first save.</param>
    public JsonWorkspaceStore(string path)
    {
        _path = path;
    }

    /// <inheritdoc />
    public Workspace Load()
    {
        if (!File.Exists(_path))
        {
            return Workspace.Empty();
        }
        try
        {
            WorkspaceDocument? document = JsonSerializer.Deserialize<WorkspaceDocument>(File.ReadAllText(_path), SerializerOptions);
            return document is null ? SetAside() : WorkspaceDocumentMapper.ToWorkspace(document);
        }
        catch (JsonException)
        {
            return SetAside();
        }
        catch (InvalidDataException)
        {
            return SetAside();
        }
    }

    /// <inheritdoc />
    public void Save(Workspace workspace)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        string temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(WorkspaceDocumentMapper.ToDocument(workspace), SerializerOptions));
        File.Move(temporary, _path, true);
    }

    private Workspace SetAside()
    {
        File.Move(_path, _path + BadFileSuffix, true);
        return Workspace.Empty();
    }
}
