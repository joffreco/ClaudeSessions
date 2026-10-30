using System;
using ClaudeSessions.Core.Interfaces;

namespace ClaudeSessions.Core.Application;

/// <summary>
/// Zooms the terminals one point at a time, as Windows Terminal does, between <see cref="MinSize"/>
/// and <see cref="MaxSize"/>. A zoomed size is saved; going back to the configured size forgets it.
/// </summary>
public sealed class ZoomService
{
    /// <summary>Smallest font size, in points.</summary>
    public const short MinSize = 6;

    /// <summary>Largest font size, in points.</summary>
    public const short MaxSize = 72;

    private readonly IPreferencesStore _store;
    private readonly short _configuredSize;

    /// <summary>Creates the service at the saved size, else at the configured one.</summary>
    /// <param name="store">Keeps the zoomed size among the view preferences.</param>
    /// <param name="configuredSize">Font size of the settings, in points.</param>
    public ZoomService(IPreferencesStore store, short configuredSize)
    {
        _store = store;
        _configuredSize = Clamp(configuredSize);
        Size = store.Load().FontSize is short saved ? Clamp(saved) : _configuredSize;
    }

    /// <summary>Current font size of the terminals, in points.</summary>
    public short Size { get; private set; }

    /// <summary>Makes the font one point larger.</summary>
    /// <returns>True when the size changed.</returns>
    public bool ZoomIn()
    {
        return SetSize(Size + 1);
    }

    /// <summary>Makes the font one point smaller.</summary>
    /// <returns>True when the size changed.</returns>
    public bool ZoomOut()
    {
        return SetSize(Size - 1);
    }

    /// <summary>Goes back to the configured size.</summary>
    /// <returns>True when the size changed.</returns>
    public bool Reset()
    {
        return SetSize(_configuredSize);
    }

    private bool SetSize(int size)
    {
        short next = Clamp(size);
        if (next == Size)
        {
            return false;
        }
        Size = next;
        _store.Save(_store.Load() with { FontSize = next == _configuredSize ? null : next });
        return true;
    }

    private static short Clamp(int size)
    {
        return (short)Math.Clamp(size, MinSize, MaxSize);
    }
}
