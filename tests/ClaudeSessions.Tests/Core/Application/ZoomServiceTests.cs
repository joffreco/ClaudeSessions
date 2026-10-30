using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Core.Application;

/// <summary>Zooming the terminals and keeping the zoom.</summary>
public sealed class ZoomServiceTests
{
    /// <summary>Without a saved zoom, the terminals start at the configured size.</summary>
    [Fact]
    public void Starts_at_the_configured_size()
    {
        ZoomService zoom = new(new InMemoryPreferencesStore(ViewPreferences.None), 11);

        Assert.Equal(11, zoom.Size);
    }

    /// <summary>A saved zoom wins over the configured size.</summary>
    [Fact]
    public void Starts_at_the_saved_size()
    {
        ZoomService zoom = new(new InMemoryPreferencesStore(new ViewPreferences(14, null)), 11);

        Assert.Equal(14, zoom.Size);
    }

    /// <summary>Zooming in adds one point and saves the new size.</summary>
    [Fact]
    public void Zooms_in_by_one_point_and_saves_it()
    {
        InMemoryPreferencesStore store = new(ViewPreferences.None);
        ZoomService zoom = new(store, 11);

        bool changed = zoom.ZoomIn();

        Assert.True(changed);
        Assert.Equal(12, zoom.Size);
        Assert.Equal((short?)12, store.Saved.FontSize);
    }

    /// <summary>Zooming out takes one point off and saves the new size.</summary>
    [Fact]
    public void Zooms_out_by_one_point_and_saves_it()
    {
        InMemoryPreferencesStore store = new(ViewPreferences.None);
        ZoomService zoom = new(store, 11);

        zoom.ZoomOut();

        Assert.Equal(10, zoom.Size);
        Assert.Equal((short?)10, store.Saved.FontSize);
    }

    /// <summary>Going back to the configured size forgets the saved zoom.</summary>
    [Fact]
    public void Reset_forgets_the_saved_zoom()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(15, null));
        ZoomService zoom = new(store, 11);

        bool changed = zoom.Reset();

        Assert.True(changed);
        Assert.Equal(11, zoom.Size);
        Assert.Null(store.Saved.FontSize);
    }

    /// <summary>Zooming back onto the configured size forgets the zoom too, so a later change of the settings applies.</summary>
    [Fact]
    public void Forgets_the_zoom_when_it_comes_back_to_the_configured_size()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(12, null));
        ZoomService zoom = new(store, 11);

        zoom.ZoomOut();

        Assert.Equal(11, zoom.Size);
        Assert.Null(store.Saved.FontSize);
    }

    /// <summary>Resetting at the configured size changes and saves nothing.</summary>
    [Fact]
    public void Reset_at_the_configured_size_saves_nothing()
    {
        InMemoryPreferencesStore store = new(ViewPreferences.None);
        ZoomService zoom = new(store, 11);

        bool changed = zoom.Reset();

        Assert.False(changed);
        Assert.Equal(0, store.SaveCount);
    }

    /// <summary>The font never grows past the largest size, and a refused step saves nothing.</summary>
    [Fact]
    public void Stops_at_the_largest_size()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(ZoomService.MaxSize, null));
        ZoomService zoom = new(store, 11);

        bool changed = zoom.ZoomIn();

        Assert.False(changed);
        Assert.Equal(ZoomService.MaxSize, zoom.Size);
        Assert.Equal(0, store.SaveCount);
    }

    /// <summary>The font never shrinks below the smallest size.</summary>
    [Fact]
    public void Stops_at_the_smallest_size()
    {
        ZoomService zoom = new(new InMemoryPreferencesStore(new ViewPreferences(ZoomService.MinSize, null)), 11);

        bool changed = zoom.ZoomOut();

        Assert.False(changed);
        Assert.Equal(ZoomService.MinSize, zoom.Size);
    }

    /// <summary>A saved size out of range, from a hand-edited file, is brought back into range.</summary>
    [Fact]
    public void Brings_an_out_of_range_saved_size_back_into_range()
    {
        ZoomService zoom = new(new InMemoryPreferencesStore(new ViewPreferences(-3, null)), 11);

        Assert.Equal(ZoomService.MinSize, zoom.Size);
    }

    /// <summary>Zooming keeps the other preferences.</summary>
    [Fact]
    public void Keeps_the_wheel_choice_when_zooming()
    {
        InMemoryPreferencesStore store = new(new ViewPreferences(null, 5));
        ZoomService zoom = new(store, 11);

        zoom.ZoomIn();

        Assert.Equal(new ViewPreferences(12, 5), store.Saved);
    }

    /// <summary>A configured size out of range is brought back into range as well.</summary>
    [Fact]
    public void Brings_an_out_of_range_configured_size_back_into_range()
    {
        ZoomService zoom = new(new InMemoryPreferencesStore(ViewPreferences.None), 500);

        Assert.Equal(ZoomService.MaxSize, zoom.Size);
    }
}
