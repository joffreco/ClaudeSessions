using System.Linq;

namespace ClaudeSessions.Adapters;

/// <summary>The environment variables that tell the WSL side where the app expects the hook to write.</summary>
public static class WslEnvironment
{
    /// <summary>Variable holding the folder of the hook records.</summary>
    public const string RecordsVariable = "CLAUDE_SESSIONS_RECORDS";

    /// <summary>Variable listing what Windows passes on to WSL.</summary>
    public const string ForwardVariable = "WSLENV";

    private const string ForwardEntry = RecordsVariable + "/p";

    /// <summary>Adds the records folder to the variables passed to WSL, as a path WSL can use.</summary>
    /// <param name="current">Current value of WSLENV; null or empty when unset.</param>
    /// <returns>The new value, keeping the other entries; unchanged when the entry is already there.</returns>
    public static string ForwardRecords(string? current)
    {
        if (string.IsNullOrEmpty(current))
        {
            return ForwardEntry;
        }
        return current.Split(':').Contains(ForwardEntry) ? current : current + ":" + ForwardEntry;
    }
}
