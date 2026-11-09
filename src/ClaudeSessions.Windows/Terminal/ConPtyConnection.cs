using System;
using Microsoft.Terminal.Wpf;

namespace ClaudeSessions.Windows.Terminal;

/// <summary>
/// Connects a Windows Terminal control to a command run in a ConPTY pseudo-console. The process
/// starts when the control starts the connection, at the size the control last asked for.
/// </summary>
internal sealed class ConPtyConnection : ITerminalConnection, IDisposable
{
    private const int DefaultColumns = 120;
    private const int DefaultRows = 30;

    private readonly string _commandLine;
    private PseudoConsoleProcess? _process;
    private int _columns = DefaultColumns;
    private int _rows = DefaultRows;
    private bool _closed;

    /// <summary>Creates a connection that has not started yet.</summary>
    /// <param name="commandLine">Full command line of the process.</param>
    public ConPtyConnection(string commandLine)
    {
        _commandLine = commandLine;
    }

    /// <inheritdoc />
    public event EventHandler<TerminalOutputEventArgs>? TerminalOutput;

    /// <summary>Raised on a background thread with each chunk of output, after the terminal got it.</summary>
    public event Action<string>? OutputObserved;

    /// <summary>Raised on a background thread with the exit code once the process ends on its own.</summary>
    public event Action<int>? Exited;

    /// <summary>Raised when the process could not be started, with the reason.</summary>
    public event Action<string>? StartFailed;

    /// <summary>Raised on the calling thread each time input is written to the process.</summary>
    public event Action? InputWritten;

    /// <inheritdoc />
    public void Start()
    {
        if (_process is not null || _closed)
        {
            return;
        }
        try
        {
            _process = PseudoConsoleProcess.Start(_commandLine, _columns, _rows);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            StartFailed?.Invoke(exception.Message);
            return;
        }
        _process.OutputReceived += OnOutput;
        _process.Exited += code => Exited?.Invoke(code);
        _process.BeginReading();
    }

    /// <inheritdoc />
    public void WriteInput(string data)
    {
        _process?.Write(data);
        InputWritten?.Invoke();
    }

    /// <inheritdoc />
    public void Resize(uint rows, uint columns)
    {
        _rows = (int)rows;
        _columns = (int)columns;
        _process?.Resize(_columns, _rows);
    }

    /// <inheritdoc />
    public void Close()
    {
        Dispose();
    }

    /// <summary>Ends the process without raising <see cref="Exited"/>.</summary>
    public void Dispose()
    {
        _closed = true;
        Exited = null;
        OutputObserved = null;
        _process?.Dispose();
        _process = null;
    }

    private void OnOutput(string data)
    {
        TerminalOutput?.Invoke(this, new TerminalOutputEventArgs(data));
        OutputObserved?.Invoke(data);
    }
}
