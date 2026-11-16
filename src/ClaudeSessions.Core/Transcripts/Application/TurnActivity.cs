using System;
using System.Linq;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Core.Transcripts.Application;

/// <summary>
/// Tells from a conversation whether Claude is working: a turn runs from the last prompt until Claude Code
/// marks its end, the model stops because it is done, or the user interrupts it. A session whose process died
/// mid-turn never marks the end, so a turn with no write for <see cref="StaleAfter"/> and no tool awaited is
/// taken as over.
/// </summary>
public static class TurnActivity
{
    /// <summary>How long a turn may go without a write to the transcript, no tool awaited, before it is taken as over.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    /// <summary>Reads the turn under way.</summary>
    /// <param name="conversation">The conversation, or its last entries.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The turn under way; null when Claude is not working.</returns>
    public static TurnProgress? Of(Conversation? conversation, DateTimeOffset now)
    {
        if (conversation is null || conversation.Entries.Count == 0)
        {
            return null;
        }
        int start = -1;
        for (int index = conversation.Entries.Count - 1; index >= 0; index--)
        {
            ConversationEntry entry = conversation.Entries[index];
            if (entry is TurnEnd)
            {
                return null;
            }
            if (entry is UserPrompt)
            {
                start = index;
                break;
            }
        }
        if (start < 0 || conversation.Entries[^1] is AssistantMessage { EndsTurn: true })
        {
            return null;
        }
        AssistantMessage[] replies = [.. conversation.Entries.Skip(start + 1).OfType<AssistantMessage>()];
        string? running = replies.LastOrDefault()?.Blocks.OfType<ToolCall>().LastOrDefault(call => call.Result is null)?.Name;
        if (running is null && conversation.LastWrite is DateTimeOffset written && now - written > StaleAfter)
        {
            return null;
        }
        return new TurnProgress(conversation.Entries[start].Timestamp, replies.Sum(reply => reply.OutputTokens), running);
    }
}
