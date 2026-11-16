using ClaudeSessions.Core.Transcripts.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>Which slash commands open a menu that only the terminal shows.</summary>
public sealed class SlashCommandsTests
{
    /// <summary>A command opens a menu always, only without arguments, or never.</summary>
    /// <param name="message">The message sent.</param>
    /// <param name="opens">Whether it opens a menu.</param>
    [Theory]
    [InlineData("/rewind", true)]
    [InlineData("/usage", true)]
    [InlineData("/config", true)]
    [InlineData("  /resume  ", true)]
    [InlineData("/model", true)]
    [InlineData("/model sonnet", false)]
    [InlineData("/compact", false)]
    [InlineData("/ecole-publish ajoute la page", false)]
    [InlineData("rewind", false)]
    [InlineData("/rewind\nautre ligne", false)]
    public void Tells_which_commands_open_a_menu(string message, bool opens)
    {
        Assert.Equal(opens, SlashCommands.OpensMenu(message));
    }
}
