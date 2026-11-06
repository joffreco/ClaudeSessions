using System;
using System.Globalization;
using System.IO;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>
/// Temporary diagnostic of the taskbar flashing: appends what happens to flash-trace.log in the data
/// folder. To remove once the flashing behaves.
/// </summary>
internal static class FlashTrace
{
    /// <summary>Appends one line, with the time.</summary>
    /// <param name="text">What happened.</param>
    public static void Write(string text)
    {
        try
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + text + Environment.NewLine;
            File.AppendAllText(Path.Combine(AppPaths.DataFolder, "flash-trace.log"), line);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
