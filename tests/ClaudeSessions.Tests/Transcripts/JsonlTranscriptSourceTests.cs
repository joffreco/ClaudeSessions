using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClaudeSessions.Adapters.Transcripts;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>Reading Claude Code transcripts into conversations, with the shapes seen in real transcripts.</summary>
public sealed class JsonlTranscriptSourceTests
{
    private const string Time = "2026-11-12T10:00:00.000Z";
    private static readonly SessionId Session = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
    private static readonly string[] InitialNames = ["eco", "old"];
    private static readonly string[] SecondNames = ["eco", "claude-api"];
    private static readonly string[] AddedNames = ["ecole-publish"];

    /// <summary>A prompt and its reply.</summary>
    [Fact]
    public async Task Reads_a_prompt_and_its_reply()
    {
        using TempFolder folder = new();
        Write(folder, Prompt("p1", null, "Bonjour"), Reply("a1", "p1", "m1", Text("Salut")));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Equal("Bonjour", Assert.IsType<UserPrompt>(entries[0]).Text);
        AssistantMessage reply = Assert.IsType<AssistantMessage>(entries[1]);
        Assert.Equal("Salut", Assert.IsType<AssistantText>(Assert.Single(reply.Blocks)).Text);
        Assert.Equal(2, entries.Count);
    }

