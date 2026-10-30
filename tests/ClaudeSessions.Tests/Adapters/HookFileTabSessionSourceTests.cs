using System;
using System.IO;
using System.Threading.Tasks;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Reading the records claude-sessions-hook.sh writes for each tab.</summary>
public sealed class HookFileTabSessionSourceTests
{
    private static readonly TabId Tab = new(Guid.Parse("11111111-2222-3333-4444-555555555555"));
    private static readonly SessionId Session = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"));

    /// <summary>A record written by the hook gives the current session of the tab.</summary>
    [Fact]
    public async Task Reads_the_session_recorded_by_the_hook()
    {
        using TempFolder folder = new();
        WriteRecord(folder, Tab, $$"""{"tab_id":"{{Tab}}","session_id":"{{Session}}","cwd":"/home/joff/Sources","transcript_path":"/x.jsonl"}""");
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Equal(Session, read);
    }

    /// <summary>No record means no session.</summary>
    [Fact]
    public async Task Reads_nothing_when_no_record_exists()
    {
        using TempFolder folder = new();
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Null(read);
    }

    /// <summary>An unreachable folder means no session, not a failure.</summary>
    [Fact]
    public async Task Reads_nothing_when_the_folder_is_missing()
    {
        HookFileTabSessionSource source = new(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N")));

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Null(read);
    }

    /// <summary>A malformed record is ignored.</summary>
    [Fact]
    public async Task Ignores_a_malformed_record()
    {
        using TempFolder folder = new();
        WriteRecord(folder, Tab, "{\"tab_id\":");
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Null(read);
    }

    /// <summary>A record naming another tab is ignored.</summary>
    [Fact]
    public async Task Ignores_a_record_of_another_tab()
    {
        using TempFolder folder = new();
        WriteRecord(folder, Tab, $$"""{"tab_id":"99999999-2222-3333-4444-555555555555","session_id":"{{Session}}"}""");
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Null(read);
    }

    /// <summary>A session id that is not a UUID is ignored, so it never reaches a command line.</summary>
    [Fact]
    public async Task Ignores_a_session_id_that_is_not_a_uuid()
    {
        using TempFolder folder = new();
        WriteRecord(folder, Tab, $$"""{"tab_id":"{{Tab}}","session_id":"x; rm -rf ~"}""");
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Null(read);
    }

    /// <summary>An oversized file is not read as a record.</summary>
    [Fact]
    public async Task Ignores_an_oversized_file()
    {
        using TempFolder folder = new();
        string padding = new(' ', (int)HookFileTabSessionSource.MaxRecordBytes);
        WriteRecord(folder, Tab, $$"""{"tab_id":"{{Tab}}","session_id":"{{Session}}"}""" + padding);
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Null(read);
    }

    /// <summary>Extra fields written by a later hook version do not break the read.</summary>
    [Fact]
    public async Task Ignores_unknown_fields()
    {
        using TempFolder folder = new();
        WriteRecord(folder, Tab, $$"""{"tab_id":"{{Tab}}","session_id":"{{Session}}","source":"clear","extra":[1,2]}""");
        HookFileTabSessionSource source = new(folder.Path);

        SessionId? read = await source.ReadAsync(Tab);

        Assert.Equal(Session, read);
    }

    /// <summary>Forgetting a tab deletes its record.</summary>
    [Fact]
    public void Deletes_the_record_of_a_forgotten_tab()
    {
        using TempFolder folder = new();
        string path = WriteRecord(folder, Tab, "{}");
        HookFileTabSessionSource source = new(folder.Path);

        source.Forget(Tab);

        Assert.False(File.Exists(path));
    }

    /// <summary>Forgetting a tab without a record does nothing.</summary>
    [Fact]
    public void Forgets_a_tab_without_record_silently()
    {
        HookFileTabSessionSource source = new(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N")));

        Exception? thrown = Record.Exception(() => source.Forget(Tab));

        Assert.Null(thrown);
    }

    /// <summary>Writes the record file of a tab.</summary>
    /// <param name="folder">The records folder.</param>
    /// <param name="tabId">The tab.</param>
    /// <param name="content">The file content.</param>
    /// <returns>The path of the file.</returns>
    private static string WriteRecord(TempFolder folder, TabId tabId, string content)
    {
        string path = folder.File(tabId + ".json");
        File.WriteAllText(path, content);
        return path;
    }
}
