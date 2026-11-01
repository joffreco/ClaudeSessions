using ClaudeSessions.Adapters;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Comparing the installed app with its repository after a pull.</summary>
public sealed class UpdateCheckTests
{
    private const string Head = "1111111111111111111111111111111111111111";
    private const string Installed = "8a668b19fdbf795c1a680501f17d2a21f0ea2220";

    /// <summary>The head commit comes first, then the subjects of the missing commits.</summary>
    [Fact]
    public void Reads_the_head_and_the_changes()
    {
        UpdateCheck? check = UpdateCheck.Parse($"{Head}\nfeat: update from the menu\r\nfix: repaint after RDP\n");

        Assert.NotNull(check);
        Assert.Equal(Head, check.Head);
        Assert.Equal(["feat: update from the menu", "fix: repaint after RDP"], check.Changes);
    }

    /// <summary>Output that does not start with a commit is not a check.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("fatal: not a git repository\n")]
    public void Rejects_output_without_a_commit(string output)
    {
        Assert.Null(UpdateCheck.Parse(output));
    }

    /// <summary>The app is current only when built from the head commit, whatever the case of the hash.</summary>
    [Fact]
    public void Compares_the_installed_commit_with_the_head()
    {
        UpdateCheck check = new(Installed, []);

        Assert.True(check.IsInstalled(Installed.ToUpperInvariant()));
        Assert.False(check.IsInstalled(Head));
        Assert.False(check.IsInstalled(null));
    }

    /// <summary>The commit is read from the informational version the SDK writes.</summary>
    [Theory]
    [InlineData("1.0.0+" + Installed, Installed)]
    [InlineData("1.0.0", null)]
    [InlineData("1.0.0+local", null)]
    [InlineData(null, null)]
    public void Reads_the_installed_commit(string? informationalVersion, string? expected)
    {
        Assert.Equal(expected, UpdateCheck.RevisionOf(informationalVersion));
    }
}
