using System;
using System.Windows;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Windows.Views;

/// <summary>Keeps a restored window reachable when the screens changed since it was saved.</summary>
internal static class ScreenBounds
{
    private const double MinimumVisibleWidth = 120;
    private const double MinimumVisibleHeight = 60;
    private const double MinimumWidth = 400;
    private const double MinimumHeight = 300;

    /// <summary>Returns the bounds unchanged when enough of the window lies on a screen, else the default bounds.</summary>
    /// <param name="bounds">The saved bounds.</param>
    /// <returns>Bounds whose title bar can be reached.</returns>
    public static WindowBounds KeepVisible(WindowBounds bounds)
    {
        Rect screen = new(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        Rect window = new(bounds.Left, bounds.Top, Math.Max(bounds.Width, MinimumWidth), Math.Max(bounds.Height, MinimumHeight));
        Rect overlap = Rect.Intersect(screen, window);
        bool reachable = !overlap.IsEmpty && overlap.Width >= MinimumVisibleWidth && overlap.Height >= MinimumVisibleHeight;
        WindowBounds kept = reachable ? bounds with { Width = window.Width, Height = window.Height } : WindowBounds.Default;
        return kept with { Maximized = bounds.Maximized };
    }
}
