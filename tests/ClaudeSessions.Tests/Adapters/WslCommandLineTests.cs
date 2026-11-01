using System;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Application.Dto;
using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>The command line that runs a tab's launcher in WSL.</summary>
public sealed class WslCommandLineTests
{
    private static readonly TabId Tab = new(Guid.Parse("11111111-2222-3333-4444-555555555555"));
    private static readonly SessionId Session = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));

    /// <summary>The distribution, directory, launcher and both ids appear in order.</summary>
    [Fact]
    public void Builds_the_launcher_command_line()
    {
        WslCommandLine builder = new(new AppSettings { Distro = "Debian" });

        string commandLine = builder.Build(new TabLaunchRequest(Tab, Session, "/home/joff/Sources"));

        Assert.Equal("wsl.exe -d Debian --cd /home/joff/Sources -e /home/joff/.local/bin/claude-sessions-tab 11111111-2222-3333-4444-555555555555 aaaaaaaa-0000-0000-0000-000000000001", commandLine);
    }

    /// <summary>Without a distribution, wsl.exe gets no -d and runs its default distribution.</summary>
    [Fact]
    public void Leaves_out_the_distribution_when_none_is_set()
    {
        WslCommandLine builder = new(new AppSettings());

        string commandLine = builder.Build(new TabLaunchRequest(Tab, Session, "/"));

        Assert.StartsWith("wsl.exe --cd / -e ", commandLine, StringComparison.Ordinal);
    }

    /// <summary>A directory with spaces and an apostrophe stays one argument.</summary>
    [Fact]
    public void Quotes_a_directory_with_spaces()
    {
        WslCommandLine builder = new(new AppSettings());

        string commandLine = builder.Build(new TabLaunchRequest(Tab, Session, "/home/joff/l'été 2026"));

        Assert.Contains("--cd \"/home/joff/l'été 2026\" -e", commandLine, StringComparison.Ordinal);
    }

    /// <summary>A directory trying to inject options stays inside its quotes.</summary>
    [Fact]
    public void Keeps_an_injection_attempt_inside_one_argument()
    {
        WslCommandLine builder = new(new AppSettings());

        string commandLine = builder.Build(new TabLaunchRequest(Tab, Session, "/tmp\" -e rm -rf ~ \""));

        Assert.Contains("--cd \"/tmp\\\" -e rm -rf ~ \\\"\" -e", commandLine, StringComparison.Ordinal);
    }

    /// <summary>The settings choose the executable, distribution and launcher.</summary>
    [Fact]
    public void Uses_the_configured_distribution_and_launcher()
    {
        WslCommandLine builder = new(new AppSettings { WslExecutable = "C:\\Windows\\System32\\wsl.exe", Distro = "Ubuntu", LauncherPath = "/opt/tab" });

        string commandLine = builder.Build(new TabLaunchRequest(Tab, Session, "/"));

        Assert.StartsWith("C:\\Windows\\System32\\wsl.exe -d Ubuntu --cd / -e /opt/tab ", commandLine, StringComparison.Ordinal);
    }

    private static readonly InstallSource Repository = new("Ubuntu-24.04", "/home/joff/Projets/ClaudeSessions");
    private const string Installed = "8a668b19fdbf795c1a680501f17d2a21f0ea2220";

    /// <summary>The check pulls without prompting in the repository and lists the commits since the installed one.</summary>
    [Fact]
    public void Builds_the_update_check_arguments()
    {
        string[] arguments = WslCommandLine.UpdateCheckArguments(Repository, Installed);

        Assert.Equal(["-d", "Ubuntu-24.04", "--cd", "/home/joff/Projets/ClaudeSessions", "-e", "bash", "-lc"], arguments[..7]);
        Assert.StartsWith("GIT_TERMINAL_PROMPT=0 git pull --ff-only ", arguments[7], StringComparison.Ordinal);
        Assert.Equal(["check", Installed], arguments[8..]);
    }

    /// <summary>An unknown or malformed installed commit never reaches the shell.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("HEAD; rm -rf ~")]
    public void Passes_no_unknown_commit_to_the_shell(string? installed)
    {
        string[] arguments = WslCommandLine.UpdateCheckArguments(Repository, installed);

        Assert.Equal("unknown", arguments[^1]);
    }

    /// <summary>The publish runs publish.sh from a login shell in the repository.</summary>
    [Fact]
    public void Builds_the_publish_arguments()
    {
        Assert.Equal(["-d", "Ubuntu-24.04", "--cd", "/home/joff/Projets/ClaudeSessions", "-e", "bash", "-lc", "./publish.sh"], WslCommandLine.PublishArguments(Repository));
    }

    // category N/A: error — building the command line has no failure path; the ids are validated types.
}
