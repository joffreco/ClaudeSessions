using System;
using System.Runtime.InteropServices;

namespace ClaudeSessions.Windows.Interop;

/// <summary>Win32 entry points of the windows, the keyboard and the monitors; those of the pseudo-console live in ClaudeSessions.Terminal.</summary>
internal static partial class NativeMethods
{
    /// <summary>Window message: a key is pressed.</summary>
    internal const int WmKeyDown = 0x0100;

    /// <summary>Window message: a key is pressed with Alt or F10.</summary>
    internal const int WmSysKeyDown = 0x0104;

    /// <summary>Window message: the mouse wheel turns.</summary>
    internal const int WmMouseWheel = 0x020A;

    /// <summary>Window message: the left button is pressed over the client area.</summary>
    internal const int WmLeftButtonDown = 0x0201;

    /// <summary>Window message: the right button is pressed over the client area.</summary>
    internal const int WmRightButtonDown = 0x0204;

    /// <summary>Window message: the middle button is pressed over the client area.</summary>
    internal const int WmMiddleButtonDown = 0x0207;

    /// <summary>Window message: the right button is pressed over the non-client area.</summary>
    internal const int WmNcRightButtonDown = 0x00A4;

    /// <summary>Window message: which part of the window lies under a point.</summary>
    internal const int WmNcHitTest = 0x0084;

    /// <summary>Window message: the left button is pressed over the non-client area.</summary>
    internal const int WmNcLeftButtonDown = 0x00A1;

    /// <summary>Window message: the left button is released over the non-client area.</summary>
    internal const int WmNcLeftButtonUp = 0x00A2;

    /// <summary>Window message: the mouse left the non-client area.</summary>
    internal const int WmNcMouseLeave = 0x02A2;

    /// <summary>Hit-test answer: the maximize button, over which Windows 11 shows its snap layouts.</summary>
    internal static readonly IntPtr HtMaxButton = 9;

    /// <summary>Window message: Windows asks for the maximized position and size of the window.</summary>
    internal const int WmGetMinMaxInfo = 0x0024;

    /// <summary>Window message: the position or size of the window is about to change.</summary>
    internal const int WmWindowPosChanging = 0x0046;

    /// <summary>WINDOWPOS flag: the size is kept.</summary>
    internal const uint SwpNoSize = 0x0001;

    /// <summary>WINDOWPOS flag: the position is kept.</summary>
    internal const uint SwpNoMove = 0x0002;

    /// <summary>MonitorFromWindow flag: the nearest monitor when the window lies on none.</summary>
    internal const uint MonitorDefaultToNearest = 0x00000002;

    /// <summary>ShowWindow command: hide the window.</summary>
    internal const uint SwHide = 0;

    /// <summary>ShowWindow command: the window is minimized.</summary>
    internal const uint SwShowMinimized = 2;

    /// <summary>ShowWindow command: the window is maximized.</summary>
    internal const uint SwShowMaximized = 3;

    /// <summary>WINDOWPLACEMENT flag: a minimized window comes back maximized.</summary>
    internal const uint WpfRestoreToMaximized = 0x0002;

    /// <summary>Win32 WINDOWPOS.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPos
    {
        /// <summary>The window.</summary>
        public IntPtr Hwnd;

        /// <summary>The window it is placed behind.</summary>
        public IntPtr HwndInsertAfter;

        /// <summary>Left edge.</summary>
        public int X;

        /// <summary>Top edge.</summary>
        public int Y;

        /// <summary>Width.</summary>
        public int Cx;

        /// <summary>Height.</summary>
        public int Cy;

        /// <summary>SWP_* flags.</summary>
        public uint Flags;
    }

    /// <summary>Win32 POINT, in pixels.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenPoint
    {
        /// <summary>Horizontal coordinate.</summary>
        public int X;

        /// <summary>Vertical coordinate.</summary>
        public int Y;
    }

    /// <summary>Win32 RECT, in pixels.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenRect
    {
        /// <summary>Left edge.</summary>
        public int Left;

        /// <summary>Top edge.</summary>
        public int Top;

        /// <summary>Right edge, excluded.</summary>
        public int Right;

        /// <summary>Bottom edge, excluded.</summary>
        public int Bottom;
    }

    /// <summary>Win32 MONITORINFO.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        /// <summary>Size of the structure.</summary>
        public int CbSize;

        /// <summary>Whole monitor, in virtual-screen coordinates.</summary>
        public ScreenRect RcMonitor;

        /// <summary>Monitor without the taskbar and docked bars.</summary>
        public ScreenRect RcWork;

