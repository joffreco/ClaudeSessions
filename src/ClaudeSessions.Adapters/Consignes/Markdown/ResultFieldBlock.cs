namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>
/// A field where the user pastes a result the session asked for, such as the rows of a SQL query, and sends it
/// to the session; written <c>&lt;!-- result: &lt;name&gt; --&gt;</c> on its own line.
/// </summary>
/// <param name="Name">The result's name, which the session reads back; <c>resultat-&lt;n&gt;</c> when the mark names none.</param>
public sealed record ResultFieldBlock(string Name) : MarkdownBlock;
