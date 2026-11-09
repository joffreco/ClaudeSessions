using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Core.Application.Dto;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Windows.Views;

/// <summary>Lists the tabs of every window, those that expect the user first, and jumps to the one chosen.</summary>
internal sealed class TabSwitcher : Window
{
    private readonly List<TabSwitchEntry> _entries;
    private readonly TextBox _filter;
    private readonly ListBox _list;
    private TabId? _chosen;

    private TabSwitcher(List<TabSwitchEntry> entries)
    {
        _entries = entries;
        Title = "Aller à un onglet";
        Width = 680;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 520;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = ChromePalette.Chrome;
        _filter = new TextBox { Margin = new Thickness(8), Padding = new Thickness(4), Background = ChromePalette.Field, Foreground = ChromePalette.Text, CaretBrush = ChromePalette.Text };
        _list = new ListBox { Margin = new Thickness(8, 0, 8, 8), MaxHeight = 440, Background = ChromePalette.Field, Foreground = ChromePalette.Text, BorderThickness = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        DockPanel layout = new();
        DockPanel.SetDock(_filter, Dock.Top);
        layout.Children.Add(_filter);
        layout.Children.Add(_list);
        Content = layout;
        _filter.TextChanged += (_, _) => ShowMatches();
        PreviewKeyDown += OnKey;
        Loaded += (_, _) => Keyboard.Focus(_filter);
        ShowMatches();
    }

    /// <summary>Shows the switcher.</summary>
    /// <param name="owner">Window the switcher belongs to.</param>
    /// <param name="entries">The tabs, in the order to keep among equals.</param>
    /// <returns>The tab chosen, or null when cancelled.</returns>
    public static TabId? Ask(Window owner, List<TabSwitchEntry> entries)
    {
        TabSwitcher switcher = new(entries) { Owner = owner };
        switcher.ShowDialog();
        return switcher._chosen;
    }

    private void ShowMatches()
    {
        _list.Items.Clear();
        foreach (TabSwitchEntry entry in TabSwitchList.Arrange(_entries, _filter.Text))
        {
            _list.Items.Add(Row(entry));
        }
        _list.SelectedIndex = _list.Items.Count > 0 ? 0 : -1;
    }

    private ListBoxItem Row(TabSwitchEntry entry)
    {
        StackPanel texts = new();
        texts.Children.Add(new TextBlock { Text = entry.Title, Foreground = ChromePalette.Text, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        texts.Children.Add(new TextBlock { Text = entry.Cwd + AttentionNote(entry.Attention), Foreground = ChromePalette.Text, Opacity = 0.7, TextTrimming = TextTrimming.CharacterEllipsis });
        DockPanel row = new();
        TextBlock mark = new() { Text = "●", Foreground = AttentionBrush(entry.Attention), Margin = new Thickness(2, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(mark, Dock.Left);
        row.Children.Add(mark);
        row.Children.Add(texts);
        ListBoxItem item = new() { Content = row, Tag = entry.TabId, Foreground = ChromePalette.Text };
        item.MouseLeftButtonUp += (_, _) => Choose(entry.TabId);
        return item;
    }

    private static string AttentionNote(TabAttention attention)
    {
        return attention switch
        {
            TabAttention.Approval => " · attend ton approbation",
            TabAttention.Finished => " · a terminé",
            _ => "",
        };
    }

    private static SolidColorBrush AttentionBrush(TabAttention attention)
    {
        return attention switch
        {
            TabAttention.Approval => ChromePalette.ApprovalMark,
            TabAttention.Finished => ChromePalette.IdleMark,
            _ => Brushes.Transparent,
        };
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                if (_list.SelectedItem is ListBoxItem { Tag: TabId tabId })
                {
                    Choose(tabId);
                }
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            default:
                break;
        }
    }

    private void MoveSelection(int step)
    {
        int count = _list.Items.Count;
        if (count == 0)
        {
            return;
        }
        _list.SelectedIndex = (_list.SelectedIndex + step + count) % count;
        _list.ScrollIntoView(_list.SelectedItem);
    }

    private void Choose(TabId tabId)
    {
        _chosen = tabId;
        Close();
    }
}
