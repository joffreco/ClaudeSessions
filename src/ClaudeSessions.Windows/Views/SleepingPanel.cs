using System.Windows;
using System.Windows.Controls;

namespace ClaudeSessions.Windows.Views;

/// <summary>What a sleeping tab shows in place of its terminal: a short note and a button that wakes it.</summary>
internal sealed class SleepingPanel : StackPanel
{
    /// <summary>Creates the panel.</summary>
    /// <param name="wake">Wakes the tab: launches its session again, resumed.</param>
    public SleepingPanel(System.Action wake)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Children.Add(new TextBlock
        {
            Text = "Session en veille : l'onglet était inactif, sa mémoire est libérée.",
            Foreground = ChromePalette.Text,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12),
        });
        Button button = new() { Content = "Réveiller", Padding = new Thickness(16, 4, 16, 4), HorizontalAlignment = HorizontalAlignment.Center };
        button.Click += (_, _) => wake();
        Children.Add(button);
    }
}
