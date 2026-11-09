using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using ClaudeSessions.Adapters.Consignes;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Consignes.Fakes;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Adapters;

/// <summary>Reading and archiving the consignes of the consignes folder.</summary>
public sealed class FolderConsigneSourceTests
{
    /// <summary>Tab folders and folders outside a tab are read, archives and temporary files are not.</summary>
    [Fact]
    public void ReadsSessionFoldersOnly()
    {
        using TempFolder root = new();
        root.Write("tab-a/one.md", ConsigneFiles.Of("un"));
        root.Write("_sans-onglet/comunikator/two.md", ConsigneFiles.Of("deux"));
        root.Write("tab-a/archive/old.md", ConsigneFiles.Of("vieux"));
        root.Write("tab-a/.tmp.abc", ConsigneFiles.Of("temporaire"));
        root.Write("tab-a/notes.txt", "autre");

        IReadOnlyList<Consigne> consignes = new FolderConsigneSource(root.Path).ReadAll();

        Assert.Equal(["_sans-onglet/comunikator/two", "tab-a/one"], consignes.Select(consigne => consigne.Key.Text).Order());
    }

    /// <summary>A missing root gives no consigne.</summary>
    [Fact]
    public void MissingRootIsEmpty()
    {
        Assert.Empty(new FolderConsigneSource(Path.Combine(Path.GetTempPath(), "consignes-absent-xyz")).ReadAll());
    }

    /// <summary>A file rewritten by the script is read again.</summary>
    [Fact]
    public void ReadsAgainAChangedFile()
    {
        using TempFolder root = new();
        string file = root.Write("tab-a/one.md", ConsigneFiles.Of("un"));
        FolderConsigneSource source = new(root.Path);
        source.ReadAll();

        File.WriteAllText(file, ConsigneFiles.Of("un modifié", "Étape 2"));
        File.SetLastWriteTimeUtc(file, File.GetLastWriteTimeUtc(file).AddSeconds(5));

        Assert.Equal("un modifié", Assert.Single(source.ReadAll()).Title);
    }

    /// <summary>Archiving moves the file to the folder's archive, where it is no longer read.</summary>
    [Fact]
    public void ArchiveMovesTheFile()
    {
        using TempFolder root = new();
        root.Write("tab-a/one.md", ConsigneFiles.Of("un"));
        FolderConsigneSource source = new(root.Path);

        source.Archive(new ConsigneKey("tab-a", "one"));

        Assert.True(File.Exists(Path.Combine(root.Path, "tab-a", "archive", "one.md")));
        Assert.Empty(source.ReadAll());
    }

    /// <summary>Archiving also moves the ticks sent to the session and its cursor, so that a new consigne with the same id starts clean.</summary>
    [Fact]
    public void ArchiveMovesTheEventsFiles()
    {
        using TempFolder root = new();
        root.Write("tab-a/one.md", ConsigneFiles.Of("un"));
        root.Write("tab-a/one.events.jsonl", "{}\n");
        root.Write("tab-a/one.events.seen", "1");

        new FolderConsigneSource(root.Path).Archive(new ConsigneKey("tab-a", "one"));

        Assert.Equal(["one.events.jsonl", "one.events.seen", "one.md"], Directory.GetFiles(Path.Combine(root.Path, "tab-a", "archive")).Select(Path.GetFileName).Order());
        Assert.Empty(Directory.GetFiles(Path.Combine(root.Path, "tab-a")));
    }

    /// <summary>Each tick sent adds one complete JSON line next to the consigne, which is not read as a consigne.</summary>
    [Fact]
    public void SendTickAddsOneLinePerTick()
    {
        using TempFolder root = new();
        root.Write("tab-a/one.md", ConsigneFiles.Of("un"));
        FolderConsigneSource source = new(root.Path);
        DateTimeOffset at = new(2026, 11, 4, 11, 0, 0, TimeSpan.FromHours(-5));

        source.SendTick(new ConsigneKey("tab-a", "one"), new StepTick(0, "Lancer « la » migration", at));
        source.SendTick(new ConsigneKey("tab-a", "one"), new StepTick(2, "Vérifier", at));

        string content = File.ReadAllText(Path.Combine(root.Path, "tab-a", "one.events.jsonl"));
        string[] lines = content.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("", lines[2]);
        using JsonDocument first = JsonDocument.Parse(lines[0]);
        Assert.Equal((0, "Lancer « la » migration"), (first.RootElement.GetProperty("index").GetInt32(), first.RootElement.GetProperty("text").GetString()));
        Assert.Single(source.ReadAll());
    }

    /// <summary>A sent result adds one JSON line, of kind result, after the ticks, with its text whole.</summary>
    [Fact]
    public void SendResultAddsAResultLine()
    {
        using TempFolder root = new();
        root.Write("tab-a/one.md", ConsigneFiles.Of("un"));
        FolderConsigneSource source = new(root.Path);
        DateTimeOffset at = new(2026, 11, 4, 11, 0, 0, TimeSpan.FromHours(-5));

        source.SendTick(new ConsigneKey("tab-a", "one"), new StepTick(0, "Lancer", at));
        source.SendResult(new ConsigneKey("tab-a", "one"), new PastedResult("comptes", "id | n\n1 | 42", at));

        string[] lines = File.ReadAllText(Path.Combine(root.Path, "tab-a", "one.events.jsonl")).Split('\n');
        using JsonDocument result = JsonDocument.Parse(lines[1]);
        Assert.Equal(("result", "comptes", "id | n\n1 | 42"), (result.RootElement.GetProperty("kind").GetString(), result.RootElement.GetProperty("name").GetString(), result.RootElement.GetProperty("text").GetString()));
    }

    /// <summary>A folder outside a tab maps to its path under _sans-onglet.</summary>
    [Fact]
    public void PathOfOutsideTabFolder()
    {
        Assert.Equal(Path.Combine("/r", "_sans-onglet", "x"), new FolderConsigneSource("/r").PathOf("_sans-onglet/x"));
    }
}
