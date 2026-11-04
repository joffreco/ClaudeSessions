using System;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Adapters.Consignes.Dto;

/// <summary>A consigne file as last read, with what tells whether it changed since.</summary>
/// <param name="Written">The file's last write time when it was read.</param>
/// <param name="Length">The file's length when it was read.</param>
/// <param name="Consigne">What it held; null when it could not be parsed.</param>
public sealed record CachedConsigne(DateTime Written, long Length, Consigne? Consigne);
