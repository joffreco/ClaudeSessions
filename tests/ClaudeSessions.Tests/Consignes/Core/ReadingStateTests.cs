using System.Collections.Generic;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Consignes.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Core;

/// <summary>What the user did with the consignes.</summary>
public sealed class ReadingStateTests
{
    /// <summary>Ticking and unticking a task; an untouched consigne keeps no entry.</summary>
    [Fact]
    public void TicksTasks()
    {
        ReadingState state = new();
        ConsigneKey key = new("t", "a");

        state.SetTicked(key, 1, true);
        Assert.True(state.IsTicked(key, 1));
        state.SetTicked(key, 1, false);

        Assert.False(state.IsTicked(key, 1));
        Assert.Empty(state.Ticked);
    }

    /// <summary>Marking a consigne seen reports a change only the first time.</summary>
    [Fact]
    public void MarkSeenOnce()
    {
        ReadingState state = new();
        Consigne consigne = Make.Consigne("t", "a");

        Assert.True(state.MarkSeen(consigne));
        Assert.False(state.MarkSeen(consigne));
    }

    /// <summary>Consignes that disappeared are forgotten.</summary>
    [Fact]
    public void RetainForgetsGone()
    {
        ReadingState state = new();
        state.MarkSeen(Make.Consigne("t", "kept"));
        state.MarkSeen(Make.Consigne("t", "gone"));
        state.SetTicked(new ConsigneKey("t", "gone"), 0, true);

        Assert.True(state.Retain(new HashSet<string> { "t/kept" }));
        Assert.Equal(["t/kept"], state.Seen.Keys);
        Assert.Empty(state.Ticked);
    }
}
