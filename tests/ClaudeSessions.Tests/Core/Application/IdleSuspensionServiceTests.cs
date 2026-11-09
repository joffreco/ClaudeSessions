using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ClaudeSessions.Tests.Core.Application;

/// <summary>Picking the idle tabs to put to sleep, and keeping the chosen delay.</summary>
// category N/A §13.4 (malicious): candidates and activities come from the app's own views and its own WSL probe; no trust boundary is crossed here (the probe output parser is tested in TabActivityReportTests).
public sealed class IdleSuspensionServiceTests
{
    private static readonly TimeSpan FourHours = TimeSpan.FromHours(4);

    /// <summary>A tab waiting, off screen, untouched and quiet for the whole delay is put to sleep.</summary>
    [Fact]
    public async Task Puts_to_sleep_a_tab_untouched_and_quiet_for_the_delay()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - FourHours - TimeSpan.FromMinutes(1));
        harness.Probe.Answer(tab.TabId, new TabActivity(TestData.Now - TimeSpan.FromHours(5), 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Equal([tab], picked);
    }

    /// <summary>A tab whose session has no transcript yet is idle when the rest says so.</summary>
    [Fact]
    public async Task Puts_to_sleep_a_quiet_tab_without_a_transcript()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        harness.Probe.Answer(tab.TabId, new TabActivity(null, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Equal([tab], picked);
    }

    /// <summary>Without a chosen delay, no tab ever sleeps.</summary>
    [Fact]
    public async Task Never_puts_a_tab_to_sleep_without_a_delay()
    {
        Harness harness = new(hours: null);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromDays(30));
        harness.Probe.Answer(tab.TabId, new TabActivity(null, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>When the activity cannot be read, the tab stays awake.</summary>
    [Fact]
    public async Task Keeps_awake_a_tab_whose_activity_cannot_be_read()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        harness.Probe.Answer(tab.TabId, null);

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>A session still working (no waiting mark) stays awake.</summary>
    [Fact]
    public async Task Keeps_awake_a_working_session()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6), waitsForUser: false);
        harness.Probe.Answer(tab.TabId, new TabActivity(null, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>The tab the user has in front of them stays awake.</summary>
    [Fact]
    public async Task Keeps_awake_the_tab_on_screen()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6), isOnScreen: true);
        harness.Probe.Answer(tab.TabId, new TabActivity(null, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>A tab touched exactly the delay ago is idle.</summary>
    [Fact]
    public async Task Puts_to_sleep_a_tab_touched_exactly_the_delay_ago()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - FourHours);
        harness.Probe.Answer(tab.TabId, new TabActivity(TestData.Now - FourHours, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Equal([tab], picked);
    }

    /// <summary>A tab touched one second short of the delay stays awake.</summary>
    [Fact]
    public async Task Keeps_awake_a_tab_touched_one_second_short_of_the_delay()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - FourHours + TimeSpan.FromSeconds(1));
        harness.Probe.Answer(tab.TabId, new TabActivity(null, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>A transcript written one second short of the delay keeps the tab awake.</summary>
    [Fact]
    public async Task Keeps_awake_a_tab_whose_transcript_moved_within_the_delay()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        harness.Probe.Answer(tab.TabId, new TabActivity(TestData.Now - FourHours + TimeSpan.FromSeconds(1), 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>No running tab gives nothing to put to sleep.</summary>
    [Fact]
    public async Task Picks_nothing_among_no_tabs()
    {
        Harness harness = new(hours: 4);

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([]);

        Assert.Empty(picked);
    }

    /// <summary>
    /// A session showing the waiting mark while a background command or monitor still runs stays awake:
    /// the mark alone does not prove the session is idle.
    /// </summary>
    [Fact]
    public async Task Keeps_awake_a_waiting_session_with_a_background_process()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        harness.Probe.Answer(tab.TabId, new TabActivity(TestData.Now - TimeSpan.FromHours(6), 1));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>
    /// A session showing the waiting mark while a background agent writes its transcript stays awake,
    /// even though the user has not touched the tab for hours.
    /// </summary>
    [Fact]
    public async Task Keeps_awake_a_waiting_session_whose_background_agent_writes()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(10));
        harness.Probe.Answer(tab.TabId, new TabActivity(TestData.Now - TimeSpan.FromMinutes(2), 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>A transcript dated in the future, from a clock gap between WSL and Windows, keeps the tab awake.</summary>
    [Fact]
    public async Task Keeps_awake_a_tab_whose_transcript_is_dated_in_the_future()
    {
        Harness harness = new(hours: 4);
        IdleCandidate tab = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        harness.Probe.Answer(tab.TabId, new TabActivity(TestData.Now + TimeSpan.FromMinutes(5), 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([tab]);

        Assert.Empty(picked);
    }

    /// <summary>Among several tabs, only the idle ones are picked, in their order.</summary>
    [Fact]
    public async Task Picks_only_the_idle_tabs_among_several()
    {
        Harness harness = new(hours: 4);
        IdleCandidate idle = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        IdleCandidate busy = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromHours(6));
        IdleCandidate recent = Harness.Candidate(lastTouched: TestData.Now - TimeSpan.FromMinutes(5));
        harness.Probe.Answer(idle.TabId, new TabActivity(null, 0));
        harness.Probe.Answer(busy.TabId, new TabActivity(null, 2));
        harness.Probe.Answer(recent.TabId, new TabActivity(null, 0));

        IReadOnlyList<IdleCandidate> picked = await harness.Service.SelectAsync([idle, busy, recent]);

        Assert.Equal([idle], picked);
    }

    /// <summary>The saved delay applies from the start.</summary>
    [Fact]
    public void Starts_with_the_saved_delay()
    {
        Harness harness = new(hours: 8);

        Assert.Equal(8, harness.Service.HoursBeforeSleep);
    }

    /// <summary>Choosing a delay saves it beside the other preferences.</summary>
    [Fact]
    public void Saves_the_chosen_delay_beside_the_other_preferences()
    {
        Harness harness = new(hours: null, preferences: new ViewPreferences(14, 3));

        bool changed = harness.Service.SetHoursBeforeSleep(2);

        Assert.True(changed);
        Assert.Equal(new ViewPreferences(14, 3, 2), harness.Store.Saved);
    }

    /// <summary>Choosing no delay turns the sleep off and saves that.</summary>
    [Fact]
    public void Turning_the_sleep_off_saves_no_delay()
    {
        Harness harness = new(hours: 4);

        harness.Service.SetHoursBeforeSleep(null);

        Assert.Null(harness.Service.HoursBeforeSleep);
        Assert.Null(harness.Store.Saved.SuspendAfterHours);
    }

    /// <summary>Choosing the current delay again changes and saves nothing.</summary>
    [Fact]
    public void Choosing_the_same_delay_saves_nothing()
    {
        Harness harness = new(hours: 4);

        bool changed = harness.Service.SetHoursBeforeSleep(4);

        Assert.False(changed);
        Assert.Equal(0, harness.Store.SaveCount);
    }

    /// <summary>A delay out of range, saved or chosen, means the tabs never sleep.</summary>
    /// <param name="hours">The delay.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(IdleSuspensionService.MaxHours + 1)]
    [InlineData(int.MaxValue)]
    public void Treats_a_delay_out_of_range_as_no_delay(int hours)
    {
        Harness saved = new(hours: hours);
        Harness chosen = new(hours: 4);

        chosen.Service.SetHoursBeforeSleep(hours);

        Assert.Null(saved.Service.HoursBeforeSleep);
        Assert.Null(chosen.Service.HoursBeforeSleep);
    }

    /// <summary>The longest delay is accepted.</summary>
    [Fact]
    public void Accepts_the_longest_delay()
    {
        Harness harness = new(hours: null);

        harness.Service.SetHoursBeforeSleep(IdleSuspensionService.MaxHours);

        Assert.Equal(IdleSuspensionService.MaxHours, harness.Service.HoursBeforeSleep);
    }

    /// <summary>A service wired to in-memory fakes, its clock at <see cref="TestData.Now"/>.</summary>
    private sealed class Harness
    {
        /// <summary>Creates the harness.</summary>
        /// <param name="hours">Saved delay before a tab sleeps.</param>
        /// <param name="preferences">Other saved preferences.</param>
        public Harness(int? hours, ViewPreferences? preferences = null)
        {
            Store = new InMemoryPreferencesStore((preferences ?? ViewPreferences.None) with { SuspendAfterHours = hours });
            Probe = new FakeTabActivityProbe();
            Service = new IdleSuspensionService(Store, Probe, new FakeTimeProvider(TestData.Now));
        }

        /// <summary>The preferences store.</summary>
        public InMemoryPreferencesStore Store { get; }

        /// <summary>The activity probe.</summary>
        public FakeTabActivityProbe Probe { get; }

        /// <summary>The service under test.</summary>
        public IdleSuspensionService Service { get; }

        /// <summary>Builds a running tab with fresh ids.</summary>
        /// <param name="lastTouched">Last time the user touched it.</param>
        /// <param name="waitsForUser">True when its title carries the waiting mark.</param>
        /// <param name="isOnScreen">True when it is the shown tab of the active window.</param>
        /// <returns>The candidate.</returns>
        public static IdleCandidate Candidate(DateTimeOffset lastTouched, bool waitsForUser = true, bool isOnScreen = false)
        {
            SessionTab tab = TestData.Tab();
            return new IdleCandidate(tab.Id, tab.SessionId, waitsForUser, isOnScreen, lastTouched);
        }
    }
}
