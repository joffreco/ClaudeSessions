using System;
using ClaudeSessions.Adapters.Consignes;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Consignes.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Adapters;

/// <summary>Reading a consigne file written by the consigne script.</summary>
public sealed class ConsigneFileParserTests
{
    private static readonly ConsigneKey Key = new("tab", "export");

    /// <summary>Every header field and the body are read.</summary>
    [Fact]
    public void ReadsHeaderAndBody()
    {
        Consigne? consigne = ConsigneFileParser.Parse(ConsigneFiles.Of("Tester l'export", "Étape 1\n\nÉtape 2", tabId: "t1", status: "en-revision"), Key);

        Assert.NotNull(consigne);
        Assert.Equal(("Tester l'export", ConsigneStatus.InReview, "t1", "682 runner1", "sess", "/home/joff/Sources/comunikator"),
            (consigne.Title, consigne.Status, consigne.TabId, consigne.TabTitle, consigne.SessionId, consigne.Cwd));
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.FromHours(-5)), consigne.Created);
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(-5)), consigne.Updated);
        Assert.Equal("Étape 1\n\nÉtape 2", consigne.Body);
    }

    /// <summary>Header values are JSON strings, so quotes, colons and line breaks survive.</summary>
    [Fact]
    public void DecodesJsonEscapes()
    {
        Consigne? consigne = ConsigneFileParser.Parse(ConsigneFiles.Of("Titre \\\"piégé\\\": a\\nb"), Key);

        Assert.Equal("Titre \"piégé\": a\nb", consigne?.Title);
    }

    /// <summary>A body may hold its own <c>---</c> lines; only the first two close the header.</summary>
    [Fact]
    public void KeepsFencesOfTheBody()
    {
        Consigne? consigne = ConsigneFileParser.Parse(ConsigneFiles.Of("t", "avant\n---\naprès"), Key);

        Assert.Equal("avant\n---\naprès", consigne?.Body);
    }

    /// <summary>Any status other than en-revision is ready.</summary>
    [Fact]
    public void UnknownStatusIsReady()
    {
        Assert.Equal(ConsigneStatus.Ready, ConsigneFileParser.Parse(ConsigneFiles.Of("t", status: "autre"), Key)?.Status);
    }

    /// <summary>A file without a complete header, or without a title, is not a consigne.</summary>
    /// <param name="text">The file's content.</param>
    [Theory]
    [InlineData("pas d'en-tête")]
    [InlineData("---\ntitle: \"t\"\nsans fin")]
    [InlineData("---\nstatus: \"prete\"\n---\ncorps")]
    public void RefusesIncompleteFiles(string text)
    {
        Assert.Null(ConsigneFileParser.Parse(text, Key));
    }

    /// <summary>Windows line ends are read like Unix ones.</summary>
    [Fact]
    public void ReadsCrLf()
    {
        Assert.Equal("t", ConsigneFileParser.Parse(ConsigneFiles.Of("t").Replace("\n", "\r\n", StringComparison.Ordinal), Key)?.Title);
    }
}
