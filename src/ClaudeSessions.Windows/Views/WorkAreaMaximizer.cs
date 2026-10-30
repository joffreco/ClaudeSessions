using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ClaudeSessions.Windows.Interop;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// Maximizes a window without title bar onto the work area of its monitor. Left to Windows, such a
/// window overflows the screen by its frame on every side, cutting the edges of the tab strip; and
/// on a secondary monitor Windows may override the asked size, so the final position is forced too.
/// <see cref="Overflow"/> measures what is left, if anything.
/// </summary>
internal static class WorkAreaMaximizer
{
    /// <summary>Makes a window maximize onto the work area of its monitor; call before it is shown.</summary>
    /// <param name="window">The window.</param>
    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(window).Handle).AddHook(OnMessage);
    }

    /// <summary>Measures how far a window reaches past the work area of its monitor.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The overflow on each side, in device-independent units; zero where it does not overflow.</returns>
    public static Thickness Overflow(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero
            || !TryGetMonitorInfo(NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MonitorDefaultToNearest), out NativeMethods.MonitorInfo monitor)
            || !NativeMethods.GetWindowRect(hwnd, out NativeMethods.ScreenRect bounds))
        {
            return new Thickness(0);
        }
        NativeMethods.ScreenRect work = monitor.RcWork;
        DpiScale dpi = VisualTreeHelper.GetDpi(window);
        return new Thickness(
            Math.Max(0, work.Left - bounds.Left) / dpi.DpiScaleX,
            Math.Max(0, work.Top - bounds.Top) / dpi.DpiScaleY,
            Math.Max(0, bounds.Right - work.Right) / dpi.DpiScaleX,
            Math.Max(0, bounds.Bottom - work.Bottom) / dpi.DpiScaleY);
    }

    private static IntPtr OnMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (message)
        {
            case NativeMethods.WmGetMinMaxInfo:
                AskForWorkArea(hwnd, lParam);
                break;
            case NativeMethods.WmWindowPosChanging:
                KeepOnWorkArea(hwnd, lParam);
                break;
        }
        return IntPtr.Zero;
    }

    private static void AskForWorkArea(IntPtr hwnd, IntPtr minMaxInfo)
    {
        if (!TryGetMonitorInfo(NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MonitorDefaultToNearest), out NativeMethods.MonitorInfo monitor))
        {
            return;
        }
        NativeMethods.MinMaxInfo limits = Marshal.PtrToStructure<NativeMethods.MinMaxInfo>(minMaxInfo);
        limits.PtMaxPosition = new NativeMethods.ScreenPoint { X = monitor.RcWork.Left - monitor.RcMonitor.Left, Y = monitor.RcWork.Top - monitor.RcMonitor.Top };
        limits.PtMaxSize = new NativeMethods.ScreenPoint { X = monitor.RcWork.Right - monitor.RcWork.Left, Y = monitor.RcWork.Bottom - monitor.RcWork.Top };
        Marshal.StructureToPtr(limits, minMaxInfo, false);
    }

    private static void KeepOnWorkArea(IntPtr hwnd, IntPtr windowPos)
    {
        if (!NativeMethods.IsZoomed(hwnd))
        {
            return;
        }
        NativeMethods.WindowPos position = Marshal.PtrToStructure<NativeMethods.WindowPos>(windowPos);
        if ((position.Flags & (NativeMethods.SwpNoMove | NativeMethods.SwpNoSize)) != 0)
        {
            return;
        }
        NativeMethods.ScreenRect proposed = new() { Left = position.X, Top = position.Y, Right = position.X + position.Cx, Bottom = position.Y + position.Cy };
        if (!TryGetMonitorInfo(NativeMethods.MonitorFromRect(ref proposed, NativeMethods.MonitorDefaultToNearest), out NativeMethods.MonitorInfo monitor))
        {
            return;
        }
        NativeMethods.ScreenRect work = monitor.RcWork;
        position.X = work.Left;
        position.Y = work.Top;
        position.Cx = work.Right - work.Left;
        position.Cy = work.Bottom - work.Top;
        Marshal.StructureToPtr(position, windowPos, false);
    }

    private static bool TryGetMonitorInfo(IntPtr monitor, out NativeMethods.MonitorInfo info)
    {
        info = new NativeMethods.MonitorInfo { CbSize = Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        return monitor != IntPtr.Zero && NativeMethods.GetMonitorInfo(monitor, ref info);
    }
}
