using System;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Core.Application;

/// <summary>
/// Sets how many lines a wheel notch scrolls in the terminals. The terminal turns a wheel delta into
/// lines with the Windows setting, one line per <c>120 / systemLines</c> of delta; the service
/// rescales the delta so that a notch gives the chosen number of lines instead. The choice is saved.
/// </summary>
public sealed class WheelScrollService
{
    /// <summary>Wheel delta of one notch, as Windows reports it.</summary>
    public const int NotchDelta = 120;

    /// <summary>Most lines a notch may scroll.</summary>
    public const int MaxLines = 100;

    private readonly IPreferencesStore _store;

    /// <summary>Creates the service with the saved choice.</summary>
    /// <param name="store">Keeps the choice among the view preferences.</param>
    public WheelScrollService(IPreferencesStore store)
    {
        _store = store;
        LinesPerNotch = Valid(store.Load().WheelLines);
    }

    /// <summary>Lines scrolled per notch, or null to follow the Windows setting.</summary>
    public int? LinesPerNotch { get; private set; }

    /// <summary>Chooses the lines scrolled per notch and saves the choice.</summary>
    /// <param name="lines">Lines per notch, from 1 to <see cref="MaxLines"/>, or null to follow Windows.</param>
    /// <returns>True when the choice changed.</returns>
    public bool SetLinesPerNotch(int? lines)
    {
        int? next = Valid(lines);
        if (next == LinesPerNotch)
        {
            return false;
        }
        LinesPerNotch = next;
        _store.Save(_store.Load() with { WheelLines = next });
        return true;
    }

    /// <summary>Rescales a wheel delta so that the terminal scrolls the chosen lines per notch.</summary>
    /// <param name="delta">The delta Windows reported.</param>
    /// <param name="systemLines">The Windows setting, in lines per notch; 0 or less means pages or nothing.</param>
    /// <returns>The delta to hand to the terminal, or null to leave the message as it is.</returns>
    public int? Rescale(int delta, int systemLines)
    {
        if (LinesPerNotch is not int lines || systemLines <= 0 || systemLines > NotchDelta)
        {
            return null;
        }
        long scaled = (long)delta * lines * (NotchDelta / systemLines) / NotchDelta;
        return (int)Math.Clamp(scaled, short.MinValue, short.MaxValue);
    }

    private static int? Valid(int? lines)
    {
        return lines is >= 1 and <= MaxLines ? lines : null;
    }
}
