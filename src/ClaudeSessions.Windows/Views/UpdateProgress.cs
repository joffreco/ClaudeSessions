using System.Windows;
using System.Windows.Controls;

namespace ClaudeSessions.Windows.Views;

/// <summary>Small window shown while an update step runs in WSL; it cancels nothing when closed.</summary>
internal sealed class UpdateProgress : Window
{
    private UpdateProgress(string message)
    {
        Title = "Mise à jour";
        AppTheme.DressDialog(this);
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = new TextBlock
        {
            Text = message + "\n\nLes sessions continuent pendant ce temps.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16),
        };
    }

    /// <summary>Shows the window without blocking.</summary>
    /// <param name="owner">Window it belongs to.</param>
    /// <param name="message">What is being done.</param>
    /// <returns>The window, to close when the step ends.</returns>
    public static UpdateProgress Open(Window owner, string message)
    {
        UpdateProgress progress = new(message) { Owner = owner };
        progress.Show();
        return progress;
    }
}
