using System;
using System.IO;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Where the application keeps its files on Windows.</summary>
internal static class AppPaths
{
    /// <summary>Folder of the application data, under %LOCALAPPDATA%.</summary>
    public static string DataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClaudeSessions");

    /// <summary>The saved workspace.</summary>
    public static string WorkspaceFile => Path.Combine(DataFolder, "workspace.json");

    /// <summary>Where the WSL hook writes the session of each tab and its approval requests.</summary>
    public static string SessionRecordsFolder => Path.Combine(DataFolder, "session-tabs");

    /// <summary>The settings.</summary>
    public static string SettingsFile => Path.Combine(DataFolder, "settings.json");

    /// <summary>The view preferences chosen from the keyboard or the menu: zoom, lines per wheel notch.</summary>
    public static string PreferencesFile => Path.Combine(DataFolder, "preferences.json");
}
