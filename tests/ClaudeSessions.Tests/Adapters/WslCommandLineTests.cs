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
        WslCommandLine builder = new(new AppSettings());

        string commandLine = builder.Build(new TabLaunchRequest(Tab, Session, "/home/joff/Sources"));

        Assert.Equal("wsl.exe -d Debian --cd /home/joff/Sources -e /home/joff/.local/bin/claude-sessions-tab 11111111-2222-3333-4444-555555555555 aaaaaaaa-0000-0000-0000-000000000001", commandLine);
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

    // category N/A: error — building the command line has no failure path; the ids are validated types.
}
