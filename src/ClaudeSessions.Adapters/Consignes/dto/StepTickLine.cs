using System;

namespace ClaudeSessions.Adapters.Consignes.Dto;

/// <summary>One line of a consigne's events file, read by <c>consigne wait</c>.</summary>
/// <param name="Index">The step's position among the consigne's tasks, from 0.</param>
/// <param name="Text">The step's text.</param>
/// <param name="At">When it was ticked.</param>
public sealed record StepTickLine(int Index, string Text, DateTimeOffset At);