    /// <summary>One reply spans one line per block; parallel results hang off their own call and are matched by id.</summary>
    [Fact]
    public async Task Groups_a_reply_and_matches_parallel_tool_results()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "Liste"),
            Reply("a1", "p1", "m1", Thinking("Je regarde")),
            Reply("a2", "a1", "m1", Use("t1", "Bash")),
            Reply("a3", "a2", "m1", Use("t2", "Read")),
            Result("r1", "a2", "t1", "fichier.txt"),
            Result("r2", "a3", "t2", "contenu", error: true),
            Reply("a4", "r2", "m2", Text("Fait")));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        AssistantMessage first = Assert.IsType<AssistantMessage>(entries[1]);
        Assert.Equal("Je regarde", Assert.IsType<AssistantThinking>(first.Blocks[0]).Text);
        ToolCall bash = Assert.IsType<ToolCall>(first.Blocks[1]);
        Assert.Equal(("Bash", "fichier.txt", false), (bash.Name, bash.Result!.Text, bash.Result.IsError));
        ToolCall read = Assert.IsType<ToolCall>(first.Blocks[2]);
        Assert.Equal(("Read", true), (read.Name, read.Result!.IsError));
        Assert.Equal("Fait", Assert.IsType<AssistantText>(Assert.IsType<AssistantMessage>(entries[2]).Blocks[0]).Text);
        Assert.Equal(3, entries.Count);
    }

    /// <summary>A call whose result has not come back yet has no result.</summary>
    [Fact]
    public async Task Leaves_a_running_call_without_result()
    {
        using TempFolder folder = new();
        Write(folder, Prompt("p1", null, "Teste"), Reply("a1", "p1", "m1", Use("t1", "Bash")));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Null(Assert.IsType<ToolCall>(Assert.IsType<AssistantMessage>(entries[1]).Blocks[0]).Result);
    }

    /// <summary>After /rewind and a new prompt, the abandoned replies stay in the file but leave the conversation.</summary>
    [Fact]
    public async Task Follows_the_branch_of_the_last_prompt_after_a_rewind()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "Un"),
            Reply("a1", "p1", "m1", Text("Réponse un")),
            Prompt("p2", "a1", "Deux"),
            Reply("a2", "p2", "m2", Text("Réponse deux")),
            Prompt("p3", "a1", "Deux, autrement"),
            Reply("a3", "p3", "m3", Text("Réponse trois")));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Equal(["Un", "Réponse un", "Deux, autrement", "Réponse trois"], entries.Select(Describe));
    }

    /// <summary>A compaction boundary has no parent; the history before it stays, marked.</summary>
    [Fact]
    public async Task Keeps_the_history_before_a_compaction()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "Avant"),
            Reply("a1", "p1", "m1", Text("Réponse avant")),
            Boundary("b1", "a1"),
            Line(new { type = "user", uuid = "s1", parentUuid = "b1", isSidechain = false, isCompactSummary = true, timestamp = Time, message = new { role = "user", content = "Résumé" } }),
            Prompt("p2", "s1", "Après"));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Equal(["Avant", "Réponse avant", "[compaction]", "Après"], entries.Select(Describe));
    }

    /// <summary>Lines of other types, such as attachments, link the chain without showing.</summary>
    [Fact]
    public async Task Follows_the_chain_through_lines_that_do_not_show()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "Un"),
            Line(new { type = "attachment", uuid = "x1", parentUuid = "p1", isSidechain = false, timestamp = Time, attachment = new { type = "file" } }),
            Reply("a1", "x1", "m1", Text("Réponse")));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Equal(["Un", "Réponse"], entries.Select(Describe));
    }

    /// <summary>Subagent lines, messages meant for the model, unknown lines and broken lines stay out.</summary>
    [Fact]
    public async Task Leaves_out_what_the_user_did_not_see()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "Vu"),
            Line(new { type = "mode", mode = "normal", sessionId = "x" }),
            Prompt("p2", "p1", "Rappel interne", meta: true),
            Reply("x1", "p2", "mx", Text("Sous-agent"), sidechain: true),
            "{\"type\":\"user\",\"uuid\":",
            Reply("a1", "p2", "m1", Text("Réponse")));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Equal(["Vu", "Réponse"], entries.Select(Describe));
    }

    /// <summary>Slash commands, ! commands and their output come as tagged user lines.</summary>
    [Fact]
    public async Task Recognises_commands_and_their_output()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "<command-name>/model</command-name>\n<command-message>model</command-message>\n<command-args>sonnet</command-args>"),
            Prompt("p2", "p1", "<local-command-stdout>Modèle changé</local-command-stdout>"),
            Prompt("p3", "p2", "<bash-input>ls</bash-input>"),
            Prompt("p4", "p3", "<bash-stdout>a.txt</bash-stdout><bash-stderr></bash-stderr>"),
            Prompt("p5", "p4", "<task-notification>Tâche terminée</task-notification>"));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        SlashCommand command = Assert.IsType<SlashCommand>(entries[0]);
        Assert.Equal(("/model", "sonnet"), (command.Name, command.Arguments));
        Assert.Equal("Modèle changé", Assert.IsType<CommandOutput>(entries[1]).Text);
        Assert.Equal("ls", Assert.IsType<ShellCommand>(entries[2]).Command);
        Assert.Equal("a.txt", Assert.IsType<CommandOutput>(entries[3]).Text);
        Assert.Equal("Tâche terminée", Assert.IsType<Notice>(entries[4]).Text);
    }

    /// <summary>Command output and tool results lose the terminal's color codes, as /compact prints them.</summary>
    [Fact]
    public async Task Removes_terminal_color_codes_from_output()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "<local-command-stdout>\u001b[2mCompacted (ctrl+o to see full summary)\u001b[22m</local-command-stdout>"),
            Reply("a1", "p1", "m1", Use("t1", "Bash")),
            Result("r1", "a1", "t1", "\u001b[31mrouge\u001b[0m normal"));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        Assert.Equal("Compacted (ctrl+o to see full summary)", Assert.IsType<CommandOutput>(entries[0]).Text);
        Assert.Equal("rouge normal", Assert.IsType<ToolCall>(Assert.IsType<AssistantMessage>(entries[1]).Blocks[0]).Result!.Text);
    }

    /// <summary>The skills come from the listings Claude Code attaches: a full one, then the new ones.</summary>
    [Fact]
    public async Task Reads_the_skills_listed_to_the_model()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Line(new { type = "attachment", uuid = "x1", parentUuid = (string?)null, isSidechain = false, timestamp = Time, attachment = new { type = "skill_listing", isInitial = true, names = InitialNames, content = "- eco: Map of the repos.\n- old: Gone soon." } }),
            Line(new { type = "attachment", uuid = "x2", parentUuid = "x1", isSidechain = false, timestamp = Time, attachment = new { type = "skill_listing", isInitial = true, names = SecondNames, content = "- eco: Map of the repos.\n- claude-api: Reference.\nTRIGGER — read first." } }),
            Line(new { type = "attachment", uuid = "x3", parentUuid = "x2", isSidechain = false, timestamp = Time, attachment = new { type = "skill_listing", isInitial = false, names = AddedNames, content = "- ecole-publish: Add a page." } }),
            Prompt("p1", "x3", "Bonjour"));

        Conversation? conversation = await new JsonlTranscriptSource(folder.Path).ReadAsync(Session);

        Assert.NotNull(conversation);
        Assert.Equal(
            [new SkillInfo("claude-api", "Reference.\nTRIGGER — read first."), new SkillInfo("eco", "Map of the repos."), new SkillInfo("ecole-publish", "Add a page.")],
            conversation.Skills);
        Assert.Equal(["Bonjour"], conversation.Entries.Select(Describe));
    }

    /// <summary>Each read takes only what was added; a line still being written waits for the next read.</summary>
    [Fact]
    public async Task Reads_only_complete_lines_added_since_the_last_read()
    {
        using TempFolder folder = new();
        string file = Write(folder, Prompt("p1", null, "Un"));
        JsonlTranscriptSource source = new(folder.Path);
        Assert.Single((await source.ReadAsync(Session))!.Entries);

        string next = Reply("a1", "p1", "m1", Text("Réponse"));
        File.AppendAllText(file, next[..10]);
        Assert.Single((await source.ReadAsync(Session))!.Entries);

        File.AppendAllText(file, next[10..] + "\n");
        Assert.Equal(2, (await source.ReadAsync(Session))!.Entries.Count);
    }

    /// <summary>A file shorter than what was read was replaced: it is read again from the start.</summary>
    [Fact]
    public async Task Starts_over_when_the_file_was_replaced()
    {
        using TempFolder folder = new();
        string file = Write(folder, Prompt("p1", null, "Un long message de départ"), Reply("a1", "p1", "m1", Text("Réponse")));
        JsonlTranscriptSource source = new(folder.Path);
        Assert.Equal(2, (await source.ReadAsync(Session))!.Entries.Count);

        File.WriteAllText(file, Prompt("q1", null, "Neuf") + "\n");

        Assert.Equal(["Neuf"], (await source.ReadAsync(Session))!.Entries.Select(Describe));
    }

    /// <summary>The file change of an Edit comes with its result, and a Write that creates a file shows its content as added lines.</summary>
    [Fact]
    public async Task Reads_the_file_change_of_edit_and_write()
    {
        using TempFolder folder = new();
        object[] edit = [new { type = "tool_result", tool_use_id = "t1", content = "updated" }];
        object[] write = [new { type = "tool_result", tool_use_id = "t2", content = "created" }];
        object[] hunks = [new { oldStart = 56, oldLines = 1, newStart = 56, newLines = 2, lines = new[] { " a", "-b", "+c", "+d" } }];
        Write(
            folder,
            Prompt("p1", null, "Change"),
            Reply("a1", "p1", "m1", Use("t1", "Edit")),
            Reply("a2", "a1", "m1", Use("t2", "Write")),
            Line(new { type = "user", uuid = "r1", parentUuid = "a2", isSidechain = false, timestamp = Time, message = new { role = "user", content = edit }, toolUseResult = new { type = "update", structuredPatch = hunks } }),
            Line(new { type = "user", uuid = "r2", parentUuid = "r1", isSidechain = false, timestamp = Time, message = new { role = "user", content = write }, toolUseResult = new { type = "create", content = "un\r\ndeux\n", structuredPatch = Array.Empty<object>() } }));

        AssistantMessage reply = Assert.IsType<AssistantMessage>((await ReadEntries(folder))[1]);

        DiffHunk change = Assert.Single(Assert.IsType<ToolCall>(reply.Blocks[0]).Result!.Patch!);
        Assert.Equal((56, 56), (change.OldStart, change.NewStart));
        Assert.Equal([" a", "-b", "+c", "+d"], change.Lines);
        DiffHunk created = Assert.Single(Assert.IsType<ToolCall>(reply.Blocks[1]).Result!.Patch!);
        Assert.Equal((0, 1), (created.OldStart, created.NewStart));
        Assert.Equal(["+un", "+deux"], created.Lines);
    }

    /// <summary>A reply carries its tokens and whether it ends the turn; the turn's duration and an interruption end a turn.</summary>
    [Fact]
    public async Task Reads_tokens_and_turn_ends()
    {
        using TempFolder folder = new();
        Write(
            folder,
            Prompt("p1", null, "Liste"),
            Line(new { type = "assistant", uuid = "a1", parentUuid = "p1", isSidechain = false, timestamp = Time, message = new { id = "m1", role = "assistant", stop_reason = "end_turn", usage = new { output_tokens = 2458 }, content = new[] { Text("Fait") } } }),
            Line(new { type = "system", subtype = "turn_duration", uuid = "s1", parentUuid = "a1", isSidechain = false, timestamp = Time, durationMs = 33991 }),
            Prompt("p2", "s1", "Encore"),
            Prompt("i1", "p2", "[Request interrupted by user]"));

        IReadOnlyList<ConversationEntry> entries = await ReadEntries(folder);

        AssistantMessage reply = Assert.IsType<AssistantMessage>(entries[1]);
        Assert.Equal((2458, true), (reply.OutputTokens, reply.EndsTurn));
        Assert.Equal(new TurnEnd(reply.Timestamp, TimeSpan.FromMilliseconds(33991), false), entries[2]);
        Assert.True(Assert.IsType<TurnEnd>(entries[4]).Interrupted);
    }

    /// <summary>The recent entries come from the end of the transcript only, and are mapped again only once it grew.</summary>
    [Fact]
    public async Task Reads_the_recent_entries_from_the_end()
    {
        using TempFolder folder = new();
        string filler = new('x', 400);
        string[] lines = [.. Enumerable.Range(0, 4000).Select(index => Prompt($"p{index}", index == 0 ? null : $"p{index - 1}", $"{index} {filler}"))];
        string path = Write(folder, lines);
        JsonlTranscriptSource source = new(folder.Path);

        Conversation? recent = await source.ReadRecentAsync(Session);

        Assert.InRange(recent!.Entries.Count, 1, JsonlTranscriptSource.RecentRecords);
        Assert.StartsWith("3999 ", Assert.IsType<UserPrompt>(recent.Entries[^1]).Text);
        Assert.Same(recent, await source.ReadRecentAsync(Session));
        File.AppendAllText(path, Prompt("p4000", "p3999", "fin") + "\n");
        Assert.Equal("fin", Assert.IsType<UserPrompt>((await source.ReadRecentAsync(Session))!.Entries[^1]).Text);
    }

    /// <summary>A session without a transcript has no conversation.</summary>
    [Fact]
    public async Task Reads_nothing_without_a_transcript()
    {
        using TempFolder folder = new();

        Assert.Null(await new JsonlTranscriptSource(folder.Path).ReadAsync(Session));
        Assert.Null(await new JsonlTranscriptSource(Path.Combine(folder.Path, "missing")).ReadAsync(Session));
    }

    private static async Task<IReadOnlyList<ConversationEntry>> ReadEntries(TempFolder folder)
    {
        Conversation? conversation = await new JsonlTranscriptSource(folder.Path).ReadAsync(Session);
        Assert.NotNull(conversation);
        return conversation.Entries;
    }

    private static string Describe(ConversationEntry entry)
    {
        return entry switch
        {
            UserPrompt prompt => prompt.Text,
            AssistantMessage reply => string.Join(" ", reply.Blocks.OfType<AssistantText>().Select(t => t.Text)),
            CompactionMarker => "[compaction]",
            _ => entry.GetType().Name,
        };
    }

    private static string Write(TempFolder folder, params string[] lines)
    {
        return folder.Write($"-home-joff-Sources/{Session}.jsonl", string.Join("\n", lines) + "\n");
    }

    private static string Line(object record)
    {
        return JsonSerializer.Serialize(record);
    }

    private static string Prompt(string uuid, string? parent, string text, bool meta = false)
    {
        return Line(new { type = "user", uuid, parentUuid = parent, isSidechain = false, isMeta = meta, timestamp = Time, message = new { role = "user", content = text } });
    }

    private static string Reply(string uuid, string parent, string messageId, object block, bool sidechain = false)
    {
        return Line(new { type = "assistant", uuid, parentUuid = parent, isSidechain = sidechain, timestamp = Time, message = new { id = messageId, role = "assistant", content = new[] { block } } });
    }

    private static string Result(string uuid, string parent, string toolUseId, string text, bool error = false)
    {
        object[] content = [new { type = "tool_result", tool_use_id = toolUseId, content = text, is_error = error }];
        return Line(new { type = "user", uuid, parentUuid = parent, isSidechain = false, timestamp = Time, message = new { role = "user", content } });
    }

    private static string Boundary(string uuid, string logicalParent)
    {
        return Line(new { type = "system", subtype = "compact_boundary", uuid, parentUuid = (string?)null, logicalParentUuid = logicalParent, isSidechain = false, timestamp = Time, content = "Conversation compacted" });
    }

    private static object Text(string text)
    {
        return new { type = "text", text };
    }

    private static object Thinking(string thinking)
    {
        return new { type = "thinking", thinking };
    }

    private static object Use(string id, string name)
    {
        return new { type = "tool_use", id, name, input = new { command = "ls" } };
    }
}
