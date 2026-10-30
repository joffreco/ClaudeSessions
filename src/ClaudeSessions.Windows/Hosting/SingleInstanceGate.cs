using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>
/// Keeps a single instance per Windows session: a second launch asks the first one, through a
/// named pipe, to come to the front, then exits.
/// </summary>
internal sealed class SingleInstanceGate : IDisposable
{
    private const string MutexName = @"Local\ClaudeSessions.Instance";
    private const string PipeName = "ClaudeSessions.Activate";
    private const int ConnectTimeoutMilliseconds = 2000;

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstanceGate(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>Takes the instance lock, or hands over to the running instance.</summary>
    /// <returns>The gate when this process is the first instance; null when another one runs and was activated.</returns>
    public static SingleInstanceGate? TryAcquire()
    {
        Mutex mutex = new(true, MutexName, out bool created);
        if (created)
        {
            return new SingleInstanceGate(mutex);
        }
        mutex.Dispose();
        SignalRunningInstance();
        return null;
    }

    /// <summary>Listens for later launches; each one calls <paramref name="activate"/> on a background thread.</summary>
    /// <param name="activate">What to do when another launch asks for the window.</param>
    public void ListenForActivation(Action activate)
    {
        Task.Run(() => ListenAsync(activate));
    }

    /// <summary>Stops listening and releases the instance lock.</summary>
    public void Dispose()
    {
        _stop.Cancel();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        _stop.Dispose();
    }

    private async Task ListenAsync(Action activate)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeServerStream server = new(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                activate();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
            }
        }
    }

    private static void SignalRunningInstance()
    {
        try
        {
            using NamedPipeClientStream client = new(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(ConnectTimeoutMilliseconds);
        }
        catch (TimeoutException)
        {
        }
        catch (IOException)
        {
        }
    }
}
