using System.IO;
using ClaudeSessions.Adapters;
using ClaudeSessions.Adapters.Dto;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Conversion between the workspace and its document.</summary>
public sealed class WorkspaceDocumentMapperTests
{
    /// <summary>The document is written at the current format version.</summary>
    [Fact]
    public void Writes_the_current_version()
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(Workspace.Empty());

        Assert.Equal(WorkspaceDocument.CurrentVersion, document.Version);
    }

    /// <summary>A title saved with a state mark comes back without it, since the state is stale once read.</summary>
    [Fact]
    public void Drops_the_state_mark_of_a_saved_title()
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab(title: "◐ Fix the hook"))], []));

        Workspace workspace = WorkspaceDocumentMapper.ToWorkspace(document);

        Assert.Equal("Fix the hook", workspace.Windows[0].Tabs[0].Title);
    }

    /// <summary>Bounds kept in device pixels come back in device pixels, with their values.</summary>
    [Fact]
    public void Keeps_bounds_saved_in_device_pixels()
    {
        SessionWindow window = TestData.Window(WindowStatus.Open, TestData.Tab());
        window.Bounds = new WindowBounds(-3840, 961, 1536, 900, true, BoundsUnit.DevicePixels);
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([window], []));

        Workspace workspace = WorkspaceDocumentMapper.ToWorkspace(document);

        Assert.Equal(new WindowBounds(-3840, 961, 1536, 900, true, BoundsUnit.DevicePixels), workspace.Windows[0].Bounds);
    }

    /// <summary>Bounds saved before the unit existed are read as device-independent pixels, as they were written.</summary>
    [Fact]
    public void Reads_bounds_without_a_unit_as_device_independent()
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab())], []));
        document.Windows[0] = document.Windows[0] with { Bounds = new BoundsDocument { Left = -2783.2, Top = 969.6, Width = 768, Height = 825.6 } };

        Workspace workspace = WorkspaceDocumentMapper.ToWorkspace(document);

        Assert.Equal(new WindowBounds(-2783.2, 969.6, 768, 825.6, false, BoundsUnit.DeviceIndependent), workspace.Windows[0].Bounds);
    }

    /// <summary>An unknown unit, or a number outside the enumeration, falls back to device-independent pixels instead of refusing the workspace.</summary>
    /// <param name="unit">The saved unit.</param>
    [Theory]
    [InlineData("Inches")]
    [InlineData("devicepixels")]
    [InlineData("7")]
    [InlineData("")]
    public void Reads_an_unknown_unit_as_device_independent(string unit)
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab())], []));
        document.Windows[0] = document.Windows[0] with { Bounds = document.Windows[0].Bounds with { Unit = unit } };

        Workspace workspace = WorkspaceDocumentMapper.ToWorkspace(document);

        Assert.Equal(BoundsUnit.DeviceIndependent, workspace.Windows[0].Bounds.Unit);
    }

    /// <summary>A window with an unknown status is refused.</summary>
    [Fact]
    public void Refuses_an_unknown_window_status()
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab())], []));
        document.Windows[0] = document.Windows[0] with { Status = "Minimized" };

        Assert.Throws<InvalidDataException>(() => WorkspaceDocumentMapper.ToWorkspace(document));
    }

    /// <summary>A numeric status outside the enumeration is refused, not cast blindly.</summary>
    [Fact]
    public void Refuses_a_numeric_status_outside_the_enumeration()
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab())], []));
        document.Windows[0] = document.Windows[0] with { Status = "7" };

        Assert.Throws<InvalidDataException>(() => WorkspaceDocumentMapper.ToWorkspace(document));
    }

    /// <summary>A window id that is not a UUID is refused.</summary>
    [Fact]
    public void Refuses_an_invalid_window_id()
    {
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, TestData.Tab())], []));
        document.Windows[0] = document.Windows[0] with { Id = "window-1" };

        Assert.Throws<InvalidDataException>(() => WorkspaceDocumentMapper.ToWorkspace(document));
    }

    /// <summary>An invalid selected tab id falls back to the first tab instead of failing.</summary>
    [Fact]
    public void Falls_back_to_the_first_tab_for_an_invalid_selection()
    {
        SessionTab tab = TestData.Tab();
        WorkspaceDocument document = WorkspaceDocumentMapper.ToDocument(new Workspace([TestData.Window(WindowStatus.Open, tab)], []));
        document.Windows[0] = document.Windows[0] with { SelectedTabId = "garbage" };

        Workspace workspace = WorkspaceDocumentMapper.ToWorkspace(document);

        Assert.Equal(tab.Id, workspace.Windows[0].SelectedTabId);
    }
}
