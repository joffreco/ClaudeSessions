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

    // category N/A: error, malicious, fringe — pure string mapping of values the user sets in their own settings file.
}
