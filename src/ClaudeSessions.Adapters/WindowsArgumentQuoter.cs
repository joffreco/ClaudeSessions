using System.Text;

namespace ClaudeSessions.Adapters;

/// <summary>Quotes arguments for a Windows command line, following the CommandLineToArgvW rules.</summary>
public sealed class WindowsArgumentQuoter
{
    /// <summary>Quotes one argument so that the receiving process gets it back unchanged.</summary>
    /// <param name="argument">The argument.</param>
    /// <returns>The argument, quoted when it is empty or holds a space, a tab or a double quote.</returns>
    public static string Quote(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"', '\n', '\v']) < 0)
        {
            return argument;
        }
        StringBuilder quoted = new("\"");
        int backslashes = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }
            int repeat = character == '"' ? backslashes * 2 + 1 : backslashes;
            quoted.Append('\\', repeat).Append(character);
            backslashes = 0;
        }
        quoted.Append('\\', backslashes * 2).Append('"');
        return quoted.ToString();
    }
}
