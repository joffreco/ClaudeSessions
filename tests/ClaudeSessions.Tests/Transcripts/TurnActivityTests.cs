using System;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Application;
using ClaudeSessions.Core.Transcripts.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>Telling from a conversation whether Claude is working.</summary>
public sealed class TurnActivityTests
{
    private static readonly SessionId Session = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
    private static readonly DateTimeOffset Sent = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    /// <summary>After a prompt, the turn runs: its start, the tokens of its replies and the tool awaited are given.</summary>
    [Fact]
    public void A_turn_runs_from_the_prompt()
    {
        Conversation conversation = Of(
            new UserPrompt(Sent, "Liste"),
            new AssistantMessage(Sent.AddSeconds(2), [new ToolCall("t1", "Read", "{}", new ToolResult("ok", false))], 120),
            new AssistantMessage(Sent.AddSeconds(5), [new AssistantText("Je lance"), new ToolCall("t2", "Bash", "{}", null)], 380));

        TurnProgress? progress = TurnActivity.Of(conversation, Sent.AddSeconds(33));

        Assert.Equal(new TurnProgress(Sent, 500, "Bash"), progress);
    }

    /// <summary>The end Claude Code marks, an interruption or a reply that ends the turn stops it.</summary>
    [Fact]
    public void A_turn_stops_at_its_end()
    {
        UserPrompt prompt = new(Sent, "Liste");
        AssistantMessage answer = new(Sent.AddSeconds(4), [new AssistantText("Fait")], 50);

        Assert.Null(TurnActivity.Of(Of(prompt, answer, new TurnEnd(Sent.AddSeconds(5), TimeSpan.FromSeconds(5), false)), Sent.AddSeconds(6)));
        Assert.Null(TurnActivity.Of(Of(prompt, new TurnEnd(Sent.AddSeconds(1), null, true)), Sent.AddSeconds(6)));
        Assert.Null(TurnActivity.Of(Of(prompt, answer with { EndsTurn = true }), Sent.AddSeconds(6)));
        Assert.NotNull(TurnActivity.Of(Of(prompt, answer), Sent.AddSeconds(6)));
    }

    /// <summary>Without a prompt, or long silent with no tool awaited, nothing runs; a tool awaited keeps the turn going.</summary>
    [Fact]
    public void Nothing_runs_without_a_prompt_or_after_a_long_silence()
    {
        Assert.Null(TurnActivity.Of(null, Sent));
        Assert.Null(TurnActivity.Of(Of(new AssistantMessage(Sent, [new AssistantText("Bonjour")])), Sent));

        DateTimeOffset later = Sent + TurnActivity.StaleAfter + TimeSpan.FromSeconds(1);
        Conversation silent = Of(new UserPrompt(Sent, "Liste")) with { LastWrite = Sent };
        Assert.Null(TurnActivity.Of(silent, later));
        Conversation waiting = Of(new UserPrompt(Sent, "Liste"), new AssistantMessage(Sent, [new ToolCall("t1", "Bash", "{}", null)])) with { LastWrite = Sent };
        Assert.Equal("Bash", TurnActivity.Of(waiting, later)?.RunningTool);
    }

    private static Conversation Of(params ConversationEntry[] entries)
    {
        return new Conversation(Session, entries, []);
    }
}
