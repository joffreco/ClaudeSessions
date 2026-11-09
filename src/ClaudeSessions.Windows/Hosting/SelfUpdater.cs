using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClaudeSessions.Adapters;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>
/// Updates the app from the WSL repository it was installed from: pulls and compares the commits, then,
/// once confirmed, publishes while the app keeps running and hands over to install.ps1, which waits for
/// the app to exit, copies the new version and starts it again.
/// </summary>
internal static class SelfUpdater
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromMinutes(10);

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
        WslRun run = await WslProcessRunner.RunAsync(wslExecutable, WslCommandLine.UpdateCheckArguments(source, InstalledRevision), StepTimeout).ConfigureAwait(false);
        UpdateCheck? check = run.Succeeded ? UpdateCheck.Parse(run.StandardOutput) : null;
        return (check, check is null ? run.Tail : "");
    }

    /// <summary>Publishes the app into the publish folder of the repository.</summary>
    /// <param name="wslExecutable">The wsl.exe to run.</param>
    /// <param name="source">The repository.</param>
    /// <returns>Whether it worked, with the last lines written.</returns>
    public static Task<WslRun> PublishAsync(string wslExecutable, InstallSource source)
    {
        return WslProcessRunner.RunAsync(wslExecutable, WslCommandLine.PublishArguments(source), StepTimeout);
    }

    /// <summary>
    /// Starts install.ps1 of the repository, hidden, so that it replaces the app once this process exits
    /// and starts the new version.
    /// </summary>
    /// <param name="source">The repository.</param>
    /// <returns>Null when the installer started, else why it did not.</returns>
    public static string? StartInstaller(InstallSource source)
    {
        // Not the app's working directory, the installation folder: the installer would lock the folder it replaces.
        ProcessStartInfo start = new("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetTempPath() };
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
}
