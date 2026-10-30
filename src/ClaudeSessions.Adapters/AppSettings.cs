namespace ClaudeSessions.Adapters;

/// <summary>Settings of the application, read from settings.json; every value has a default.</summary>
public sealed record AppSettings
{
    /// <summary>Executable started for each tab.</summary>
    public string WslExecutable { get; init; } = "wsl.exe";

    /// <summary>WSL distribution that runs the sessions.</summary>
    public string Distro { get; init; } = "Debian";

    /// <summary>Home directory of the WSL user.</summary>
    public string WslHome { get; init; } = "/home/joff";

    /// <summary>WSL path of the claude-sessions-tab launcher.</summary>
    public string LauncherPath { get; init; } = "/home/joff/.local/bin/claude-sessions-tab";

    /// <summary>WSL directory of the first tab of a new window.</summary>
    public string DefaultCwd { get; init; } = "/home/joff/Sources";

    /// <summary>Font of the terminals.</summary>
    public string FontFamily { get; init; } = "Cascadia Mono";

    /// <summary>Font size of the terminals, in points.</summary>
    public short FontSize { get; init; } = 11;

    /// <summary>Folder where the hook records the session of each tab, as Windows sees it.</summary>
    public string SessionTabsDirectory => WindowsPathOf(WslHome.TrimEnd('/') + "/.claude/session-tabs");

    /// <summary>Converts a WSL path to the path Windows uses to reach it.</summary>
    /// <param name="wslPath">An absolute WSL path.</param>
    /// <returns>The \\wsl.localhost path of the same file.</returns>
    public string WindowsPathOf(string wslPath)
    {
        return @"\\wsl.localhost\" + Distro + wslPath.Replace('/', '\\');
    }
}
