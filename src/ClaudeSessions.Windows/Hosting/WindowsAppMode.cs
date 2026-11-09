using Microsoft.Win32;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Reads the app mode chosen in Windows (Personalization › Colors): light or dark.</summary>
internal static class WindowsAppMode
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string LightAppsValue = "AppsUseLightTheme";

    /// <summary>Tells whether Windows asks apps for their dark mode; a missing value means light, the Windows default.</summary>
    /// <returns>True when the app mode is dark.</returns>
    public static bool IsDark()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue(LightAppsValue) is int light && light == 0;
    }
}
