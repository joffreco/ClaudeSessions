using ClaudeSessions.Adapters;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Paths derived from the settings.</summary>
public sealed class AppSettingsTests
{
    /// <summary>A WSL path maps to its \\wsl.localhost path.</summary>
    [Fact]
    public void Maps_a_wsl_path_to_its_windows_path()
    {
        AppSettings settings = new() { Distro = "Debian" };

        string path = settings.WindowsPathOf("/home/joff/Sources");

        Assert.Equal(@"\\wsl.localhost\Debian\home\joff\Sources", path);
    }

    /// <summary>The consignes live under the home directory of the WSL user, whatever trailing slash it carries.</summary>
    [Fact]
    public void Locates_the_consignes_under_the_wsl_home()
    {
        AppSettings settings = new() { Distro = "Ubuntu", WslHome = "/home/someone/" };

        string folder = settings.ConsignesDirectory;

        Assert.Equal(@"\\wsl.localhost\Ubuntu\home\someone\.claude\consignes", folder);
    }

    /// <summary>Without a settings file the Consignes app reads its folder every two seconds.</summary>
    [Fact]
    public void Polls_the_consignes_every_two_seconds_by_default()
    {
        AppSettings settings = new();

        Assert.Equal(2, settings.ConsignesPollSeconds);
    }

    // category N/A: error, malicious, fringe — pure string mapping of values the user sets in their own settings file.
}
