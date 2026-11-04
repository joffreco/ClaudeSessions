using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;

namespace ClaudeSessions.Adapters.Consignes;

/// <summary>
/// Reads the tabs from ClaudeSessions' workspace.json, read only and shared, since ClaudeSessions rewrites it:
/// <c>windows[].tabs[]</c> are open, <c>closedTabs[].tab</c> closed. Unknown or missing fields are skipped.
/// </summary>
public sealed class WorkspaceTabDirectory : ITabDirectory
{
    private readonly string _path;

    /// <summary>Creates the directory.</summary>
    /// <param name="path">The path of workspace.json.</param>
    public WorkspaceTabDirectory(string path)
    {
        _path = path;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, TabInfo>? Read()
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

    /// <summary>Reads the tabs of a workspace document.</summary>
    /// <param name="root">The document's root.</param>
    /// <returns>The tabs by id; an open tab wins over a closed one with the same id.</returns>
    public static Dictionary<string, TabInfo> TabsOf(JsonElement root)
    {
        Dictionary<string, TabInfo> tabs = new(StringComparer.Ordinal);
        foreach (JsonElement closed in ArrayOf(root, "closedTabs"))
        {
            if (closed.ValueKind == JsonValueKind.Object && closed.TryGetProperty("tab", out JsonElement tab))
            {
                Add(tabs, tab, isOpen: false);
            }
        }
        foreach (JsonElement window in ArrayOf(root, "windows"))
        {
            foreach (JsonElement tab in ArrayOf(window, "tabs"))
            {
                Add(tabs, tab, isOpen: true);
            }
        }
        return tabs;
    }

    private static void Add(Dictionary<string, TabInfo> tabs, JsonElement tab, bool isOpen)
    {
        if (tab.ValueKind != JsonValueKind.Object || StringOf(tab, "id") is not string id)
        {
            return;
        }
        tabs[id] = new TabInfo(StringOf(tab, "title"), isOpen);
    }

    private static JsonElement[] ArrayOf(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement array) && array.ValueKind == JsonValueKind.Array
            ? [.. array.EnumerateArray()]
            : [];
    }

    private static string? StringOf(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
