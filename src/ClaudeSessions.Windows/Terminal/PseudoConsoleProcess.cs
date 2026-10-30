using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClaudeSessions.Windows.Interop;
using Microsoft.Win32.SafeHandles;

namespace ClaudeSessions.Windows.Terminal;

/// <summary>A process running inside a ConPTY pseudo-console, with its output read on a background thread.</summary>
internal sealed class PseudoConsoleProcess : IDisposable
{
    private const int ReadBufferSize = 8192;

    private readonly IntPtr _pseudoConsole;
    private readonly IntPtr _process;
    private readonly IntPtr _thread;
    private readonly FileStream _input;
    private readonly FileStream _output;
    private readonly object _lock = new();
    private bool _disposed;

    private PseudoConsoleProcess(IntPtr pseudoConsole, NativeMethods.ProcessInformation process, FileStream input, FileStream output)
    {
        _pseudoConsole = pseudoConsole;
        _process = process.HProcess;
        _thread = process.HThread;
        _input = input;
        _output = output;
    }

    /// <summary>Raised on a background thread with each chunk of output.</summary>
    public event Action<string>? OutputReceived;

    /// <summary>Raised on a background thread with the exit code once the process ends.</summary>
    public event Action<int>? Exited;

    /// <summary>Starts a process in a new pseudo-console.</summary>
    /// <param name="commandLine">Full command line of the process.</param>
    /// <param name="columns">Width of the console.</param>
    /// <param name="rows">Height of the console.</param>
    /// <returns>The running process; call <see cref="BeginReading"/> once the events are subscribed.</returns>
    /// <exception cref="Win32Exception">The pseudo-console or the process could not be created.</exception>
    public static PseudoConsoleProcess Start(string commandLine, int columns, int rows)
    {
        CreatePipeOrThrow(out SafeFileHandle consoleInput, out SafeFileHandle inputWriter);
        CreatePipeOrThrow(out SafeFileHandle outputReader, out SafeFileHandle consoleOutput);
        int result = NativeMethods.CreatePseudoConsole(SizeOf(columns, rows), consoleInput, consoleOutput, 0, out IntPtr pseudoConsole);
        consoleInput.Dispose();
        consoleOutput.Dispose();
        if (result != 0)
        {
            inputWriter.Dispose();
            outputReader.Dispose();
            throw new Win32Exception(result, "ConptyCreatePseudoConsole failed.");
        }
        try
        {
            NativeMethods.ProcessInformation process = CreateProcessIn(pseudoConsole, commandLine);
            return new PseudoConsoleProcess(
                pseudoConsole,
                process,
                new FileStream(inputWriter, FileAccess.Write),
                new FileStream(outputReader, FileAccess.Read));
        }
        catch
        {
            NativeMethods.ClosePseudoConsole(pseudoConsole);
            inputWriter.Dispose();
            outputReader.Dispose();
            throw;
        }
    }

    /// <summary>Starts the background reading of the output and the wait for the exit.</summary>
    public void BeginReading()
    {
        Task.Factory.StartNew(ReadOutput, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task.Factory.StartNew(WaitForExit, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>Sends keyboard input to the process.</summary>
    /// <param name="data">The input, as the terminal encodes it.</param>
    public void Write(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                _input.Write(bytes);
                _input.Flush();
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>Resizes the pseudo-console.</summary>
    /// <param name="columns">New width.</param>
    /// <param name="rows">New height.</param>
    public void Resize(int columns, int rows)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _ = NativeMethods.ResizePseudoConsole(_pseudoConsole, SizeOf(columns, rows));
            }
        }
    }

    /// <summary>Ends the process and releases the pseudo-console; no event is raised afterwards.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }
        OutputReceived = null;
        Exited = null;
        _ = NativeMethods.TerminateProcess(_process, NativeMethods.TerminatedExitCode);
        IntPtr pseudoConsole = _pseudoConsole;
        Task.Run(() => NativeMethods.ClosePseudoConsole(pseudoConsole));
        _input.Dispose();
    }

    private void ReadOutput()
    {
        using StreamReader reader = new(_output, new UTF8Encoding(false), false, ReadBufferSize);
        char[] buffer = new char[ReadBufferSize];
        try
        {
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                OutputReceived?.Invoke(new string(buffer, 0, read));
            }
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void WaitForExit()
    {
        _ = NativeMethods.WaitForSingleObject(_process, NativeMethods.Infinite);
        int exitCode = NativeMethods.GetExitCodeProcess(_process, out uint code) ? unchecked((int)code) : -1;
        Exited?.Invoke(exitCode);
        lock (_lock)
        {
            if (!_disposed)
            {
                _disposed = true;
                IntPtr pseudoConsole = _pseudoConsole;
                Task.Run(() => NativeMethods.ClosePseudoConsole(pseudoConsole));
                _input.Dispose();
            }
        }
        _ = NativeMethods.CloseHandle(_thread);
        _ = NativeMethods.CloseHandle(_process);
    }

    private static NativeMethods.ProcessInformation CreateProcessIn(IntPtr pseudoConsole, string commandLine)
    {
        IntPtr size = IntPtr.Zero;
        _ = NativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
        IntPtr attributeList = Marshal.AllocHGlobal(size);
        try
        {
            ThrowIfFalse(NativeMethods.InitializeProcThreadAttributeList(attributeList, 1, 0, ref size), "InitializeProcThreadAttributeList");
            try
            {
                ThrowIfFalse(
                    NativeMethods.UpdateProcThreadAttribute(attributeList, 0, NativeMethods.ProcThreadAttributePseudoConsole, pseudoConsole, IntPtr.Size, IntPtr.Zero, IntPtr.Zero),
                    "UpdateProcThreadAttribute");
                NativeMethods.StartupInfoEx startupInfo = new() { LpAttributeList = attributeList };
                startupInfo.StartupInfo.Cb = Marshal.SizeOf<NativeMethods.StartupInfoEx>();
                ThrowIfFalse(
                    NativeMethods.CreateProcess(
                        null,
                        commandLine,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        false,
                        NativeMethods.ExtendedStartupInfoPresent | NativeMethods.CreateUnicodeEnvironment,
                        IntPtr.Zero,
                        null,
                        ref startupInfo,
                        out NativeMethods.ProcessInformation process),
                    "CreateProcess");
                return process;
            }
            finally
            {
                NativeMethods.DeleteProcThreadAttributeList(attributeList);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(attributeList);
        }
    }

    private static void CreatePipeOrThrow(out SafeFileHandle readPipe, out SafeFileHandle writePipe)
    {
        ThrowIfFalse(NativeMethods.CreatePipe(out readPipe, out writePipe, IntPtr.Zero, 0), "CreatePipe");
    }

    private static void ThrowIfFalse(bool succeeded, string call)
    {
        if (!succeeded)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), call + " failed.");
        }
    }

    private static NativeMethods.Coord SizeOf(int columns, int rows)
    {
        return new NativeMethods.Coord
        {
            X = (short)Math.Clamp(columns, 1, short.MaxValue),
            Y = (short)Math.Clamp(rows, 1, short.MaxValue),
        };
    }
}
