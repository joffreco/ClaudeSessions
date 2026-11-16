using System;
using System.Collections.Generic;
using System.IO;
using ClaudeSessions.Adapters.Transcripts.Dto;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// The message lines of one transcript file, read incrementally: Claude Code only appends to a
/// transcript, so each refresh reads the bytes added since the last one. A line still being
/// written, without its line break, waits for the next refresh.
/// </summary>
internal sealed class TranscriptLog
{
    private readonly List<TranscriptRecord> _records = [];
    private readonly long _tailBytes;
    private readonly int _maxRecords;
    private long _offset;

    /// <summary>Creates the log of a file; nothing is read yet.</summary>
    /// <param name="path">The transcript file.</param>
    /// <param name="tailBytes">When above 0, the first refresh reads only the last bytes of the file, from the first line that starts in them.</param>
    /// <param name="maxRecords">Most lines kept; the oldest are dropped beyond.</param>
    public TranscriptLog(string path, long tailBytes = 0, int maxRecords = int.MaxValue)
    {
        Path = path;
        _tailBytes = tailBytes;
        _maxRecords = maxRecords;
    }

    /// <summary>Gets the transcript file.</summary>
    public string Path { get; }

    /// <summary>Gets the message lines read so far, in file order.</summary>
    public IReadOnlyList<TranscriptRecord> Records => _records;

    /// <summary>Gets how many lines were read in all, the dropped ones included; it grows whenever lines are added.</summary>
    public long Version { get; private set; }

    /// <summary>Gets when the file was last written, as of the last refresh.</summary>
    public DateTimeOffset? LastWrite { get; private set; }

    /// <summary>Reads what was added to the file since the last refresh.</summary>
    /// <returns>False when the file no longer exists.</returns>
    public bool Refresh()
    {
        if (!File.Exists(Path))
        {
            return false;
        }
        using FileStream stream = new(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        LastWrite = File.GetLastWriteTimeUtc(Path);
        if (stream.Length < _offset)
        {
            // Shorter than what was read: the file was replaced, start over.
            _records.Clear();
            _offset = 0;
            Version++;
        }
        bool skipFirstLine = false;
        if (_offset == 0 && _tailBytes > 0 && stream.Length > _tailBytes)
        {
            _offset = stream.Length - _tailBytes;
            skipFirstLine = true;
        }
        long added = stream.Length - _offset;
        if (added == 0)
        {
            return true;
        }
        byte[] bytes = new byte[added];
        stream.Seek(_offset, SeekOrigin.Begin);
        stream.ReadExactly(bytes);
        int start = 0;
        if (skipFirstLine)
        {
            // The tail starts inside a line: drop it up to its line break.
            start = Array.IndexOf(bytes, (byte)'\n') + 1;
            if (start == 0)
            {
                _offset = 0;
                return true;
            }
        }
        _offset += ReadLines(bytes, start);
        if (_records.Count > _maxRecords)
        {
            _records.RemoveRange(0, _records.Count - _maxRecords);
        }
        return true;
    }

    // Parses every complete line from start and returns how many bytes were consumed, start included.
    private int ReadLines(byte[] bytes, int start)
    {
        for (int index = Array.IndexOf(bytes, (byte)'\n', start); index >= 0; index = Array.IndexOf(bytes, (byte)'\n', start))
        {
            int length = index - start;
            if (length > 0 && bytes[index - 1] == '\r')
            {
                length--;
            }
            if (length > 0 && TranscriptLineParser.Parse(bytes.AsMemory(start, length)) is { } record)
            {
                _records.Add(record);
                Version++;
            }
            start = index + 1;
        }
        return start;
    }
}
