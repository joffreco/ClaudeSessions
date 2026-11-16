using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Reads the transcripts Claude Code writes under ~/.claude/projects/&lt;project&gt;/&lt;session&gt;.jsonl.
/// The JSONL format is Claude Code's own and undocumented; everything that depends on it lives
/// in this folder: <see cref="TranscriptLineParser"/> for the syntax of a line,
/// <see cref="ActiveBranch"/> for how lines chain, <see cref="ConversationMapper"/> for what
/// messages mean. Each transcript is read incrementally and kept in memory; for the recent entries,
/// only the end of the file is read and kept. A transcript that did not grow is not mapped again.
/// </summary>
public sealed class JsonlTranscriptSource : ITranscriptSource
{
    /// <summary>How much of the end of a transcript the recent entries are read from.</summary>
    public const long RecentBytes = 1024 * 1024;

    /// <summary>Most lines the recent entries are read from.</summary>
    public const int RecentRecords = 2_000;

    private const string Extension = ".jsonl";

    private readonly string _projectsDirectory;
    private readonly Dictionary<SessionId, Cached> _full = [];
    private readonly Dictionary<SessionId, Cached> _recent = [];
    private readonly object _gate = new();

    /// <summary>Creates the source.</summary>
    /// <param name="projectsDirectory">The ~/.claude/projects folder, as Windows sees it.</param>
    public JsonlTranscriptSource(string projectsDirectory)
    {
        _projectsDirectory = projectsDirectory;
    }

    /// <inheritdoc />
    public Task<Conversation?> ReadAsync(SessionId session)
    {
        return Task.Run(() => Read(session, _full, path => new TranscriptLog(path), FullConversation));
    }

    /// <inheritdoc />
    public Task<Conversation?> ReadRecentAsync(SessionId session)
    {
        return Task.Run(() => Read(session, _recent, path => new TranscriptLog(path, RecentBytes, RecentRecords), RecentConversation));
    }

    private static Conversation FullConversation(SessionId session, TranscriptLog log)
    {
        return new Conversation(session, ConversationMapper.Map(ActiveBranch.Resolve(log.Records), log.Records), ConversationMapper.Skills(log.Records), log.LastWrite);
    }

    // The end of a file holds no whole chain: its lines are taken in file order, without the subagents'.
    private static Conversation RecentConversation(SessionId session, TranscriptLog log)
    {
        List<Dto.TranscriptRecord> main = [.. log.Records.Where(record => !record.IsSidechain)];
        return new Conversation(session, ConversationMapper.Map(main, log.Records), [], log.LastWrite);
    }

    private Conversation? Read(SessionId session, Dictionary<SessionId, Cached> cache, Func<string, TranscriptLog> open, Func<SessionId, TranscriptLog, Conversation> map)
    {
        lock (_gate)
        {
            try
            {
                Cached? cached = Log(session, cache, open);
                if (cached is null)
                {
                    return null;
                }
                if (cached.Conversation is null || cached.Version != cached.Log.Version)
                {
                    cached.Conversation = map(session, cached.Log);
                    cached.Version = cached.Log.Version;
                }
                else if (cached.Conversation.LastWrite != cached.Log.LastWrite)
                {
                    cached.Conversation = cached.Conversation with { LastWrite = cached.Log.LastWrite };
                }
                return cached.Conversation;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }

    private Cached? Log(SessionId session, Dictionary<SessionId, Cached> cache, Func<string, TranscriptLog> open)
    {
        if (cache.TryGetValue(session, out Cached? known))
        {
            if (known.Log.Refresh())
            {
                return known;
            }
            // The file moved or was deleted: look for it again.
            cache.Remove(session);
        }
        string? path = Find(session);
        if (path is null)
        {
            return null;
        }
        TranscriptLog log = open(path);
        if (!log.Refresh())
        {
            return null;
        }
        Cached added = new(log);
        cache[session] = added;
        return added;
    }

    // The project folder is the session's working directory with every / replaced, so the
    // session id alone does not say which folder holds it.
    private string? Find(SessionId session)
    {
        if (!Directory.Exists(_projectsDirectory))
        {
            return null;
        }
        string name = session + Extension;
        foreach (string project in Directory.EnumerateDirectories(_projectsDirectory))
        {
            string candidate = Path.Combine(project, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    private sealed class Cached(TranscriptLog log)
    {
        public TranscriptLog Log { get; } = log;

        public long Version { get; set; } = -1;

        public Conversation? Conversation { get; set; }
    }
}
