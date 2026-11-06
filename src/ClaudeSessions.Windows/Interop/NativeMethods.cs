using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ClaudeSessions.Windows.Interop;

/// <summary>Win32 and ConPTY entry points used to run a process inside a pseudo-console.</summary>
internal static partial class NativeMethods
{
    /// <summary>Process creation flag: the STARTUPINFO is a STARTUPINFOEX.</summary>
    internal const uint ExtendedStartupInfoPresent = 0x00080000;

    /// <summary>Process creation flag: the environment block is Unicode.</summary>
    internal const uint CreateUnicodeEnvironment = 0x00000400;

    /// <summary>Attribute that attaches a pseudo-console to a new process.</summary>
    internal static readonly IntPtr ProcThreadAttributePseudoConsole = 0x00020016;

    /// <summary>Wait without time limit.</summary>
    internal const uint Infinite = 0xFFFFFFFF;

    /// <summary>Exit code given to a process this application terminates.</summary>
    internal const uint TerminatedExitCode = 1;

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

    /// <summary>Character cell size of a console.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Coord
    {
        /// <summary>Columns.</summary>
        public short X;

        /// <summary>Rows.</summary>
        public short Y;
    }

    /// <summary>Win32 STARTUPINFOW.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct StartupInfo
    {
        /// <summary>Size of the structure, here of the STARTUPINFOEX that holds it.</summary>
        public int Cb;

        /// <summary>Reserved.</summary>
        public IntPtr LpReserved;

        /// <summary>Desktop name.</summary>
        public IntPtr LpDesktop;

        /// <summary>Console title.</summary>
        public IntPtr LpTitle;

        /// <summary>Window X position.</summary>
        public int DwX;

        /// <summary>Window Y position.</summary>
        public int DwY;

        /// <summary>Window width.</summary>
        public int DwXSize;

        /// <summary>Window height.</summary>
        public int DwYSize;

        /// <summary>Console width in characters.</summary>
        public int DwXCountChars;

        /// <summary>Console height in characters.</summary>
        public int DwYCountChars;

        /// <summary>Console colors.</summary>
        public int DwFillAttribute;

        /// <summary>Which members are used.</summary>
        public int DwFlags;

        /// <summary>Show state of the window.</summary>
        public short WShowWindow;

        /// <summary>Reserved.</summary>
        public short CbReserved2;

        /// <summary>Reserved.</summary>
        public IntPtr LpReserved2;

        /// <summary>Standard input.</summary>
        public IntPtr HStdInput;

        /// <summary>Standard output.</summary>
        public IntPtr HStdOutput;

        /// <summary>Standard error.</summary>
        public IntPtr HStdError;
    }

    /// <summary>Win32 STARTUPINFOEXW.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupInfoEx
    {
        /// <summary>The base structure.</summary>
        public StartupInfo StartupInfo;

        /// <summary>Attribute list holding the pseudo-console.</summary>
        public IntPtr LpAttributeList;
    }

    /// <summary>Win32 PROCESS_INFORMATION.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        /// <summary>Handle of the new process.</summary>
        public IntPtr HProcess;

        /// <summary>Handle of its main thread.</summary>
        public IntPtr HThread;

        /// <summary>Process id.</summary>
        public int DwProcessId;

