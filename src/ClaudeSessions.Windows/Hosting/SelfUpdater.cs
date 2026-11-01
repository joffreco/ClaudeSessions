using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClaudeSessions.Adapters;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Outcome of a step run in WSL.</summary>
/// <param name="Succeeded">True when the step exited with code 0.</param>
/// <param name="StandardOutput">Everything written on standard output.</param>
/// <param name="Tail">The last lines written on either stream, to explain a failure.</param>
internal sealed record WslRun(bool Succeeded, string StandardOutput, string Tail);

/// <summary>
/// Updates the app from the WSL repository it was installed from: pulls and compares the commits, then,
/// once confirmed, publishes while the app keeps running and hands over to install.ps1, which waits for
/// the app to exit, copies the new version and starts it again.
/// </summary>
internal static class SelfUpdater
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromMinutes(10);
    private const int TailLines = 25;

    /// <summary>The repository the installed app comes from; null when it was not installed from WSL.</summary>
    public static InstallSource? Source => InstallSourceFile.Load(Path.Combine(AppContext.BaseDirectory, InstallSourceFile.FileName));

    /// <summary>Commit the running app was built from; null when unknown.</summary>
    public static string? InstalledRevision => UpdateCheck.RevisionOf(typeof(SelfUpdater).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Pulls the repository (fast-forward only) and lists what the running app lacks.</summary>
    /// <param name="wslExecutable">The wsl.exe to run.</param>
    /// <param name="source">The repository.</param>
    /// <returns>The check, or why it failed.</returns>
    public static async Task<(UpdateCheck? Check, string Failure)> CheckAsync(string wslExecutable, InstallSource source)
    {
        WslRun run = await RunAsync(wslExecutable, WslCommandLine.UpdateCheckArguments(source, InstalledRevision)).ConfigureAwait(false);
        UpdateCheck? check = run.Succeeded ? UpdateCheck.Parse(run.StandardOutput) : null;
        return (check, check is null ? run.Tail : "");
    }

    /// <summary>Publishes the app into the publish folder of the repository.</summary>
    /// <param name="wslExecutable">The wsl.exe to run.</param>
    /// <param name="source">The repository.</param>
    /// <returns>Whether it worked, with the last lines written.</returns>
    public static Task<WslRun> PublishAsync(string wslExecutable, InstallSource source)
    {
        return RunAsync(wslExecutable, WslCommandLine.PublishArguments(source));
    }

    /// <summary>
    /// Starts install.ps1 of the repository, hidden, so that it replaces the app once this process exits
    /// and starts the new version.
    /// </summary>
    /// <param name="source">The repository.</param>
    /// <returns>Null when the installer started, else why it did not.</returns>
    public static string? StartInstaller(InstallSource source)
    {
        ProcessStartInfo start = new("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        string[] arguments =
        [
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", source.InstallerPath,
            "-WaitForProcess", Environment.ProcessId.ToString(CultureInfo.InvariantCulture), "-Restart",
        ];
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        try
        {
            using Process? process = Process.Start(start);
            return process is null ? "powershell.exe n'a pas démarré." : null;
        }
        catch (Win32Exception exception)
        {
            return $"powershell.exe n'a pas pu démarrer : {exception.Message}";
        }
    }

    /// <summary>Joins lines into a bulleted list for a message, keeping at most <paramref name="limit"/> of them.</summary>
    /// <param name="lines">The lines.</param>
    /// <param name="limit">How many lines to show.</param>
    /// <returns>One bullet per line, then how many were left out.</returns>
    public static string BulletList(IReadOnlyList<string> lines, int limit = 15)
    {
        IEnumerable<string> shown = lines.Take(limit).Select(line => "• " + line);
        string more = lines.Count > limit ? $"\n… et {lines.Count - limit} autres" : "";
        return string.Join('\n', shown) + more;
    }

    private static async Task<WslRun> RunAsync(string wslExecutable, IEnumerable<string> arguments)
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
        using CancellationTokenSource timeout = new(StepTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return new WslRun(false, "", $"Arrêt après {StepTimeout.TotalMinutes} minutes sans fin.\n\n{Tail()}");
        }
        lock (gate)
        {
            return new WslRun(process.ExitCode == 0, output.ToString(), string.Join('\n', tail));
        }
    }
}
