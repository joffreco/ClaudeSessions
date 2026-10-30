using ClaudeSessions.Adapters;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Quoting of command-line arguments under the CommandLineToArgvW rules.</summary>
public sealed class WindowsArgumentQuoterTests
{
    /// <summary>A plain argument is left as is.</summary>
    [Fact]
    public void Leaves_a_plain_argument_alone()
    {
        string quoted = WindowsArgumentQuoter.Quote("/home/joff/Sources");

        Assert.Equal("/home/joff/Sources", quoted);
    }

    /// <summary>An argument with a space is wrapped in quotes.</summary>
    [Fact]
    public void Quotes_an_argument_with_a_space()
    {
        string quoted = WindowsArgumentQuoter.Quote("/home/joff/My Projects");

        Assert.Equal("\"/home/joff/My Projects\"", quoted);
    }

    /// <summary>An empty argument becomes a pair of quotes, so it is not lost.</summary>
    [Fact]
    public void Quotes_an_empty_argument()
    {
        string quoted = WindowsArgumentQuoter.Quote("");

        Assert.Equal("\"\"", quoted);
    }

    /// <summary>An embedded double quote is escaped, so it cannot end the argument early.</summary>
    [Fact]
    public void Escapes_an_embedded_quote()
    {
        string quoted = WindowsArgumentQuoter.Quote("a\" -e evil");

        Assert.Equal("\"a\\\" -e evil\"", quoted);
    }

    /// <summary>Backslashes before a quote are doubled; elsewhere they stay single.</summary>
    [Theory]
    [InlineData("C:\\dir \\\"x", "\"C:\\dir \\\\\\\"x\"")]
    [InlineData("C:\\dir with space\\", "\"C:\\dir with space\\\\\"")]
    [InlineData("C:\\plain\\", "C:\\plain\\")]
    public void Doubles_backslashes_only_before_a_quote(string argument, string expected)
    {
        string quoted = WindowsArgumentQuoter.Quote(argument);

        Assert.Equal(expected, quoted);
    }

    /// <summary>Quotes and apostrophes in a directory name survive.</summary>
    [Fact]
    public void Keeps_apostrophes_unquoted_when_there_is_no_space()
    {
        string quoted = WindowsArgumentQuoter.Quote("/home/joff/l'été");

        Assert.Equal("/home/joff/l'été", quoted);
    }
}
