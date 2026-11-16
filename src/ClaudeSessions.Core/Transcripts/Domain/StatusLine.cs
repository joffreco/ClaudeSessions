using System.Collections.Generic;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>The status line Claude Code last showed under a session's input, with its colors.</summary>
/// <param name="Rows">Its lines, from top to bottom.</param>
public sealed record StatusLine(IReadOnlyList<StatusRow> Rows);

/// <summary>One line of a status line.</summary>
/// <param name="Segments">Its parts as the status line command separated them with a vertical bar.</param>
public sealed record StatusRow(IReadOnlyList<StatusSegment> Segments);

/// <summary>One part of a status line between two vertical bars.</summary>
/// <param name="Parts">Its pieces of text and its percentages, in order.</param>
public sealed record StatusSegment(IReadOnlyList<StatusPart> Parts);

/// <summary>A piece of a status line segment.</summary>
/// <param name="Color">Its color, or null for the default one.</param>
public abstract record StatusPart(StatusColor? Color);

/// <summary>Text of a status line.</summary>
/// <param name="Text">The text.</param>
/// <param name="Color">Its color, or null for the default one.</param>
/// <param name="Dim">True when the command asked for faint text.</param>
public sealed record StatusText(string Text, StatusColor? Color, bool Dim) : StatusPart(Color);

/// <summary>A percentage of a status line, shown as a gauge.</summary>
/// <param name="Percent">The value, from 0 to 100.</param>
/// <param name="Text">The text it was written as, such as <c>45%</c> or <c>(78%)</c>.</param>
/// <param name="Color">Its color, or null for the default one.</param>
/// <param name="Labeled">True when a word precedes it in its segment, such as <c>Smart 7%</c>: the main gauge of that segment.</param>
public sealed record StatusGauge(int Percent, string Text, StatusColor? Color, bool Labeled) : StatusPart(Color);

/// <summary>A color of a status line.</summary>
/// <param name="Red">Red, from 0 to 255.</param>
/// <param name="Green">Green, from 0 to 255.</param>
/// <param name="Blue">Blue, from 0 to 255.</param>
public readonly record struct StatusColor(byte Red, byte Green, byte Blue);