        /// <summary>Monitor flags.</summary>
        public uint DwFlags;
    }

    /// <summary>Win32 MINMAXINFO.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MinMaxInfo
    {
        /// <summary>Reserved.</summary>
        public ScreenPoint PtReserved;

        /// <summary>Size of the maximized window.</summary>
        public ScreenPoint PtMaxSize;

        /// <summary>Position of the maximized window, relative to its monitor.</summary>
        public ScreenPoint PtMaxPosition;

        /// <summary>Smallest size the user can give the window.</summary>
        public ScreenPoint PtMinTrackSize;

        /// <summary>Largest size the user can give the window.</summary>
        public ScreenPoint PtMaxTrackSize;
    }

    /// <summary>Win32 WINDOWPLACEMENT: the state of a window and its bounds when restored, in workspace pixels.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPlacement
    {
        /// <summary>Size of the structure.</summary>
        public int Length;

        /// <summary>Placement flags.</summary>
        public uint Flags;

        /// <summary>ShowWindow command of the current state.</summary>
        public uint ShowCmd;

        /// <summary>Top-left corner when minimized.</summary>
        public ScreenPoint PtMinPosition;

        /// <summary>Top-left corner when maximized.</summary>
        public ScreenPoint PtMaxPosition;

        /// <summary>Bounds when neither minimized nor maximized.</summary>
        public ScreenRect RcNormalPosition;
    }

    /// <summary>Finds the monitor a window lies on.</summary>
    /// <param name="window">The window.</param>
    /// <param name="flags">What to return when the window lies on no monitor.</param>
    /// <returns>The monitor, or zero.</returns>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr MonitorFromWindow(IntPtr window, uint flags);

    /// <summary>Finds the monitor a rectangle mostly lies on.</summary>
    /// <param name="bounds">The rectangle, in screen pixels.</param>
    /// <param name="flags">What to return when the rectangle lies on no monitor.</param>
    /// <returns>The monitor, or zero.</returns>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr MonitorFromRect(ref ScreenRect bounds, uint flags);

    /// <summary>Finds the deepest window under a point of the screen.</summary>
    /// <param name="point">The point, in screen pixels.</param>
    /// <returns>The window, or zero.</returns>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr WindowFromPoint(ScreenPoint point);

    /// <summary>Finds the thread that created a window.</summary>
    /// <param name="window">The window.</param>
    /// <param name="processId">Receives the process id; may be zero.</param>
    /// <returns>The thread id, or 0 when the window does not exist.</returns>
    [LibraryImport("user32.dll")]
    internal static partial uint GetWindowThreadProcessId(IntPtr window, IntPtr processId);

    /// <summary>Id of the calling thread.</summary>
    /// <returns>The thread id.</returns>
    [LibraryImport("kernel32.dll")]
    internal static partial uint GetCurrentThreadId();

    /// <summary>Tells whether a window is maximized.</summary>
    /// <param name="window">The window.</param>
    /// <returns>True when it is maximized.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsZoomed(IntPtr window);

    /// <summary>Reads the bounds and work area of a monitor.</summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="info">Receives the bounds; its size member must be set.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    /// <summary>Reads the bounds of a window, frame included, in screen pixels.</summary>
    /// <param name="window">The window.</param>
    /// <param name="bounds">Receives the bounds.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(IntPtr window, out ScreenRect bounds);

    /// <summary>Virtual key of the left Ctrl key.</summary>
    internal const int VkLeftControl = 0xA2;

    /// <summary>Virtual key of the right Alt key, which is AltGr on the layouts that have one.</summary>
    internal const int VkRightMenu = 0xA5;

    /// <summary>ToUnicodeEx flag: leave the keyboard state alone, so a pending dead key survives the call.</summary>
    internal const uint ToUnicodeKeepKeyboardState = 0x4;

    /// <summary>Reads the state of a key for the calling thread.</summary>
    /// <param name="virtualKey">The virtual key.</param>
    /// <returns>Negative when the key is down.</returns>
    [LibraryImport("user32.dll")]
    internal static partial short GetKeyState(int virtualKey);

    /// <summary>Reads the state of every key for the calling thread.</summary>
    /// <param name="state">Receives 256 bytes, one per virtual key.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetKeyboardState([Out] byte[] state);

    /// <summary>Finds the keyboard layout of a thread.</summary>
    /// <param name="threadId">The thread; 0 for the calling one.</param>
    /// <returns>The layout.</returns>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetKeyboardLayout(uint threadId);

    /// <summary>Translates a key press into the characters it types.</summary>
    /// <param name="virtualKey">The virtual key.</param>
    /// <param name="scanCode">Its scan code.</param>
    /// <param name="state">State of every key.</param>
    /// <param name="buffer">Receives the characters.</param>
    /// <param name="bufferSize">Size of the buffer, in characters.</param>
    /// <param name="flags">Behaviour flags.</param>
    /// <param name="layout">The keyboard layout.</param>
    /// <returns>The number of characters; 0 for none, negative for a dead key.</returns>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int ToUnicodeEx(uint virtualKey, uint scanCode, [In] byte[] state, [Out] char[] buffer, int bufferSize, uint flags, IntPtr layout);

    /// <summary>Reads the state of a window and its restored bounds, in workspace pixels.</summary>
    /// <param name="window">The window.</param>
    /// <param name="placement">Receives the placement; its length member must be set.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowPlacement(IntPtr window, ref WindowPlacement placement);

    /// <summary>Sets the state of a window and its restored bounds, in workspace pixels.</summary>
    /// <param name="window">The window.</param>
    /// <param name="placement">The placement; its length member must be set.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPlacement(IntPtr window, ref WindowPlacement placement);

    /// <summary>Window attribute: the title bar and frame drawn by Windows use the dark mode.</summary>
    internal const int DwmUseImmersiveDarkMode = 20;

    /// <summary>Sets an attribute of the frame Windows draws around a window.</summary>
    /// <param name="window">The window.</param>
    /// <param name="attribute">The attribute, such as <see cref="DwmUseImmersiveDarkMode"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="size">Size of the value in bytes.</param>
    /// <returns>Zero on success, an HRESULT otherwise.</returns>
    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
