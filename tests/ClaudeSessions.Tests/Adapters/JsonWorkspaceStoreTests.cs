using System.IO;
using System.Linq;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Saving and loading the workspace file.</summary>
public sealed class JsonWorkspaceStoreTests
{
    /// <summary>A saved workspace loads back with its windows, tabs, bounds, selection and closed tabs.</summary>
    [Fact]
    public void Loads_back_what_it_saved()
    {
        using TempFolder folder = new();
        JsonWorkspaceStore store = new(folder.File("workspace.json"));
        SessionTab first = TestData.Tab("/home/joff/Sources/comutils", "Hook");
        SessionTab second = TestData.Tab("/home/joff/Sources/ck-widget");
        SessionWindow window = new(new WindowId(System.Guid.NewGuid()), WindowStatus.Closed, new WindowBounds(10, 20, 900, 600, true), [first, second], second.Id);
        ClosedTab closed = new(TestData.Tab("/tmp"), window.Id, TestData.Now);
        store.Save(new Workspace([window], [closed]));

        Workspace loaded = store.Load();

        SessionWindow loadedWindow = Assert.Single(loaded.Windows);
        Assert.Equal(window.Id, loadedWindow.Id);
        Assert.Equal(WindowStatus.Closed, loadedWindow.Status);
        Assert.Equal(window.Bounds, loadedWindow.Bounds);
        Assert.Equal(second.Id, loadedWindow.SelectedTabId);
        Assert.Equal([(first.Id, first.Cwd, first.SessionId, first.Title), (second.Id, second.Cwd, second.SessionId, second.Title)], loadedWindow.Tabs.Select(tab => (tab.Id, tab.Cwd, tab.SessionId, tab.Title)));
        Assert.Equal((closed.Tab.Id, closed.WindowId, closed.ClosedAt), (loaded.ClosedTabs[0].Tab.Id, loaded.ClosedTabs[0].WindowId, loaded.ClosedTabs[0].ClosedAt));
    }

    /// <summary>No file means an empty workspace.</summary>
    [Fact]
    public void Loads_an_empty_workspace_when_no_file_exists()
    {
        using TempFolder folder = new();
        JsonWorkspaceStore store = new(folder.File("workspace.json"));

        Workspace loaded = store.Load();

        Assert.Empty(loaded.Windows);
    }

    /// <summary>A file that is not JSON is set aside as .bad and an empty workspace is used.</summary>
    [Fact]
    public void Sets_aside_a_corrupted_file()
    {
        using TempFolder folder = new();
        string path = folder.File("workspace.json");
        File.WriteAllText(path, "{ not json");
        JsonWorkspaceStore store = new(path);

        Workspace loaded = store.Load();

        Assert.Empty(loaded.Windows);
        Assert.False(File.Exists(path));
        Assert.Equal("{ not json", File.ReadAllText(path + JsonWorkspaceStore.BadFileSuffix));
    }

    /// <summary>A file of another format version is set aside rather than misread.</summary>
    [Fact]
    public void Sets_aside_a_file_of_another_version()
    {
        using TempFolder folder = new();
        string path = folder.File("workspace.json");
        File.WriteAllText(path, """{ "version": 2, "windows": [], "closedTabs": [] }""");
        JsonWorkspaceStore store = new(path);

        Workspace loaded = store.Load();

        Assert.Empty(loaded.Windows);
        Assert.True(File.Exists(path + JsonWorkspaceStore.BadFileSuffix));
    }

    /// <summary>A tab whose session id is not a UUID makes the whole file invalid.</summary>
    [Fact]
    public void Sets_aside_a_file_with_an_invalid_session_id()
    {
        using TempFolder folder = new();
        string path = folder.File("workspace.json");
        File.WriteAllText(path, WorkspaceWithTab("; rm -rf ~", "/home/joff"));
        JsonWorkspaceStore store = new(path);

        Workspace loaded = store.Load();

        Assert.Empty(loaded.Windows);
        Assert.True(File.Exists(path + JsonWorkspaceStore.BadFileSuffix));
    }

    /// <summary>A tab whose directory is not an absolute WSL path makes the file invalid.</summary>
    [Fact]
    public void Sets_aside_a_file_with_a_relative_directory()
    {
        using TempFolder folder = new();
        string path = folder.File("workspace.json");
        File.WriteAllText(path, WorkspaceWithTab("0f8fad5b-d9cb-469f-a165-70867728950e", "C:\\Windows"));
        JsonWorkspaceStore store = new(path);

        Workspace loaded = store.Load();

        Assert.Empty(loaded.Windows);
    }

    /// <summary>A JSON null document counts as unreadable.</summary>
    [Fact]
    public void Sets_aside_a_null_document()
    {
        using TempFolder folder = new();
        string path = folder.File("workspace.json");
        File.WriteAllText(path, "null");
        JsonWorkspaceStore store = new(path);

        Workspace loaded = store.Load();

        Assert.Empty(loaded.Windows);
        Assert.True(File.Exists(path + JsonWorkspaceStore.BadFileSuffix));
    }

    /// <summary>The first save creates the missing folder and leaves no temporary file behind.</summary>
    [Fact]
    public void Creates_the_folder_and_leaves_no_temporary_file()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "nested", "workspace.json");
        JsonWorkspaceStore store = new(path);

        store.Save(Workspace.Empty());

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    /// <summary>A second save replaces the first one.</summary>
    [Fact]
    public void Replaces_the_previous_save()
    {
        using TempFolder folder = new();
        JsonWorkspaceStore store = new(folder.File("workspace.json"));
        store.Save(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab())], []));

        store.Save(Workspace.Empty());

        Assert.Empty(store.Load().Windows);
    }

    /// <summary>Builds a version 1 document holding one window with one tab.</summary>
    /// <param name="sessionId">Session id written for the tab.</param>
    /// <param name="cwd">Directory written for the tab.</param>
    /// <returns>The JSON text.</returns>
    private static string WorkspaceWithTab(string sessionId, string cwd)
    {
        return $$"""
            { "version": 1, "closedTabs": [], "windows": [ {
              "id": "6f9619ff-8b86-d011-b42d-00cf4fc964ff", "status": "Open",
              "bounds": { "left": 0, "top": 0, "width": 800, "height": 600, "maximized": false },
              "tabs": [ { "id": "1b4e28ba-2fa1-11d2-883f-0016d3cca427", "cwd": {{System.Text.Json.JsonSerializer.Serialize(cwd)}}, "sessionId": {{System.Text.Json.JsonSerializer.Serialize(sessionId)}} } ] } ] }
            """;
    }
}
