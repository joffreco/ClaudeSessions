using ClaudeSessions.Core.Consignes.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Core;

/// <summary>Naming a session by its working directory.</summary>
public sealed class FolderLabelTests
{
    /// <summary>The last folder, as ClaudeSessions shows it; "/" for the root.</summary>
    /// <param name="cwd">The working directory.</param>
    /// <param name="expected">The label.</param>
    [Theory]
    [InlineData("/home/joff/Sources/comunikator", "comunikator")]
    [InlineData("/home/joff/Sources/comunikator/", "comunikator")]
    [InlineData("/", "/")]
    [InlineData("", "/")]
    public void LastFolder(string cwd, string expected)
    {
        Assert.Equal(expected, FolderLabel.Of(cwd));
    }
}
