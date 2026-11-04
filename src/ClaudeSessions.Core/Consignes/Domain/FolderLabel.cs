namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>Names a session by its working directory, as ClaudeSessions does for a tab without a title.</summary>
public static class FolderLabel
{
    /// <summary>The last folder of a path.</summary>
    /// <param name="cwd">A working directory, with forward slashes.</param>
    /// <returns>Its last folder; "/" for the root or an empty path.</returns>
    public static string Of(string cwd)
    {
        string trimmed = cwd.TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        string name = slash < 0 ? trimmed : trimmed[(slash + 1)..];
        return name.Length == 0 ? "/" : name;
    }
}
