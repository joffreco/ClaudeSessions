using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>Whether a restored window can still be grabbed by its title bar on a monitor.</summary>
// category N/A §13.4 (malicious): the bounds come from the app's own workspace file and Windows; no trust boundary is crossed.
public sealed class WindowBoundsTests
{
    private static readonly ScreenRectangle Laptop = new(-3840, 961, -2304, 1921);

    /// <summary>A window inside the work area is reachable.</summary>
    [Fact]
    public void Reaches_a_window_inside_the_work_area()
    {
        WindowBounds bounds = new(-3700, 1000, 768, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.True(reachable);
    }

    /// <summary>A window on a monitor that no longer exists, far from this one, is not reachable.</summary>
    [Fact]
    public void Does_not_reach_a_window_on_a_vanished_monitor()
    {
        WindowBounds bounds = new(3900, 300, 1200, 760, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.False(reachable);
    }

    /// <summary>A window whose title bar sits above the work area, only its body showing, is not reachable.</summary>
    [Fact]
    public void Does_not_reach_a_window_whose_title_bar_is_above_the_work_area()
    {
        WindowBounds bounds = new(-3700, 900, 768, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.False(reachable);
    }

    /// <summary>A window showing exactly the minimum of its title bar on the right edge is reachable.</summary>
    [Fact]
    public void Reaches_a_window_showing_exactly_the_minimum_width()
    {
        WindowBounds bounds = new(Laptop.Right - WindowBounds.ReachableTitleWidth, 1000, 768, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.True(reachable);
    }

    /// <summary>A window showing one pixel less than the minimum of its title bar is not reachable.</summary>
    [Fact]
    public void Does_not_reach_a_window_one_pixel_short_of_the_minimum_width()
    {
        WindowBounds bounds = new(Laptop.Right - WindowBounds.ReachableTitleWidth + 1, 1000, 768, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.False(reachable);
    }

    /// <summary>A window whose top sits exactly at the bottom edge of the title height is reachable.</summary>
    [Fact]
    public void Reaches_a_window_with_exactly_the_title_height_on_screen()
    {
        WindowBounds bounds = new(-3700, Laptop.Bottom - WindowBounds.ReachableTitleHeight, 768, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.True(reachable);
    }

    /// <summary>A window narrower than the minimum title width is reachable when all of its width is on screen.</summary>
    [Fact]
    public void Reaches_a_narrow_window_fully_on_screen()
    {
        WindowBounds bounds = new(-3700, 1000, 100, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.True(reachable);
    }

    /// <summary>A narrow window cut by the edge of the monitor is not reachable.</summary>
    [Fact]
    public void Does_not_reach_a_narrow_window_cut_by_the_edge()
    {
        WindowBounds bounds = new(Laptop.Right - 50, 1000, 100, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.False(reachable);
    }

    /// <summary>A window of zero or negative size is not reachable.</summary>
    /// <param name="width">Its width.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Does_not_reach_a_window_without_width(double width)
    {
        WindowBounds bounds = new(-3700, 1000, width, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.False(reachable);
    }

    /// <summary>A window spanning two monitors is reachable on the one holding enough of its title bar.</summary>
    [Fact]
    public void Reaches_a_window_straddling_the_edge_of_the_monitor()
    {
        WindowBounds bounds = new(Laptop.Right - 400, 1000, 768, 800, false, BoundsUnit.DevicePixels);

        bool reachable = bounds.TitleBarLiesOn(Laptop);

        Assert.True(reachable);
    }
}
