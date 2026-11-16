using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Reads the status lines that <c>wsl/claude-sessions-statusline</c> keeps, two files per session in
/// <c>~/.claude/session-status</c>: <c>&lt;session&gt;.txt</c>, a copy of what the user's own status line
/// command printed for Claude Code, and <c>&lt;session&gt;.json</c>, the input Claude Code gave it, from which
/// the session name is read so that its segment is left out.
/// </summary>
public sealed class StatusLineFiles : IStatusLineSource
{
    /// <summary>Largest file accepted; a bigger one is not a status line.</summary>
    public const long MaxBytes = 64 * 1024;

    private readonly string _directory;

    /// <summary>Creates the source.</summary>
    /// <param name="directory">The ~/.claude/session-status folder, as Windows sees it.</param>
    public StatusLineFiles(string directory)
    {
        _directory = directory;
    }

    /// <inheritdoc />
    public Task<StatusLine?> ReadAsync(SessionId session)
    {
        return Task.Run(() => Read(session));
    }

    private StatusLine? Read(SessionId session)
    {
        string? text = ReadFile(session + ".txt");
        return text is null ? null : StatusLineText.Parse(text, SessionNameIn(ReadFile(session + ".json")));
    }

    private static string? SessionNameIn(string? input)
    {
        if (input is null)
        {
            return null;
        }
        try
        {
            using JsonDocument document = JsonDocument.Parse(input);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("session_name", out JsonElement name)
                && name.ValueKind == JsonValueKind.String
                ? name.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string? ReadFile(string name)
    {
        try
        {
            FileInfo file = new(Path.Combine(_directory, name));
            if (!file.Exists || file.Length > MaxBytes)
            {
                return null;
            }
            using FileStream stream = new(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader reader = new(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
