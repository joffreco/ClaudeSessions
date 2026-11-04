using System;

namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>Instructions a Claude session handed to the user to run, as deposited by the consigne script.</summary>
/// <param name="Key">Where it is stored.</param>
/// <param name="Title">Its title.</param>
/// <param name="Status">Whether it is ready or still under review.</param>
/// <param name="TabId">The ClaudeSessions tab of the session; empty outside a tab.</param>
/// <param name="TabTitle">The tab's title when the consigne was deposited; empty when unknown.</param>
/// <param name="SessionId">The Claude session that deposited it; empty when unknown.</param>
/// <param name="Cwd">The session's working directory.</param>
/// <param name="Created">When it was first deposited.</param>
/// <param name="Updated">When it was last deposited.</param>
/// <param name="Body">The instructions, in Markdown.</param>
public sealed record Consigne(
    ConsigneKey Key,
    string Title,
    ConsigneStatus Status,
    string TabId,
    string TabTitle,
    string SessionId,
    string Cwd,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    string Body);
