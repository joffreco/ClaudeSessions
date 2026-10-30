using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Reads the default WSL distribution from the registry key where WSL lists the distributions of the user.</summary>
internal static class WslDefaultDistribution
{
    private const string LxssKey = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    /// <summary>Reads the name of the default distribution.</summary>
    /// <returns>Its name, such as Ubuntu-24.04; null when WSL has none or the key cannot be read.</returns>
    public static string? Read()
    {
        try
        {
            using RegistryKey? lxss = Registry.CurrentUser.OpenSubKey(LxssKey);
            if (lxss?.GetValue("DefaultDistribution") is not string id)
            {
                return null;
            }
            using RegistryKey? distribution = lxss.OpenSubKey(id);
            return distribution?.GetValue("DistributionName") is string { Length: > 0 } name ? name : null;
        }
        catch (Exception e) when (e is SecurityException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
