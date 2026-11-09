using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Runs one command in WSL through wsl.exe and collects its output.</summary>
internal static class WslProcessRunner
{
    private const int TailLines = 25;

    /// <summary>Runs wsl.exe with nothing on its standard input and collects what it writes.</summary>
    /// <param name="wslExecutable">The wsl.exe to run.</param>
    /// <param name="arguments">Its arguments, one per entry.</param>
    /// <param name="timeout">Longest run; the process tree is killed past it.</param>
    /// <returns>The outcome of the run.</returns>
    public static async Task<WslRun> RunAsync(string wslExecutable, IEnumerable<string> arguments, TimeSpan timeout)
    {
        ProcessStartInfo start = new(wslExecutable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        StringBuilder output = new();
        Queue<string> tail = new();
        object gate = new();
        void Keep(string? line, bool isOutput)
        {
            if (line is null)
            {
                return;
            }
            lock (gate)
            {
                if (isOutput)
                {
                    output.Append(line).Append('\n');
                }
                tail.Enqueue(line);
                if (tail.Count > TailLines)
                {
                    tail.Dequeue();
                }
            }
        }
        string Tail()
        {
            lock (gate)
            {
                return string.Join('\n', tail);
            }
        }

        using Process process = new() { StartInfo = start };
        process.OutputDataReceived += (_, e) => Keep(e.Data, isOutput: true);
        process.ErrorDataReceived += (_, e) => Keep(e.Data, isOutput: false);
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            return new WslRun(false, "", $"{wslExecutable} n'a pas pu démarrer : {exception.Message}");
        }
        // Nothing may wait for an answer: a credentials prompt fails instead of hanging.
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using CancellationTokenSource deadline = new(timeout);
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return new WslRun(false, "", $"Arrêt après {Duration(timeout)} sans fin.\n\n{Tail()}");
        }
        lock (gate)
        {
            return new WslRun(process.ExitCode == 0, output.ToString(), string.Join('\n', tail));
        }
    }

    private static string Duration(TimeSpan timeout)
    {
        return timeout.TotalMinutes >= 1 ? $"{timeout.TotalMinutes} minutes" : $"{timeout.TotalSeconds} secondes";
    }
}
