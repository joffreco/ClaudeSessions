using System;
using System.Collections.Generic;
using System.Globalization;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Conversations.Views;

/// <summary>Words of the activity line and of the end of a turn, in French.</summary>
internal static class ActivityText
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-CA");

    /// <summary>Describes a turn under way, such as « Claude travaille · 33 s · ↓ 2,5 k jetons · Bash en cours ».</summary>
    /// <param name="progress">The turn.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The line.</returns>
    public static string Of(TurnProgress progress, DateTimeOffset now)
    {
        List<string> parts = ["Claude travaille"];
        if (progress.Started is DateTimeOffset started)
        {
            parts.Add(Duration(now - started));
        }
        if (progress.OutputTokens > 0)
        {
            parts.Add("↓ " + Tokens(progress.OutputTokens));
        }
        if (progress.RunningTool is string tool)
        {
            parts.Add(tool + " en cours");
        }
        return string.Join(" · ", parts);
    }

    /// <summary>Writes a duration the way the terminal does, in French: « 33 s », « 1 min 12 s », « 1 h 05 min ».</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The duration, written.</returns>
    public static string Duration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }
        if (duration.TotalHours >= 1)
        {
            return string.Create(French, $"{(int)duration.TotalHours} h {duration.Minutes:00} min");
        }
        if (duration.TotalMinutes >= 1)
        {
            return string.Create(French, $"{duration.Minutes} min {duration.Seconds} s");
        }
        return string.Create(French, $"{duration.Seconds} s");
    }

    private static string Tokens(int tokens)
    {
        return tokens < 1000
            ? string.Create(French, $"{tokens} jetons")
            : string.Create(French, $"{tokens / 1000.0:0.#} k jetons");
    }
}
