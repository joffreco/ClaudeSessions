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

    /// <summary>The hook records live under the home directory of the WSL user.</summary>
    [Fact]
    public void Locates_the_hook_records_under_the_wsl_home()
    {
        AppSettings settings = new() { Distro = "Ubuntu", WslHome = "/home/someone/" };

        string folder = settings.SessionTabsDirectory;

        Assert.Equal(@"\\wsl.localhost\Ubuntu\home\someone\.claude\session-tabs", folder);
    }

    // category N/A: error, malicious, fringe — pure string mapping of values the user sets in their own settings file.
}
