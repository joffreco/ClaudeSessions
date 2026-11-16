using System;
using System.Threading.Tasks;
using ClaudeSessions.Adapters.Transcripts;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>Reading the status lines the wrapper keeps for each session.</summary>
public sealed class StatusLineFilesTests
{
    private static readonly SessionId Session = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));

    /// <summary>The segment that is the session name, read from the kept input, is left out.</summary>
    [Fact]
    public async Task Leaves_out_the_session_name()
    {
        using TempFolder folder = new();
        folder.Write($"{Session}.txt", "\u001b[38;5;220mOpus 5.5\u001b[39m \u001b[2m|\u001b[22m \u001b[33mclaude sessions\u001b[39m");
        folder.Write($"{Session}.json", """{"session_id":"x","session_name":"claude sessions"}""");

        StatusLine? line = await new StatusLineFiles(folder.Path).ReadAsync(Session);

        StatusSegment segment = Assert.Single(Assert.Single(line!.Rows).Segments);
        Assert.Equal("Opus 5.5", Assert.IsType<StatusText>(Assert.Single(segment.Parts)).Text);
    }

    /// <summary>Without the kept input, or with an unreadable one, every segment stays.</summary>
    [Fact]
    public async Task Keeps_every_segment_without_the_input()
    {
        using TempFolder folder = new();
        folder.Write($"{Session}.txt", "Opus 5.5 | claude sessions");
        Assert.Equal(2, (await new StatusLineFiles(folder.Path).ReadAsync(Session))!.Rows[0].Segments.Count);

        folder.Write($"{Session}.json", "{not json");
        Assert.Equal(2, (await new StatusLineFiles(folder.Path).ReadAsync(Session))!.Rows[0].Segments.Count);
    }

    /// <summary>No file, an empty one or a folder out of reach means no status line.</summary>
    [Fact]
    public async Task Reads_nothing_without_a_status_line()
    {
        using TempFolder folder = new();
        Assert.Null(await new StatusLineFiles(folder.Path).ReadAsync(Session));

        folder.Write($"{Session}.txt", "\u001b[0m  \n");
        Assert.Null(await new StatusLineFiles(folder.Path).ReadAsync(Session));
        Assert.Null(await new StatusLineFiles(folder.File("absent")).ReadAsync(Session));
    }
}
