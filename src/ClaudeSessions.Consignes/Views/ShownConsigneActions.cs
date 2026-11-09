using System.Windows;
using ClaudeSessions.Adapters.Consignes.Markdown;
using ClaudeSessions.Consignes.Hosting;
using ClaudeSessions.Consignes.Interfaces;
using ClaudeSessions.Core.Consignes.Domain;

namespace ClaudeSessions.Consignes.Views;

/// <summary>The actions on the consigne the window shows, each failure told to the user in a message.</summary>
/// <param name="controller">What keeps the reading state and reaches the session.</param>
/// <param name="key">The consigne shown.</param>
/// <param name="owner">The window that owns the messages.</param>
internal sealed class ShownConsigneActions(BoardController controller, ConsigneKey key, Window owner) : IConsigneActions
{
    private const string Title = "Consignes";

    /// <inheritdoc/>
    public bool IsTicked(int index)
    {
        return controller.IsTicked(key, index);
    }

    /// <inheritdoc/>
    public bool Tick(ListItemBlock item, bool ticked)
    {
        if (item.TaskIndex is not int index)
        {
            return false;
        }
        return Succeeded(controller.Tick(key, index, ticked, item.Callback ? item.Text : null), "La session n'a pas pu être prévenue : la case reste décochée.");
    }

    /// <inheritdoc/>
    public string ResultOf(string name)
    {
        return controller.ResultOf(key, name);
    }

    /// <inheritdoc/>
    public bool SendResult(string name, string text)
    {
        return Succeeded(controller.SendResult(key, name, text), "Le résultat n'a pas pu être envoyé à la session.");
    }

    private bool Succeeded(string? failure, string message)
    {
        if (failure is null)
        {
            return true;
        }
        MessageBox.Show(owner, message + "\n\n" + failure, Title, MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
    }
}
