using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClaudeSessions.Adapters.Dto;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Adapters;

/// <summary>Converts the workspace to and from its serialized document.</summary>
public sealed class WorkspaceDocumentMapper
{
    /// <summary>Converts a workspace to its document.</summary>
    /// <param name="workspace">The workspace.</param>
    /// <returns>The document, at the current format version.</returns>
    public static WorkspaceDocument ToDocument(Workspace workspace)
    {
        return new WorkspaceDocument
        {
            Version = WorkspaceDocument.CurrentVersion,
            Windows = workspace.Windows.Select(ToDocument).ToList(),
            ClosedTabs = workspace.ClosedTabs.Select(ToDocument).ToList(),
        };
    }

    /// <summary>Converts a document to a workspace.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The workspace.</returns>
    /// <exception cref="InvalidDataException">The document has another version or an invalid value.</exception>
    public static Workspace ToWorkspace(WorkspaceDocument document)
    {
        if (document.Version != WorkspaceDocument.CurrentVersion)
        {
            throw new InvalidDataException($"Unsupported workspace version {document.Version}.");
        }
        return new Workspace(document.Windows.Select(ToWindow), document.ClosedTabs.Select(ToClosedTab));
    }

    private static WindowDocument ToDocument(SessionWindow window)
    {
        return new WindowDocument
        {
            Id = window.Id.ToString(),
            Status = window.Status.ToString(),
            Bounds = new BoundsDocument
            {
                Left = window.Bounds.Left,
                Top = window.Bounds.Top,
                Width = window.Bounds.Width,
                Height = window.Bounds.Height,
                Maximized = window.Bounds.Maximized,
            },
            SelectedTabId = window.SelectedTabId?.ToString(),
            Tabs = window.Tabs.Select(ToDocument).ToList(),
        };
    }

    private static TabDocument ToDocument(SessionTab tab)
    {
        return new TabDocument
        {
            Id = tab.Id.ToString(),
            Cwd = tab.Cwd,
            SessionId = tab.SessionId.ToString(),
            Title = tab.Title,
        };
    }

    private static ClosedTabDocument ToDocument(ClosedTab closedTab)
    {
        return new ClosedTabDocument
        {
            Tab = ToDocument(closedTab.Tab),
            WindowId = closedTab.WindowId.ToString(),
            ClosedAt = closedTab.ClosedAt,
        };
    }

    private static SessionWindow ToWindow(WindowDocument document)
    {
        if (!Enum.TryParse(document.Status, false, out WindowStatus status) || !Enum.IsDefined(status))
        {
            throw new InvalidDataException($"Invalid window status '{document.Status}'.");
        }
        BoundsDocument bounds = document.Bounds;
        List<SessionTab> tabs = document.Tabs.Select(ToTab).ToList();
        TabId? selected = TabId.TryParse(document.SelectedTabId, out TabId selectedId) ? selectedId : null;
        return new SessionWindow(
            RequireWindowId(document.Id),
            status,
            new WindowBounds(bounds.Left, bounds.Top, bounds.Width, bounds.Height, bounds.Maximized),
            tabs,
            selected);
    }

    private static SessionTab ToTab(TabDocument document)
    {
        if (!TabId.TryParse(document.Id, out TabId tabId))
        {
            throw new InvalidDataException($"Invalid tab id '{document.Id}'.");
        }
        if (!SessionId.TryParse(document.SessionId, out SessionId sessionId))
        {
            throw new InvalidDataException($"Invalid session id '{document.SessionId}'.");
        }
        if (!document.Cwd.StartsWith('/'))
        {
            throw new InvalidDataException($"Invalid working directory '{document.Cwd}'.");
        }
        return new SessionTab(tabId, document.Cwd, sessionId, document.Title is null ? null : SessionTitle.Parse(document.Title).Text);
    }

    private static ClosedTab ToClosedTab(ClosedTabDocument document)
    {
        return new ClosedTab(ToTab(document.Tab), RequireWindowId(document.WindowId), document.ClosedAt);
    }

    private static WindowId RequireWindowId(string text)
    {
        return WindowId.TryParse(text, out WindowId windowId)
            ? windowId
            : throw new InvalidDataException($"Invalid window id '{text}'.");
    }
}
