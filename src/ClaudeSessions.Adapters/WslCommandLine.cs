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

    /// <summary>
    /// Builds the arguments of wsl.exe that pull the repository (fast-forward only, never prompting for
    /// credentials, its messages on standard error), then print the head commit and the subjects of the
    /// commits since the installed one, as <see cref="UpdateCheck.Parse"/> reads them.
    /// </summary>
    /// <param name="source">The repository the app was installed from.</param>
    /// <param name="installedRevision">Commit of the installed app; null when unknown, which lists no change.</param>
    /// <returns>The arguments, one per entry, for a process start that quotes them itself.</returns>
    public static string[] UpdateCheckArguments(InstallSource source, string? installedRevision)
    {
        const string Script = "GIT_TERMINAL_PROMPT=0 git pull --ff-only >&2 && git rev-parse HEAD && { git log --format=%s $1..HEAD 2>/dev/null; true; }";
        string since = UpdateCheck.RevisionOf("+" + installedRevision) ?? "unknown";
        return [.. InRepository(source), "bash", "-lc", Script, "check", since];
    }

    /// <summary>Builds the arguments of wsl.exe that publish the app from the repository.</summary>
    /// <param name="source">The repository the app was installed from.</param>
    /// <returns>The arguments, one per entry, for a process start that quotes them itself.</returns>
    public static string[] PublishArguments(InstallSource source)
    {
        return [.. InRepository(source), "bash", "-lc", "./publish.sh"];
    }

    private static string[] InRepository(InstallSource source)
    {
        return ["-d", source.Distro, "--cd", source.RepoPath, "-e"];
    }
}
