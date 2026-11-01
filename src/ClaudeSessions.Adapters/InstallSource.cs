namespace ClaudeSessions.Adapters;

/// <summary>The WSL repository the installed app was published from, written by install.ps1 next to the app.</summary>
/// <param name="Distro">WSL distribution holding the repository.</param>
/// <param name="RepoPath">Absolute WSL path of the repository.</param>
public sealed record InstallSource(string Distro, string RepoPath)
{
    /// <summary>The installer of the repository, as Windows reaches it.</summary>
    public string InstallerPath => @"\\wsl.localhost\" + Distro + RepoPath.TrimEnd('/').Replace('/', '\\') + @"\install.ps1";
}
