using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ClaudeSessions.Terminal.Interop;

/// <summary>Win32 and ConPTY entry points used to run a process inside a pseudo-console.</summary>
internal static partial class ConsoleNativeMethods
{
    /// <summary>Process creation flag: the STARTUPINFO is a STARTUPINFOEX.</summary>
    internal const uint ExtendedStartupInfoPresent = 0x00080000;

    /// <summary>Process creation flag: the environment block is Unicode.</summary>
    internal const uint CreateUnicodeEnvironment = 0x00000400;

    /// <summary>STARTUPINFO flag: the HStd* fields are the child's standard handles, so null ones leave the pseudo-console's in place instead of the parent's.</summary>
    internal const int StartUseStdHandles = 0x00000100;

    /// <summary>Attribute that attaches a pseudo-console to a new process.</summary>
    internal static readonly IntPtr ProcThreadAttributePseudoConsole = 0x00020016;

    /// <summary>Wait without time limit.</summary>
    internal const uint Infinite = 0xFFFFFFFF;

    /// <summary>Exit code given to a process this application terminates.</summary>
    internal const uint TerminatedExitCode = 1;

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
}
