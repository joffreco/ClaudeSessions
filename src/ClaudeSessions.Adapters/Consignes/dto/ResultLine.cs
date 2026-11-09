using System;

namespace ClaudeSessions.Adapters.Consignes.Dto;

/// <summary>A line of a consigne's events file carrying a result the user pasted, read by <c>consigne wait</c>.</summary>
/// <param name="Kind">Always <see cref="ResultKind"/>, which tells it from a ticked step.</param>
/// <param name="Name">The result field's name.</param>
/// <param name="Text">What the user pasted.</param>
/// <param name="At">When it was sent.</param>
public sealed record ResultLine(string Kind, string Name, string Text, DateTimeOffset At)
{
    /// <summary>The <see cref="Kind"/> of every result line.</summary>
    public const string ResultKind = "result";
}
