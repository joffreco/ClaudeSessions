using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeSessions.Adapters;

/// <summary>
/// Keeps a single instance of an application per Windows session: a second launch asks the first one,
/// through a named pipe, to come to the front, then exits.
/// </summary>
public sealed class SingleInstanceGate : IDisposable
{
    /// <summary>The name of the ClaudeSessions application's gate.</summary>
    public const string ClaudeSessions = "ClaudeSessions";

    /// <summary>The name of the Consignes application's gate.</summary>
    public const string Consignes = "Consignes";

    private const int ConnectTimeoutMilliseconds = 2000;

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstanceGate(Mutex mutex, string pipeName)
    {
        _mutex = mutex;
        _pipeName = pipeName;
    }

    /// <summary>Takes the instance lock of an application, or hands over to its running instance.</summary>
    /// <param name="application">The application's gate name, <see cref="ClaudeSessions"/> or <see cref="Consignes"/>.</param>
    /// <returns>The gate when this process is the first instance; null when another one runs and was activated.</returns>
    public static SingleInstanceGate? TryAcquire(string application)
    {
        Mutex mutex = new(true, $@"Local\{application}.Instance", out bool created);
        string pipeName = application + ".Activate";
        if (created)
        {
            return new SingleInstanceGate(mutex, pipeName);
        }
        mutex.Dispose();
        SignalRunningInstance(pipeName);
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
                await using NamedPipeServerStream server = new(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
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

    private static void SignalRunningInstance(string pipeName)
    {
        try
        {
            using NamedPipeClientStream client = new(".", pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
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
