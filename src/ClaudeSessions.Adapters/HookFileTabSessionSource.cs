using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ClaudeSessions.Adapters.Dto;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Adapters;

/// <summary>
/// Reads the records the session-registry hook writes in ~/.claude/session-tabs, one file per tab,
/// reached from Windows through \\wsl.localhost.
/// </summary>
public sealed class HookFileTabSessionSource : ITabSessionSource
{
    /// <summary>Largest record accepted; a bigger file is not a hook record.</summary>
    public const long MaxRecordBytes = 16 * 1024;

    private readonly string _directory;

    /// <summary>Creates the source.</summary>
    /// <param name="directory">Folder of the records, as Windows sees it.</param>
    public HookFileTabSessionSource(string directory)
    {
        _directory = directory;
    }

    /// <inheritdoc />
    public Task<SessionId?> ReadAsync(TabId tabId)
    {
        return Task.Run(() => Read(tabId));
    }

    /// <inheritdoc />
    public void Forget(TabId tabId)
    {
        try
        {
            File.Delete(RecordPath(tabId));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private SessionId? Read(TabId tabId)
    {
        try
        {
            FileInfo file = new(RecordPath(tabId));
            if (!file.Exists || file.Length > MaxRecordBytes)
            {
                return null;
            }
            HookRecordDocument? record = JsonSerializer.Deserialize<HookRecordDocument>(File.ReadAllText(file.FullName));
            return Validate(record, tabId);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static SessionId? Validate(HookRecordDocument? record, TabId tabId)
    {
        if (record is null || !TabId.TryParse(record.TabId, out TabId recordedTab) || recordedTab != tabId)
        {
            return null;
        }
        return SessionId.TryParse(record.SessionId, out SessionId sessionId) ? sessionId : null;
    }

    private string RecordPath(TabId tabId)
    {
        return Path.Combine(_directory, tabId + ".json");
    }
}
