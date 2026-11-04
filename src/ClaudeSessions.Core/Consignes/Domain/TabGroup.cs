using System.Collections.Generic;

namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>The consignes of one session folder, as the list shows them.</summary>
/// <param name="Folder">The session folder.</param>
/// <param name="Label">The name shown for the group.</param>
/// <param name="IsClosed">True when the folder's tab is known to be closed.</param>
/// <param name="Consignes">Its consignes, most recently updated first.</param>
/// <param name="Unread">The consignes that changed since the user last opened them.</param>
public sealed record TabGroup(string Folder, string Label, bool IsClosed, IReadOnlyList<Consigne> Consignes, IReadOnlySet<ConsigneKey> Unread)
{
    /// <summary>Gets how many of the consignes are unread.</summary>
    public int UnreadCount => Unread.Count;
}
