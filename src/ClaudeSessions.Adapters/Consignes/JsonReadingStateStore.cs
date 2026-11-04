using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClaudeSessions.Adapters.Consignes.Dto;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;

namespace ClaudeSessions.Adapters.Consignes;

/// <summary>Keeps the reading state in a JSON file, replaced atomically on every save.</summary>
public sealed class JsonReadingStateStore : IReadingStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private readonly string _path;

    /// <summary>Creates the store.</summary>
    /// <param name="path">The path of state.json.</param>
    public JsonReadingStateStore(string path)
    {
        _path = path;
    }

    /// <inheritdoc/>
    public ReadingState Load()
    {
        try
        {
            ReadingStateDocument? document = File.Exists(_path) ? JsonSerializer.Deserialize<ReadingStateDocument>(File.ReadAllText(_path), SerializerOptions) : null;
            return document is null
                ? new ReadingState()
                : new ReadingState(
                    new Dictionary<string, DateTimeOffset>(document.Seen ?? [], StringComparer.Ordinal),
                    (document.Ticked ?? []).ToDictionary(pair => pair.Key, pair => pair.Value.ToHashSet(), StringComparer.Ordinal));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new ReadingState();
        }
    }

    /// <inheritdoc/>
    public void Save(ReadingState state)
    {
        ReadingStateDocument document = new()
        {
            Seen = new Dictionary<string, DateTimeOffset>(state.Seen),
            Ticked = state.Ticked.ToDictionary(pair => pair.Key, pair => pair.Value.Order().ToList()),
        };
        string directory = Path.GetDirectoryName(Path.GetFullPath(_path))!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, "." + Path.GetFileName(_path) + ".tmp");
        File.WriteAllText(temporary, JsonSerializer.Serialize(document, SerializerOptions));
        File.Move(temporary, _path, overwrite: true);
    }
}
