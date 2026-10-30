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

        AppSettings settings = AppSettingsFile.Load(path);

        Assert.Equal("Ubuntu", settings.Distro);
        Assert.Equal(13, settings.FontSize);
        Assert.Equal("/home/joff/Sources", settings.DefaultCwd);
    }

    /// <summary>A missing file gives the defaults and is written, so it can be edited.</summary>
    [Fact]
    public void Writes_the_defaults_when_the_file_is_missing()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "nested", "settings.json");

        AppSettings settings = AppSettingsFile.Load(path);

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal(new AppSettings(), AppSettingsFile.Load(path));
    }

    /// <summary>An unreadable file gives the defaults and is left untouched.</summary>
    [Fact]
    public void Falls_back_to_the_defaults_for_an_unreadable_file()
    {
        using TempFolder folder = new();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ broken");

        AppSettings settings = AppSettingsFile.Load(path);

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal("{ broken", File.ReadAllText(path));
    }

    // category N/A: malicious — the settings file belongs to the user who runs the application.
}
