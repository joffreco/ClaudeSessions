using ClaudeSessions.Adapters.Consignes.Markdown;

namespace ClaudeSessions.Consignes.Interfaces;

/// <summary>What the user can do in the consigne shown: tick its tasks and send results from its fields.</summary>
public interface IConsigneActions
{
    /// <summary>Tells whether a task is ticked.</summary>
    /// <param name="index">The task's position.</param>
    /// <returns>True when ticked.</returns>
    bool IsTicked(int index);

    /// <summary>Ticks or unticks a task.</summary>
    /// <param name="item">The task.</param>
    /// <param name="ticked">Its new state.</param>
    /// <returns>False when it failed and the box goes back.</returns>
    bool Tick(ListItemBlock item, bool ticked);

    /// <summary>The result last sent from a field.</summary>
    /// <param name="name">The field's name.</param>
    /// <returns>The text sent; empty when none was.</returns>
    string ResultOf(string name);

    /// <summary>Sends the session what the user pasted in a field.</summary>
    /// <param name="name">The field's name.</param>
    /// <param name="text">What the user pasted.</param>
    /// <returns>True when sent.</returns>
    bool SendResult(string name, string text);
}
