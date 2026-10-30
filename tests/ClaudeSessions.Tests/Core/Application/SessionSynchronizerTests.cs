using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ClaudeSessions.Tests.Core.Application;

/// <summary>Reading the recorded sessions of several tabs within a time limit.</summary>
public sealed class SessionSynchronizerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>Every tab answered in time comes back with its session.</summary>
    [Fact]
    public async Task Returns_the_sessions_read()
    {
        FakeTabSessionSource source = new();
        TabId tab = NewTab();
        SessionId session = new(Guid.NewGuid());
        source.Answer(tab, session);
        SessionSynchronizer synchronizer = new(source, new FakeTimeProvider(), Timeout);

        IReadOnlyDictionary<TabId, SessionId> read = await synchronizer.ReadAsync([tab]);

        Assert.Equal(session, read[tab]);
    }

    /// <summary>A tab without a record is left out.</summary>
    [Fact]
    public async Task Leaves_out_a_tab_without_record()
    {
        FakeTabSessionSource source = new();
        TabId tab = NewTab();
        source.Answer(tab, null);
        SessionSynchronizer synchronizer = new(source, new FakeTimeProvider(), Timeout);

        IReadOnlyDictionary<TabId, SessionId> read = await synchronizer.ReadAsync([tab]);

        Assert.Empty(read);
    }

    /// <summary>A failed read is left out without failing the others.</summary>
    [Fact]
    public async Task Leaves_out_a_failed_read()
    {
        FakeTabSessionSource source = new();
        TabId failing = NewTab();
        TabId working = NewTab();
        source.Fail(failing);
        source.Answer(working, new SessionId(Guid.NewGuid()));
        SessionSynchronizer synchronizer = new(source, new FakeTimeProvider(), Timeout);

        IReadOnlyDictionary<TabId, SessionId> read = await synchronizer.ReadAsync([failing, working]);

        Assert.Equal([working], read.Keys);
    }

    /// <summary>A read that hangs is given up once the time limit passes; the others still count.</summary>
    [Fact]
    public async Task Gives_up_a_hanging_read_after_the_timeout()
    {
        FakeTabSessionSource source = new();
        FakeTimeProvider clock = new();
        TabId hanging = NewTab();
        TabId working = NewTab();
        source.Hang(hanging);
        source.Answer(working, new SessionId(Guid.NewGuid()));
        SessionSynchronizer synchronizer = new(source, clock, Timeout);

        Task<IReadOnlyDictionary<TabId, SessionId>> reading = synchronizer.ReadAsync([hanging, working]);
        clock.Advance(Timeout);
        IReadOnlyDictionary<TabId, SessionId> read = await reading;

        Assert.Equal([working], read.Keys);
    }

    /// <summary>The wait does not end before the time limit while a read hangs.</summary>
    [Fact]
    public void Keeps_waiting_until_the_timeout()
    {
        FakeTabSessionSource source = new();
        FakeTimeProvider clock = new();
        TabId hanging = NewTab();
        source.Hang(hanging);
        SessionSynchronizer synchronizer = new(source, clock, Timeout);

        Task<IReadOnlyDictionary<TabId, SessionId>> reading = synchronizer.ReadAsync([hanging]);
        clock.Advance(Timeout - TimeSpan.FromMilliseconds(1));

        Assert.False(reading.IsCompleted);
    }

    /// <summary>A read still pending from an earlier round is not started again.</summary>
    [Fact]
    public async Task Does_not_start_a_second_read_while_one_is_pending()
    {
        FakeTabSessionSource source = new();
        FakeTimeProvider clock = new();
        TabId hanging = NewTab();
        source.Hang(hanging);
        SessionSynchronizer synchronizer = new(source, clock, Timeout);
        Task<IReadOnlyDictionary<TabId, SessionId>> first = synchronizer.ReadAsync([hanging]);
        clock.Advance(Timeout);
        await first;

        Task<IReadOnlyDictionary<TabId, SessionId>> second = synchronizer.ReadAsync([hanging]);
        clock.Advance(Timeout);
        await second;

        Assert.Equal(1, source.ReadCounts[hanging]);
    }

    /// <summary>Once a late read has completed, the next round starts a fresh read instead of reusing the stale one.</summary>
    [Fact]
    public async Task Starts_a_fresh_read_once_the_late_one_completed()
    {
        FakeTabSessionSource source = new();
        FakeTimeProvider clock = new();
        TabId tab = NewTab();
        SessionId session = new(Guid.NewGuid());
        TaskCompletionSource<SessionId?> late = source.Hang(tab);
        SessionSynchronizer synchronizer = new(source, clock, Timeout);
        Task<IReadOnlyDictionary<TabId, SessionId>> first = synchronizer.ReadAsync([tab]);
        clock.Advance(Timeout);
        await first;
        late.SetResult(session);

        IReadOnlyDictionary<TabId, SessionId> second = await synchronizer.ReadAsync([tab]);

        Assert.Equal(session, second[tab]);
        Assert.Equal(2, source.ReadCounts[tab]);
    }

    /// <summary>No tab means no read and an empty answer.</summary>
    [Fact]
    public async Task Returns_nothing_for_no_tab()
    {
        FakeTabSessionSource source = new();
        SessionSynchronizer synchronizer = new(source, new FakeTimeProvider(), Timeout);

        IReadOnlyDictionary<TabId, SessionId> read = await synchronizer.ReadAsync([]);

        Assert.Empty(read);
        Assert.Empty(source.ReadCounts);
    }

    /// <summary>A tab listed twice is read once.</summary>
    [Fact]
    public async Task Reads_a_duplicated_tab_once()
    {
        FakeTabSessionSource source = new();
        TabId tab = NewTab();
        source.Answer(tab, new SessionId(Guid.NewGuid()));
        SessionSynchronizer synchronizer = new(source, new FakeTimeProvider(), Timeout);

        await synchronizer.ReadAsync([tab, tab]);

        Assert.Equal(1, source.ReadCounts[tab]);
    }

    // category N/A: malicious — record contents are validated by HookFileTabSessionSource, tested with the adapters.

    /// <summary>Builds a random tab id.</summary>
    /// <returns>The id.</returns>
    private static TabId NewTab()
    {
        return new TabId(Guid.NewGuid());
    }
}
