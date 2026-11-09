using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Windows.Interop;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// Reads and restores where a window lies through the window placement of Windows, in device pixels.
/// Unlike WPF positions, device pixels need no conversion between monitors of different scaling, so a
/// window comes back exactly where it was on each screen.
/// </summary>
internal static class WindowPlacer
{
    /// <summary>Reads the restored bounds and the maximized state of a window.</summary>
    /// <param name="window">The window; its handle must exist.</param>
    /// <returns>The bounds in device pixels, or null when the window has no handle or Windows refuses.</returns>
    public static WindowBounds? Read(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        NativeMethods.WindowPlacement placement = EmptyPlacement();
        if (hwnd == IntPtr.Zero || !NativeMethods.GetWindowPlacement(hwnd, ref placement))
        {
            return null;
        }
        NativeMethods.ScreenRect normal = placement.RcNormalPosition;
        return new WindowBounds(normal.Left, normal.Top, normal.Right - normal.Left, normal.Bottom - normal.Top, IsMaximized(placement), BoundsUnit.DevicePixels);
    }

    /// <summary>
    /// Puts a window, not shown yet, back at bounds saved in device pixels, when its title bar still lies
    /// on a monitor. The maximized state is left to the WPF window state, applied when the window shows.
    /// </summary>
    /// <param name="window">The window, between the creation of its handle and its first showing.</param>
    /// <param name="bounds">The saved bounds, in device pixels.</param>
    /// <returns>True when the window was placed; false when its monitor is gone or Windows refuses.</returns>
    public static bool TryRestore(Window window, WindowBounds bounds)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        NativeMethods.ScreenRect normal = new()
        {
            Left = (int)Math.Round(bounds.Left),
            Top = (int)Math.Round(bounds.Top),
            Right = (int)Math.Round(bounds.Left + bounds.Width),
            Bottom = (int)Math.Round(bounds.Top + bounds.Height),
        };
        if (hwnd == IntPtr.Zero || bounds.Unit != BoundsUnit.DevicePixels || WorkAreaNear(normal) is not ScreenRectangle workArea || !bounds.TitleBarLiesOn(workArea))
        {
            return false;
        }
        NativeMethods.WindowPlacement placement = EmptyPlacement();
        placement.ShowCmd = NativeMethods.SwHide;
        placement.RcNormalPosition = normal;
        // The first placement may move the window to a monitor of another scaling, which makes WPF rescale
        // it; the second puts back the exact size, on a monitor that no longer changes.
        return NativeMethods.SetWindowPlacement(hwnd, ref placement) && NativeMethods.SetWindowPlacement(hwnd, ref placement);
    }

    private static NativeMethods.WindowPlacement EmptyPlacement()
    {
        return new NativeMethods.WindowPlacement { Length = Marshal.SizeOf<NativeMethods.WindowPlacement>() };
    }

    private static bool IsMaximized(NativeMethods.WindowPlacement placement)
    {
        return placement.ShowCmd == NativeMethods.SwShowMaximized
            || (placement.ShowCmd == NativeMethods.SwShowMinimized && (placement.Flags & NativeMethods.WpfRestoreToMaximized) != 0);
    }

    private static ScreenRectangle? WorkAreaNear(NativeMethods.ScreenRect bounds)
    {
        NativeMethods.MonitorInfo monitor = new() { CbSize = Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        IntPtr handle = NativeMethods.MonitorFromRect(ref bounds, NativeMethods.MonitorDefaultToNearest);
        if (handle == IntPtr.Zero || !NativeMethods.GetMonitorInfo(handle, ref monitor))
        {
            return null;
        }
        NativeMethods.ScreenRect work = monitor.RcWork;
        return new ScreenRectangle(work.Left, work.Top, work.Right, work.Bottom);
    }
}
