using System;
using System.IO;

namespace ClaudeSessions.Adapters;

/// <summary>Where the application keeps its files on Windows.</summary>
public static class AppPaths
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

    /// <summary>What the user read and ticked in the Consignes app.</summary>
    public static string ConsignesStateFile => Path.Combine(DataFolder, "consignes-state.json");

    /// <summary>The Consignes app, installed next to ClaudeSessions.</summary>
    public static string ConsignesExecutable => Path.Combine(AppContext.BaseDirectory, "Consignes.exe");
}
