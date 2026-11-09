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

    /// <summary>A result sent again for the same field replaces the previous one.</summary>
    [Fact]
    public void ResentResultReplacesThePreviousOne()
    {
        ReadingState state = new();
        ConsigneKey key = new("tab", "one");
        state.SetResult(key, "comptes", "1");

        state.SetResult(key, "comptes", "2");

        Assert.Equal("2", state.ResultOf(key, "comptes"));
    }

    /// <summary>A field with no sent result reads as empty.</summary>
    [Fact]
    public void UnsentFieldReadsEmpty()
    {
        ReadingState state = new();
        ConsigneKey key = new("tab", "one");
        state.SetResult(key, "comptes", "1");

        string result = state.ResultOf(key, "autre");

        Assert.Equal("", result);
    }

    /// <summary>A result belongs to its consigne: the same field of another consigne reads as empty.</summary>
    [Fact]
    public void ResultIsKeptPerConsigne()
    {
        ReadingState state = new();
        state.SetResult(new ConsigneKey("tab", "one"), "comptes", "1");

        string result = state.ResultOf(new ConsigneKey("tab", "two"), "comptes");

        Assert.Equal("", result);
    }

    /// <summary>A result is forgotten with its consigne.</summary>
    [Fact]
    public void ResultIsForgottenWithItsConsigne()
    {
        ReadingState state = new();
        ConsigneKey key = new("tab", "one");
        state.SetResult(key, "comptes", "1");

        bool changed = state.Retain(new HashSet<string>());

        Assert.True(changed);
        Assert.Equal("", state.ResultOf(key, "comptes"));
    }
}
