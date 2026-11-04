namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>Identifies a consigne: the folder of its session, relative to the consignes root, and its id.</summary>
/// <param name="Folder">The session's folder, a tab id or <c>_sans-onglet/&lt;folder&gt;</c>, with forward slashes.</param>
/// <param name="Id">The consigne's id, unique in its folder.</param>
public readonly record struct ConsigneKey(string Folder, string Id)
{
    /// <summary>Gets the key as one text, used to store what the user did with the consigne.</summary>
    public string Text => Folder + "/" + Id;
}