        /// <summary>Thread id.</summary>
        public int DwThreadId;
    }

    /// <summary>Creates an anonymous pipe.</summary>
    /// <param name="readPipe">Read end.</param>
    /// <param name="writePipe">Write end.</param>
    /// <param name="attributes">Security attributes; zero for the defaults.</param>
    /// <param name="size">Buffer size; zero for the default.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CreatePipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, IntPtr attributes, int size);

    /// <summary>Creates a pseudo-console (conpty.dll of the ConPTY package).</summary>
    /// <param name="size">Size in character cells.</param>
    /// <param name="input">Pipe the console reads its input from.</param>
    /// <param name="output">Pipe the console writes its output to.</param>
    /// <param name="flags">Creation flags.</param>
    /// <param name="pseudoConsole">The new pseudo-console.</param>
    /// <returns>An HRESULT.</returns>
    [LibraryImport("conpty.dll", EntryPoint = "ConptyCreatePseudoConsole")]
    internal static partial int CreatePseudoConsole(Coord size, SafeFileHandle input, SafeFileHandle output, uint flags, out IntPtr pseudoConsole);

    /// <summary>Resizes a pseudo-console.</summary>
    /// <param name="pseudoConsole">The pseudo-console.</param>
    /// <param name="size">New size in character cells.</param>
    /// <returns>An HRESULT.</returns>
    [LibraryImport("conpty.dll", EntryPoint = "ConptyResizePseudoConsole")]
    internal static partial int ResizePseudoConsole(IntPtr pseudoConsole, Coord size);

    /// <summary>Closes a pseudo-console; its clients receive a close event.</summary>
    /// <param name="pseudoConsole">The pseudo-console.</param>
    [LibraryImport("conpty.dll", EntryPoint = "ConptyClosePseudoConsole")]
    internal static partial void ClosePseudoConsole(IntPtr pseudoConsole);

    /// <summary>Initializes a process attribute list.</summary>
    /// <param name="attributeList">The list, or zero to query its size.</param>
    /// <param name="attributeCount">Number of attributes.</param>
    /// <param name="flags">Reserved, zero.</param>
    /// <param name="size">Size of the list.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool InitializeProcThreadAttributeList(IntPtr attributeList, int attributeCount, int flags, ref IntPtr size);

    /// <summary>Sets one attribute of a process attribute list.</summary>
    /// <param name="attributeList">The list.</param>
    /// <param name="flags">Reserved, zero.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="value">Its value.</param>
    /// <param name="size">Size of the value.</param>
    /// <param name="previousValue">Reserved, zero.</param>
    /// <param name="returnSize">Reserved, zero.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UpdateProcThreadAttribute(IntPtr attributeList, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previousValue, IntPtr returnSize);

    /// <summary>Releases a process attribute list.</summary>
    /// <param name="attributeList">The list.</param>
    [LibraryImport("kernel32.dll")]
    internal static partial void DeleteProcThreadAttributeList(IntPtr attributeList);

    /// <summary>Starts a process.</summary>
    /// <param name="applicationName">Executable, or null to take it from the command line.</param>
    /// <param name="commandLine">Command line; the buffer may be modified.</param>
    /// <param name="processAttributes">Zero.</param>
    /// <param name="threadAttributes">Zero.</param>
    /// <param name="inheritHandles">False: the pseudo-console carries the standard handles.</param>
    /// <param name="creationFlags">Creation flags.</param>
    /// <param name="environment">Zero to inherit the environment.</param>
    /// <param name="currentDirectory">Null to inherit the directory.</param>
    /// <param name="startupInfo">Startup information with the attribute list.</param>
    /// <param name="processInformation">Handles and ids of the new process.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CreateProcess(
        string? applicationName,
        [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfoEx startupInfo,
        out ProcessInformation processInformation);

    /// <summary>Waits for an object to be signaled.</summary>
    /// <param name="handle">The object.</param>
    /// <param name="milliseconds">Time limit.</param>
    /// <returns>The wait result.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    /// <summary>Reads the exit code of a process.</summary>
    /// <param name="process">The process.</param>
    /// <param name="exitCode">Its exit code.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    /// <summary>Terminates a process.</summary>
    /// <param name="process">The process.</param>
    /// <param name="exitCode">Exit code to give it.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TerminateProcess(IntPtr process, uint exitCode);

    /// <summary>Closes a kernel handle.</summary>
    /// <param name="handle">The handle.</param>
    /// <returns>True on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr handle);

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

    /// <summary>Finds the window in the foreground, for the temporary flash trace.</summary>
    /// <returns>The window, or zero.</returns>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetForegroundWindow();

    /// <summary>Flash flag: flash the taskbar button.</summary>
    internal const uint FlashTray = 0x00000002;

    /// <summary>Flash flag: stop flashing and restore the taskbar button.</summary>
    internal const uint FlashStop = 0;

    /// <summary>Parameters of <see cref="FlashWindowEx"/> (FLASHWINFO).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct FlashInfo
    {
        /// <summary>Size of the structure, in bytes.</summary>
        public uint Size;

        /// <summary>The window to flash.</summary>
        public IntPtr Window;

        /// <summary>What to flash, and until when.</summary>
        public uint Flags;

        /// <summary>Number of flashes, after which the button stays highlighted.</summary>
        public uint Count;

        /// <summary>Flash period in milliseconds; 0 for the cursor blink rate.</summary>
        public uint Timeout;
    }

    /// <summary>Flashes a window's taskbar button, or stops it.</summary>
    /// <param name="info">The window and the flags.</param>
    /// <returns>True when the window was active before the call.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FlashWindowEx(ref FlashInfo info);
}
