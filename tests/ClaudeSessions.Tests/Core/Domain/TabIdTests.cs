using System;
using ClaudeSessions.Core.Domain;
using Xunit;

namespace ClaudeSessions.Tests.Core.Domain;

/// <summary>Parsing and formatting of tab identifiers (WindowId and SessionId share the same code).</summary>
public sealed class TabIdTests
{
    /// <summary>A hyphenated UUID parses and prints back the same.</summary>
    [Fact]
    public void Parses_and_prints_a_hyphenated_uuid()
    {
        const string text = "0f8fad5b-d9cb-469f-a165-70867728950e";

        bool parsed = TabId.TryParse(text, out TabId id);

        Assert.True(parsed);
        Assert.Equal(text, id.ToString());
    }

    /// <summary>Text that is not a UUID is refused.</summary>
    [Fact]
    public void Refuses_text_that_is_not_a_uuid()
    {
        bool parsed = TabId.TryParse("not-a-uuid", out TabId _);

        Assert.False(parsed);
    }

    /// <summary>Null and empty text are refused.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Refuses_null_or_empty_text(string? text)
    {
        bool parsed = TabId.TryParse(text, out TabId _);

        Assert.False(parsed);
    }

    /// <summary>A path traversal attempt is refused, so it can never become a file name.</summary>
    [Fact]
    public void Refuses_a_path_traversal()
    {
        bool parsed = TabId.TryParse("../../etc/passwd", out TabId _);

        Assert.False(parsed);
    }

    /// <summary>Only the hyphenated form is accepted: braces or bare digits are refused.</summary>
    [Theory]
    [InlineData("{0f8fad5b-d9cb-469f-a165-70867728950e}")]
    [InlineData("0f8fad5bd9cb469fa16570867728950e")]
    public void Refuses_other_uuid_formats(string text)
    {
        bool parsed = TabId.TryParse(text, out TabId _);

        Assert.False(parsed);
    }

    /// <summary>An uppercase UUID parses and prints in lowercase, the form the hook writes.</summary>
    [Fact]
    public void Prints_an_uppercase_uuid_in_lowercase()
    {
        bool parsed = TabId.TryParse("0F8FAD5B-D9CB-469F-A165-70867728950E", out TabId id);

        string printed = id.ToString();

        Assert.True(parsed);
        Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e", printed);
    }

    /// <summary>Two ids holding the same UUID are equal.</summary>
    [Fact]
    public void Ids_with_the_same_uuid_are_equal()
    {
        Guid value = Guid.NewGuid();

        bool equal = new TabId(value) == new TabId(value);

        Assert.True(equal);
    }
}
