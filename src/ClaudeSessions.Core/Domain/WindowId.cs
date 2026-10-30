using System;

namespace ClaudeSessions.Core.Domain;

/// <summary>Identifier of a window, a UUID written in its lowercase hyphenated form.</summary>
/// <param name="Value">The UUID.</param>
public readonly record struct WindowId(Guid Value)
{
    /// <summary>Parses the hyphenated form of a window identifier.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="id">The identifier, when the text is a valid UUID in the "D" format.</param>
    /// <returns>True when the text is a valid identifier.</returns>
    public static bool TryParse(string? text, out WindowId id)
    {
        bool valid = Guid.TryParseExact(text, "D", out Guid value);
        id = new WindowId(value);
        return valid;
    }

    /// <summary>Returns the lowercase hyphenated form of the identifier.</summary>
    /// <returns>The identifier as text.</returns>
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
