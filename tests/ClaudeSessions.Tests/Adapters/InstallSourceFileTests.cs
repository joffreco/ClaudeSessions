using System.IO;
using ClaudeSessions.Adapters;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Reading install-source.json, the repository the app was installed from.</summary>
public sealed class InstallSourceFileTests
{
    /// <summary>The distribution and repository path written by install.ps1 are read back.</summary>
    [Fact]
    public void Reads_the_source_written_by_the_installer()
    {
        using TempFolder folder = new();
        string path = folder.File(InstallSourceFile.FileName);
        File.WriteAllText(path, """{ "distro": "Ubuntu-24.04", "repoPath": "/home/joff/Projets/ClaudeSessions" }""");

        InstallSource? source = InstallSourceFile.Load(path);

        Assert.Equal(new InstallSource("Ubuntu-24.04", "/home/joff/Projets/ClaudeSessions"), source);
    }

    /// <summary>An app installed from a Windows folder has no file, hence no source.</summary>
    [Fact]
    public void Gives_nothing_when_the_file_is_missing()
    {
        using TempFolder folder = new();

        Assert.Null(InstallSourceFile.Load(folder.File(InstallSourceFile.FileName)));
    }

    /// <summary>A broken, incomplete or relative source is ignored rather than used.</summary>
    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "distro": "Ubuntu-24.04" }""")]
    [InlineData("""{ "repoPath": "/home/joff/Projets/ClaudeSessions" }""")]
    [InlineData("""{ "distro": "Ubuntu-24.04", "repoPath": "Projets/ClaudeSessions" }""")]
    public void Ignores_an_unusable_file(string content)
    {
        using TempFolder folder = new();
        string path = folder.File(InstallSourceFile.FileName);
        File.WriteAllText(path, content);

        Assert.Null(InstallSourceFile.Load(path));
    }

    /// <summary>The installer is reached through the \\wsl.localhost share of the distribution.</summary>
    [Fact]
    public void Locates_the_installer_from_windows()
    {
        InstallSource source = new("Ubuntu-24.04", "/home/joff/Projets/ClaudeSessions/");

        Assert.Equal(@"\\wsl.localhost\Ubuntu-24.04\home\joff\Projets\ClaudeSessions\install.ps1", source.InstallerPath);
    }
}
