using System;

namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>The user ticked a step a session marked for a callback.</summary>
/// <param name="Index">The step's position among the consigne's tasks, from 0.</param>
/// <param name="Text">The step's text, so that the session recognises it even if the steps moved.</param>
/// <param name="At">When it was ticked.</param>
public sealed record StepTick(int Index, string Text, DateTimeOffset At);
