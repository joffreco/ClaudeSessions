using System;
using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>Telling which tabs wait for the user without being watched.</summary>
public sealed class AttentionTrackerTests
{
    private static readonly SessionTitle Working = SessionTitle.Parse("◐ Fix the hook");
    private static readonly SessionTitle Idle = SessionTitle.Parse("✳ Fix the hook");
    private static readonly SessionTitle Plain = SessionTitle.Parse("Fix the hook");

    private readonly AttentionTracker _tracker = new();
    private readonly TabId _tab = new(Guid.NewGuid());

    /// <summary>A session that stops working in a hidden tab waits for the user.</summary>
    [Fact]
    public void A_hidden_tab_that_finishes_waits()
    {
        _tracker.Observe(_tab, Working, watched: false);

        bool started = _tracker.Observe(_tab, Idle, watched: false);

        Assert.True(started);
        Assert.True(_tracker.IsWaiting(_tab));
    }

    /// <summary>A session that finishes under the user's eyes needs no signal.</summary>
    [Fact]
    public void A_watched_tab_that_finishes_does_not_wait()
    {
        _tracker.Observe(_tab, Working, watched: true);

        bool started = _tracker.Observe(_tab, Idle, watched: true);

        Assert.False(started);
        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>A session that starts idle, as a resumed one does, has finished nothing.</summary>
    [Fact]
    public void An_idle_mark_without_work_before_it_does_not_wait()
    {
        bool started = _tracker.Observe(_tab, Idle, watched: false);

        Assert.False(started);
        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>Several idle titles in a row signal the tab once.</summary>
    [Fact]
    public void Signals_a_finished_session_once()
    {
        _tracker.Observe(_tab, Working, watched: false);
        _tracker.Observe(_tab, Idle, watched: false);

        bool again = _tracker.Observe(_tab, Idle, watched: false);

        Assert.False(again);
        Assert.True(_tracker.IsWaiting(_tab));
    }

    /// <summary>Looking at the tab ends the wait.</summary>
    [Fact]
    public void Seeing_the_tab_ends_the_wait()
    {
        _tracker.Observe(_tab, Working, watched: false);
        _tracker.Observe(_tab, Idle, watched: false);

        bool wasWaiting = _tracker.Seen(_tab);

        Assert.True(wasWaiting);
        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>A session that works again no longer waits.</summary>
    [Fact]
    public void Working_again_ends_the_wait()
    {
        _tracker.Observe(_tab, Working, watched: false);
        _tracker.Observe(_tab, Idle, watched: false);

        _tracker.Observe(_tab, Working, watched: false);

        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>A title without a mark says nothing about the session's state.</summary>
    [Fact]
    public void A_title_without_a_mark_changes_nothing()
    {
        _tracker.Observe(_tab, Working, watched: false);
        _tracker.Observe(_tab, Plain, watched: false);

        bool started = _tracker.Observe(_tab, Idle, watched: false);

        Assert.True(started);
    }

    /// <summary>A question asked in a hidden tab makes it wait for the user.</summary>
    [Fact]
    public void A_hidden_tab_that_asks_for_an_approval_waits()
    {
        bool started = _tracker.RequestApproval(_tab, watched: false);

        Assert.True(started);
        Assert.True(_tracker.AsksApproval(_tab));
        Assert.True(_tracker.IsWaiting(_tab));
    }

    /// <summary>A question asked under the user's eyes needs no signal.</summary>
    [Fact]
    public void A_watched_tab_that_asks_for_an_approval_does_not_wait()
    {
        bool started = _tracker.RequestApproval(_tab, watched: true);

        Assert.False(started);
        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>Only the user can answer, so a spinner does not end the wait for an approval.</summary>
    [Fact]
    public void Working_marks_do_not_end_the_wait_for_an_approval()
    {
        _tracker.RequestApproval(_tab, watched: false);

        _tracker.Observe(_tab, Working, watched: false);

        Assert.True(_tracker.AsksApproval(_tab));
    }

    /// <summary>Looking at the tab ends the wait for an approval.</summary>
    [Fact]
    public void Seeing_the_tab_ends_the_wait_for_an_approval()
    {
        _tracker.RequestApproval(_tab, watched: false);

        bool wasWaiting = _tracker.Seen(_tab);

        Assert.True(wasWaiting);
        Assert.False(_tracker.AsksApproval(_tab));
        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>A finished session that also asked for an approval stops waiting at once when seen.</summary>
    [Fact]
    public void Seeing_the_tab_ends_both_waits()
    {
        _tracker.Observe(_tab, Working, watched: false);
        _tracker.Observe(_tab, Idle, watched: false);
        _tracker.RequestApproval(_tab, watched: false);

        _tracker.Seen(_tab);

        Assert.False(_tracker.IsWaiting(_tab));
    }

    /// <summary>A second request while the first is unanswered signals nothing new.</summary>
    [Fact]
    public void Signals_an_approval_once()
    {
        _tracker.RequestApproval(_tab, watched: false);

        Assert.False(_tracker.RequestApproval(_tab, watched: false));
    }

    /// <summary>A forgotten tab no longer asks for anything.</summary>
    [Fact]
    public void Forgetting_a_tab_clears_its_approval()
    {
        _tracker.RequestApproval(_tab, watched: false);

        _tracker.Forget(_tab);

        Assert.False(_tracker.AsksApproval(_tab));
    }

    /// <summary>A forgotten tab starts over, as after a relaunch.</summary>
    [Fact]
    public void Forgetting_a_tab_clears_its_state()
    {
        _tracker.Observe(_tab, Working, watched: false);
        _tracker.Observe(_tab, Idle, watched: false);

        _tracker.Forget(_tab);

        Assert.False(_tracker.IsWaiting(_tab));
        Assert.False(_tracker.Observe(_tab, Idle, watched: false));
    }
}
