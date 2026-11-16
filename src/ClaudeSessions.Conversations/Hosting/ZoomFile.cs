using System;
using System.IO;
using System.Text.Json;

namespace ClaudeSessions.Conversations.Hosting;

/// <summary>Keeps the zoom of the Conversations window between two runs, in a small JSON file.</summary>
/// <param name="path">The file.</param>
internal sealed class ZoomFile(string path)
{
    /// <summary>The zoom when none was kept: the window at its natural size.</summary>
    public const double Default = 1.0;

    /// <summary>Reads the kept zoom.</summary>
    /// <returns>The zoom; <see cref="Default"/> when the file is missing or unreadable.</returns>
    public double Load()
    {
        try
        {
            ZoomDocument? document = JsonSerializer.Deserialize<ZoomDocument>(File.ReadAllText(path));
            return document is { Zoom: > 0 and < 10 } ? document.Zoom : Default;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Default;
        }
    }

    /// <summary>Keeps a zoom; a failure is ignored, the zoom is only a preference.</summary>
    /// <param name="zoom">The zoom.</param>
    public void Save(double zoom)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new ZoomDocument(zoom)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed record ZoomDocument(double Zoom);
}
