using System.IO;
using ClaudeSessions.Adapters;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Reading settings.json.</summary>
public sealed class AppSettingsFileTests
{
    /// <summary>Values in the file override the defaults; missing ones keep them.</summary>
    [Fact]
    public void Reads_the_values_of_the_file()
    {
        using TempFolder folder = new();
        string path = folder.File("settings.json");
        File.WriteAllText(path, """{ "distro": "Ubuntu", "fontSize": 13 }""");

        AppSettings settings = AppSettingsFile.Load(path, "Debian");

        Assert.Equal("Ubuntu", settings.Distro);
        Assert.Equal(13, settings.FontSize);
        Assert.Equal("/home/joff/Sources", settings.DefaultCwd);
    }

    /// <summary>A file that names no distribution uses the default distribution of WSL.</summary>
    [Fact]
    public void Uses_the_default_distribution_when_the_file_names_none()
    {
        using TempFolder folder = new();
        string path = folder.File("settings.json");
        File.WriteAllText(path, """{ "distro": "", "fontSize": 13 }""");

        AppSettings settings = AppSettingsFile.Load(path, "Ubuntu-24.04");

        Assert.Equal("Ubuntu-24.04", settings.Distro);
        Assert.Equal(13, settings.FontSize);
    }

    /// <summary>A missing file gives the defaults and is written without a distribution, so it follows the WSL default.</summary>
    [Fact]
    public void Writes_the_defaults_when_the_file_is_missing()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "nested", "settings.json");

        AppSettings settings = AppSettingsFile.Load(path, "Ubuntu-24.04");

        Assert.Equal(new AppSettings { Distro = "Ubuntu-24.04" }, settings);
        Assert.Equal(new AppSettings(), AppSettingsFile.Load(path, null));
    }

    /// <summary>With no distribution in the file and none known to WSL, the distribution stays empty.</summary>
    [Fact]
    public void Leaves_the_distribution_empty_when_no_default_is_known()
    {
        using TempFolder folder = new();
        string path = folder.File("settings.json");
        File.WriteAllText(path, """{ "fontSize": 13 }""");

        AppSettings settings = AppSettingsFile.Load(path, null);

        Assert.Equal("", settings.Distro);
    }

    /// <summary>An unreadable file gives the defaults and is left untouched.</summary>
    [Fact]
    public void Falls_back_to_the_defaults_for_an_unreadable_file()
    {
        using TempFolder folder = new();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ broken");

        AppSettings settings = AppSettingsFile.Load(path, null);

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal("{ broken", File.ReadAllText(path));
    }

    // category N/A: malicious — the settings file belongs to the user who runs the application.
}
