using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// Reads the tabs of the open windows from ClaudeSessions' workspace.json, read only and shared,
/// since ClaudeSessions rewrites it. Unlike <see cref="JsonWorkspaceStore"/>, it never sets a file
/// aside: an unreadable file is ClaudeSessions' to handle. Unknown or invalid entries are skipped.
/// </summary>
public sealed class WorkspaceConversationTabSource : IConversationTabSource
{
    private readonly string _path;

    /// <summary>Creates the source.</summary>
    /// <param name="path">The path of workspace.json.</param>
    public WorkspaceConversationTabSource(string path)
    {
        _path = path;
    }

    /// <inheritdoc />
    public IReadOnlyList<ConversationTab>? Read()
    {
        try
        {
            using FileStream stream = new(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using JsonDocument document = JsonDocument.Parse(stream);
            return TabsOf(document.RootElement);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static List<ConversationTab> TabsOf(JsonElement root)
    {
        List<ConversationTab> tabs = [];
        int window = 0;
        foreach (JsonElement windowElement in ArrayOf(root, "windows"))
        {
            if (!string.Equals(StringOf(windowElement, "status"), nameof(WindowStatus.Open), StringComparison.OrdinalIgnoreCase)
                || !WindowId.TryParse(StringOf(windowElement, "id"), out WindowId windowId))
            {
                continue;
            }
            window++;
            foreach (JsonElement tab in ArrayOf(windowElement, "tabs"))
            {
                if (TabId.TryParse(StringOf(tab, "id"), out TabId id) && SessionId.TryParse(StringOf(tab, "sessionId"), out SessionId session))
                {
                    string cwd = StringOf(tab, "cwd") ?? "";
                    SessionTab saved = new(id, cwd, session, StringOf(tab, "title"));
                    tabs.Add(new ConversationTab(id, saved.DisplayTitle, cwd, session, windowId, window));
                }
            }
        }
        return tabs;
    }

    private static JsonElement[] ArrayOf(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement array) && array.ValueKind == JsonValueKind.Array
            ? [.. array.EnumerateArray()]
            : [];
    }

    private static string? StringOf(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
