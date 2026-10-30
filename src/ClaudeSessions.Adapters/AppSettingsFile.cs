using System.IO;
using System.Text.Json;

namespace ClaudeSessions.Adapters;

/// <summary>Reads settings.json; writes the defaults there the first time so that they can be edited.</summary>
public sealed class AppSettingsFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Loads the settings.</summary>
    /// <param name="path">Path of settings.json.</param>
    /// <returns>The settings; the defaults when the file is missing or unreadable.</returns>
    public static AppSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            WriteDefaults(path);
            return new AppSettings();
        }
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), SerializerOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    private static void WriteDefaults(string path)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        File.WriteAllText(path, JsonSerializer.Serialize(new AppSettings(), SerializerOptions));
    }
}
