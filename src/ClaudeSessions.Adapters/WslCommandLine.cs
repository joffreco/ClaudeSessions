using System.Linq;
using ClaudeSessions.Core.Application.Dto;

namespace ClaudeSessions.Adapters;

/// <summary>Builds the command line that runs the launcher of a tab in WSL.</summary>
public sealed class WslCommandLine
{
    private readonly AppSettings _settings;

    /// <summary>Creates the builder.</summary>
    /// <param name="settings">Distribution, launcher and executable to use.</param>
    public WslCommandLine(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Builds the command line of a tab.</summary>
    /// <param name="request">The tab, its session and its directory.</param>
    /// <returns>wsl.exe -d &lt;distro&gt; --cd &lt;cwd&gt; -e &lt;launcher&gt; &lt;tab&gt; &lt;session&gt;, each part quoted; without -d when no distribution is set.</returns>
    public string Build(TabLaunchRequest request)
    {
        string[] distro = string.IsNullOrEmpty(_settings.Distro) ? [] : ["-d", _settings.Distro];
        string[] arguments =
        [
            _settings.WslExecutable,
            .. distro,
            "--cd", request.Cwd,
            "-e", _settings.LauncherPath,
            request.TabId.ToString(),
            request.SessionId.ToString(),
        ];
        return string.Join(' ', arguments.Select(WindowsArgumentQuoter.Quote));
    }
}
