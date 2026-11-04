using System.Collections.Generic;
using System.IO;
using ClaudeSessions.Adapters.Consignes;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Adapters;

/// <summary>Reading the tabs from ClaudeSessions' workspace.json.</summary>
public sealed class WorkspaceTabDirectoryTests
{
    /// <summary>Open tabs come from the windows, closed ones from closedTabs[].tab; null titles stay null.</summary>
    [Fact]
    public void ReadsOpenAndClosedTabs()
    {
        using TempFolder folder = new();
        string file = folder.Write("workspace.json", """
            {"version":1,
             "windows":[{"tabs":[{"id":"a","title":"682 runner1"},{"id":"b","title":null},{"title":"sans id"}]}],
             "closedTabs":[{"tab":{"id":"c","title":"fermé"},"windowId":"w","closedAt":"2026-11-04T00:00:00Z"},{"tab":{"id":"a","title":"ancien"}}]}
            """);

        IReadOnlyDictionary<string, TabInfo>? tabs = new WorkspaceTabDirectory(file).Read();

        Assert.NotNull(tabs);
        Assert.Equal(new TabInfo("682 runner1", true), tabs["a"]);
        Assert.Equal(new TabInfo(null, true), tabs["b"]);
        Assert.Equal(new TabInfo("fermé", false), tabs["c"]);
        Assert.Equal(3, tabs.Count);
    }

    /// <summary>A missing or broken file gives null, so no tab is taken for closed.</summary>
    /// <param name="content">The file's content; null for no file.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("{pas du json")]
    public void UnreadableFileIsNull(string? content)
    {
        using TempFolder folder = new();
        string file = Path.Combine(folder.Path, "workspace.json");
        if (content is not null)
        {
            File.WriteAllText(file, content);
        }

        Assert.Null(new WorkspaceTabDirectory(file).Read());
    }

    /// <summary>An unexpected shape gives no tab rather than an error.</summary>
    [Fact]
    public void UnknownShapeIsEmpty()
    {
        using TempFolder folder = new();
        string file = folder.Write("workspace.json", """{"windows":"autre","closedTabs":[1,"x"]}""");

        Assert.Empty(new WorkspaceTabDirectory(file).Read()!);
    }
}
