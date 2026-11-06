using System;
using System.IO;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Adapters;

/// <summary>
/// Watches the folder where claude-sessions-hook.sh writes, on the Windows side so that changes are
/// notified at once: &lt;tab_id&gt;.json when a tab runs a new session, &lt;tab_id&gt;.approval when its session
/// asks the user for an approval. An approval file is a one-shot signal, deleted once reported.
/// Events are raised on a thread pool thread.
/// </summary>
public sealed class SessionRecordWatcher : IDisposable
{
    private const string RecordExtension = ".json";
    private const string ApprovalExtension = ".approval";

    private readonly string _directory;
    private readonly FileSystemWatcher _watcher;

    /// <summary>Creates the folder when needed, drops the signals of an earlier run, and starts watching.</summary>
    /// <param name="directory">The folder.</param>
    public SessionRecordWatcher(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
        foreach (string stale in Directory.EnumerateFiles(directory, "*" + ApprovalExtension))
        {
            DeleteQuietly(stale);
        }
        _watcher = new FileSystemWatcher(directory)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        };
        _watcher.Created += (_, e) => OnFile(e.Name);
        _watcher.Changed += (_, e) => OnFile(e.Name);
        _watcher.Renamed += (_, e) => OnFile(e.Name);
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>Raised when the session record of a tab changes.</summary>
    public event Action<TabId>? RecordChanged;

    /// <summary>Raised when the session of a tab asks the user for an approval.</summary>
    public event Action<TabId>? ApprovalRequested;

    /// <summary>Tells what a file of the folder means.</summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="tabId">The tab it is about, when it is a record or a signal.</param>
    /// <param name="isApproval">True for an approval signal, false for a session record.</param>
    /// <returns>False for any other file, such as a temporary file of the hook.</returns>
    public static bool TryParseName(string? fileName, out TabId tabId, out bool isApproval)
    {
        tabId = default;
        isApproval = false;
        string extension = Path.GetExtension(fileName ?? "");
        if (extension is not (RecordExtension or ApprovalExtension))
        {
            return false;
        }
        isApproval = extension == ApprovalExtension;
        return TabId.TryParse(Path.GetFileNameWithoutExtension(fileName), out tabId);
    }

    /// <summary>Stops watching.</summary>
    public void Dispose()
    {
        _watcher.Dispose();
    }

    private void OnFile(string? fileName)
    {
        if (!TryParseName(fileName, out TabId tabId, out bool isApproval))
        {
            return;
        }
        if (!isApproval)
        {
            RecordChanged?.Invoke(tabId);
            return;
        }
        string path = Path.Combine(_directory, fileName!);
        if (File.Exists(path))
        {
            DeleteQuietly(path);
            ApprovalRequested?.Invoke(tabId);
        }
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
