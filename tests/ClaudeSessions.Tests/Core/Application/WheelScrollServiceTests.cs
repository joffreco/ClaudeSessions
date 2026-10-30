using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Core.Application;

/// <summary>Choosing the lines per wheel notch and rescaling the wheel.</summary>
public sealed class WheelScrollServiceTests
{
    /// <summary>Without a saved choice, the Windows setting applies.</summary>
    [Fact]
    public void Follows_windows_without_a_saved_choice()
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(ViewPreferences.None));

        Assert.Null(wheel.LinesPerNotch);
        Assert.Null(wheel.Rescale(120, 3));
    }

    /// <summary>A saved choice is read back at start.</summary>
    [Fact]
    public void Starts_with_the_saved_choice()
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(new ViewPreferences(null, 5)));

        Assert.Equal(5, wheel.LinesPerNotch);
    }

    /// <summary>A choice is saved and keeps the zoom.</summary>
    [Fact]
    public void Saves_a_choice_and_keeps_the_zoom()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(14, null));
        WheelScrollService wheel = new(store);

        bool changed = wheel.SetLinesPerNotch(10);

        Assert.True(changed);
        Assert.Equal(new ViewPreferences(14, 10), store.Saved);
    }

    /// <summary>Going back to the Windows setting forgets the choice.</summary>
    [Fact]
    public void Going_back_to_windows_forgets_the_choice()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(null, 5));
        WheelScrollService wheel = new(store);

        wheel.SetLinesPerNotch(null);

        Assert.Equal(ViewPreferences.None, store.Saved);
    }

    /// <summary>Choosing the current value again saves nothing.</summary>
    [Fact]
    public void Choosing_the_same_value_saves_nothing()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(null, 5));
        WheelScrollService wheel = new(store);

        bool changed = wheel.SetLinesPerNotch(5);

        Assert.False(changed);
        Assert.Equal(0, store.SaveCount);
    }

    /// <summary>A notch gives the chosen lines whatever the Windows setting, by the terminal's own rule of one line per 120 / setting.</summary>
    [Theory]
    [InlineData(1, 3, 40)]
    [InlineData(10, 3, 400)]
    [InlineData(1, 1, 120)]
    [InlineData(5, 5, 120)]
    [InlineData(3, 7, 51)]
    public void Rescales_a_notch_to_the_chosen_lines(int lines, int systemLines, int expected)
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(new ViewPreferences(null, lines)));

        int? delta = wheel.Rescale(120, systemLines);

        Assert.Equal(expected, delta);
        Assert.Equal(lines, delta!.Value / (120 / systemLines));
    }

    /// <summary>Scrolling down keeps its direction.</summary>
    [Fact]
    public void Keeps_the_direction_of_the_wheel()
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(new ViewPreferences(null, 10)));

        Assert.Equal(-400, wheel.Rescale(-120, 3));
    }

    /// <summary>A Windows setting of a page per notch, or of nothing, leaves the wheel as it is.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(121)]
    public void Leaves_the_wheel_alone_for_page_or_odd_settings(int systemLines)
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(new ViewPreferences(null, 5)));

        Assert.Null(wheel.Rescale(120, systemLines));
    }

    /// <summary>A huge delta stays within the 16 bits of the wheel message.</summary>
    [Fact]
    public void Keeps_a_huge_delta_within_the_message()
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(new ViewPreferences(null, WheelScrollService.MaxLines)));

        Assert.Equal(short.MaxValue, wheel.Rescale(short.MaxValue, 1));
    }

    /// <summary>A saved value out of range, from a hand-edited file, counts as no choice.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(101)]
    public void Ignores_an_out_of_range_saved_value(int saved)
    {
        WheelScrollService wheel = new(new InMemoryPreferencesStore(new ViewPreferences(null, saved)));

        Assert.Null(wheel.LinesPerNotch);
    }
}
