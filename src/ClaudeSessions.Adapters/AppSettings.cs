namespace ClaudeSessions.Adapters;

/// <summary>Settings of the application, read from settings.json; every value has a default.</summary>
public sealed record AppSettings
{
    /// <summary>Executable started for each tab.</summary>
    public string WslExecutable { get; init; } = "wsl.exe";

    /// <summary>WSL distribution that runs the sessions; empty for the default distribution of WSL.</summary>
    public string Distro { get; init; } = "";

    /// <summary>Home directory of the WSL user.</summary>
    public string WslHome { get; init; } = "/home/joff";

    /// <summary>WSL path of the claude-sessions-tab launcher.</summary>
    public string LauncherPath { get; init; } = "/home/joff/.local/bin/claude-sessions-tab";

    /// <summary>WSL path of the claude-sessions-activity probe, read before a tab is put to sleep.</summary>
    public string ActivityProbePath { get; init; } = "/home/joff/.local/bin/claude-sessions-activity";

    /// <summary>WSL directory of the first tab of a new window.</summary>
    public string DefaultCwd { get; init; } = "/home/joff/Sources";

    /// <summary>Font of the terminals.</summary>
    public string FontFamily { get; init; } = "Cascadia Mono";

    /// <summary>Font size of the terminals, in points.</summary>
    public short FontSize { get; init; } = 11;

    /// <summary>How often the Consignes app reads the consignes folder, in seconds.</summary>
    public int ConsignesPollSeconds { get; init; } = 2;

    /// <summary>Folder where the sessions deposit their consignes, as Windows sees it.</summary>
    public string ConsignesDirectory => WindowsPathOf(WslHome.TrimEnd('/') + "/.claude/consignes");

    /// <summary>Converts a WSL path to the path Windows uses to reach it.</summary>
    /// <param name="wslPath">An absolute WSL path.</param>
    /// <returns>The \\wsl.localhost path of the same file.</returns>
    public string WindowsPathOf(string wslPath)
    {
        return @"\\wsl.localhost\" + Distro + wslPath.Replace('/', '\\');
    }
}
