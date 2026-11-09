using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClaudeSessions.Windows.Views;

/// <summary>Asks for the WSL directory of a new tab, offering recent and suggested directories.</summary>
internal sealed class FolderPrompt : Window
{
    private readonly ComboBox _choices;

    private FolderPrompt(IEnumerable<string> choices, string initial)
    {
        Title = "Nouvel onglet dans…";
        AppTheme.DressDialog(this);
        Width = 560;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        _choices = new ComboBox { IsEditable = true, ItemsSource = choices, Text = initial, Margin = new Thickness(12) };
        Button open = new() { Content = "Ouvrir", IsDefault = true, Width = 90, Margin = new Thickness(0, 0, 8, 0) };
        Button cancel = new() { Content = "Annuler", IsCancel = true, Width = 90 };
        open.Click += (_, _) => DialogResult = true;
        StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12, 0, 12, 12) };
        buttons.Children.Add(open);
        buttons.Children.Add(cancel);
        StackPanel layout = new();
        layout.Children.Add(new TextBlock { Text = "Dossier WSL de la session :", Margin = new Thickness(12, 12, 12, 0) });
        layout.Children.Add(_choices);
        layout.Children.Add(buttons);
        Content = layout;
        Loaded += (_, _) => Keyboard.Focus(_choices);
    }

    /// <summary>Shows the prompt.</summary>
    /// <param name="owner">Window the prompt belongs to.</param>
    /// <param name="choices">Directories offered in the list.</param>
    /// <param name="initial">Directory shown at first.</param>
    /// <returns>The absolute WSL directory typed or chosen, or null when cancelled or not absolute.</returns>
    public static string? Ask(Window owner, IEnumerable<string> choices, string initial)
    {
        FolderPrompt prompt = new(choices, initial) { Owner = owner };
        if (prompt.ShowDialog() != true)
        {
            return null;
        }
        string chosen = prompt._choices.Text.Trim();
        return chosen.StartsWith('/') ? chosen : null;
    }
}
