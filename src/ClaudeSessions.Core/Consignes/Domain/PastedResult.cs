using System;

namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>A result the user pasted in a consigne's result field and sent to the session.</summary>
/// <param name="Name">The field's name.</param>
/// <param name="Text">What the user pasted.</param>
/// <param name="At">When it was sent.</param>
public sealed record PastedResult(string Name, string Text, DateTimeOffset At);
