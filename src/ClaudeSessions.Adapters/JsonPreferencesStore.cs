using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeSessions.Adapters.Dto;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Adapters;

/// <summary>
/// Keeps the view preferences in a small JSON file. No file, or an unreadable one, means no
/// preference; saving no preference at all deletes the file.
/// </summary>
public sealed class JsonPreferencesStore : IPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;

    /// <summary>Creates the store.</summary>
    /// <param name="path">Path of the preferences file; its folder is created on the first save.</param>
    public JsonPreferencesStore(string path)
    {
        _path = path;
    }

    /// <inheritdoc />
    public ViewPreferences Load()
    {
        if (!File.Exists(_path))
        {
            return ViewPreferences.None;
        }
        try
        {
            PreferencesDocument? document = JsonSerializer.Deserialize<PreferencesDocument>(File.ReadAllText(_path), SerializerOptions);
            return document is null ? ViewPreferences.None : new ViewPreferences(document.FontSize, document.WheelLines, document.SuspendAfterHours);
        }
        catch (JsonException)
        {
            return ViewPreferences.None;
        }
    }

    /// <inheritdoc />
    public void Save(ViewPreferences preferences)
    {
        if (preferences == ViewPreferences.None)
        {
            File.Delete(_path);
            return;
        }
        string? folder = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        PreferencesDocument document = new() { FontSize = preferences.FontSize, WheelLines = preferences.WheelLines, SuspendAfterHours = preferences.SuspendAfterHours };
        File.WriteAllText(_path, JsonSerializer.Serialize(document, SerializerOptions));
    }
}
