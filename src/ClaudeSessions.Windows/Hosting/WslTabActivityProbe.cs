using System;
using System.Threading.Tasks;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Reads the activity of a tab's session by running claude-sessions-activity in WSL.</summary>
internal sealed class WslTabActivityProbe : ITabActivityProbe
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(15);

    private readonly string _wslExecutable;
    private readonly WslCommandLine _commandLine;

    /// <summary>Creates the probe.</summary>
    /// <param name="wslExecutable">The wsl.exe to run.</param>
    /// <param name="commandLine">Builds the arguments of the probe.</param>
    public WslTabActivityProbe(string wslExecutable, WslCommandLine commandLine)
    {
        _wslExecutable = wslExecutable;
        _commandLine = commandLine;
    }

    /// <inheritdoc />
    public async Task<TabActivity?> ReadAsync(TabId tabId, SessionId sessionId)
    {
        WslRun run = await WslProcessRunner.RunAsync(_wslExecutable, _commandLine.ActivityArguments(tabId, sessionId), ProbeTimeout).ConfigureAwait(false);
        return run.Succeeded ? TabActivityReport.Parse(run.StandardOutput) : null;
    }
}
