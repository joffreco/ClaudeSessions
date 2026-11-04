using System;
using System.IO;
using System.Reflection;

namespace ClaudeSessions.Consignes.Hosting;

/// <summary>
/// <c>Consignes.exe --check-install</c>: loads every managed assembly of the installation folder, so that
/// install.ps1 learns before switching over whether Windows (Smart App Control) blocks one of them.
/// </summary>
internal static class InstallCheck
{
    /// <summary>The argument that runs the check instead of the app.</summary>
    public const string Argument = "--check-install";

    /// <summary>Loads each assembly of the folder.</summary>
    /// <returns>0 when all load; 1 when one is blocked or unreadable.</returns>
    public static int Run()
    {
        foreach (string file in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            try
            {
                Assembly.LoadFrom(file);
            }
            catch (BadImageFormatException)
            {
            }
            catch (Exception exception) when (exception is FileLoadException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"{Path.GetFileName(file)} : {exception.Message}");
                return 1;
            }
        }
        return 0;
    }
}
