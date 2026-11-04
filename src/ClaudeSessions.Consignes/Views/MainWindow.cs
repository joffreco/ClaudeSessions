using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClaudeSessions.Adapters.Consignes.Markdown;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Consignes.Hosting;
using ClaudeSessions.Consignes.Interfaces;

namespace ClaudeSessions.Consignes.Views;

/// <summary>
/// The window: the sessions and their consignes on the left, the selected consigne on the right. The list is
/// rebuilt only when what it shows changed, keeping the selection and the folded groups.
/// </summary>
internal sealed class MainWindow : Window, IBoardView
{
    private const string AppTitle = "Consignes";
    private const string ClosedTabsKey = "#closed";
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/Consignes.ico");

    private readonly BoardController _controller;
    private readonly TreeView _tree = new() { BorderThickness = new Thickness(0) };
    private readonly ContentControl _detail = new();
    private readonly HashSet<string> _folded = [ClosedTabsKey];
    private string _shape = "";
    private ConsigneKey? _selected;
    private DateTimeOffset _shownUpdate;
    private bool _rebuilding;

    /// <summary>Creates the window.</summary>
    /// <param name="controller">What reads the consignes and keeps the reading state.</param>
    public MainWindow(BoardController controller)
    {
        _controller = controller;
        Title = AppTitle;
        Width = 1100;
        Height = 760;
        MinWidth = 640;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = BitmapFrame.Create(IconUri);
        Content = Layout();
        ShowEmpty("Lecture des consignes…");
        _tree.SelectedItemChanged += (_, _) => OnSelectionChanged();
    }

    /// <inheritdoc/>
    public void ShowGroups(IReadOnlyList<TabGroup> groups)
    {
        int unread = groups.Sum(group => group.UnreadCount);
        Title = unread > 0 ? $"{AppTitle} ({unread})" : AppTitle;
        string shape = ShapeOf(groups);
        if (shape != _shape)
        {
            _shape = shape;
            Rebuild(groups);
        }
        if (groups.Count == 0)
        {
            ShowEmpty("Aucune consigne.\n\nLes sessions Claude en déposent ici avec la commande consigne put.");
        }
        else if (_selected is null)
        {
            ShowEmpty("Choisissez une consigne à gauche.");
        }
    }

    private Grid Layout()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320), MinWidth = 200 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        DockPanel left = new() { Margin = new Thickness(8, 8, 0, 8) };
        Button menu = MenuButton();
        DockPanel.SetDock(menu, Dock.Top);
        left.Children.Add(menu);
        left.Children.Add(_tree);
        Grid.SetColumn(left, 0);
        GridSplitter splitter = new() { Width = 5, HorizontalAlignment = HorizontalAlignment.Stretch, Background = Brushes.Transparent };
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(_detail, 2);
        grid.Children.Add(left);
        grid.Children.Add(splitter);
        grid.Children.Add(_detail);
        return grid;
    }

    private Button MenuButton()
    {
        Button button = new() { Content = "☰", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(10, 4, 10, 4) };
        ContextMenu menu = new() { PlacementTarget = button, Placement = PlacementMode.Bottom };
        menu.Items.Add(MenuItem("Ouvrir le dossier des consignes", () => _controller.OpenFolder("")));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Quitter", Close));
        button.Click += (_, _) => menu.IsOpen = true;
        return button;
    }

    private static MenuItem MenuItem(string header, Action action)
    {
        MenuItem item = new() { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void Rebuild(IReadOnlyList<TabGroup> groups)
    {
        _rebuilding = true;
        try
        {
            _tree.Items.Clear();
            TreeViewItem? closed = null;
            foreach (TabGroup group in groups)
            {
                TreeViewItem node = GroupNode(group);
                if (group.IsClosed)
                {
                    closed ??= FoldableNode(ClosedTabsKey, Header("Onglets fermés", 0, secondary: true));
                    closed.Items.Add(node);
                }
                else
                {
                    _tree.Items.Add(node);
                }
            }
            if (closed is not null)
            {
                _tree.Items.Add(closed);
            }
        }
        finally
        {
            _rebuilding = false;
        }
        Reselect(groups);
    }

    private TreeViewItem GroupNode(TabGroup group)
    {
        TreeViewItem node = FoldableNode(group.Folder, Header(group.Label, group.UnreadCount, secondary: false));
        node.Tag = group;
        foreach (Consigne consigne in group.Consignes)
        {
            node.Items.Add(new TreeViewItem { Header = ConsigneHeader(consigne, group), Tag = consigne });
        }
        return node;
    }

    private TreeViewItem FoldableNode(string key, object header)
    {
        TreeViewItem node = new() { Header = header, IsExpanded = !_folded.Contains(key) };
        node.Expanded += (_, e) => { if (e.OriginalSource == node) { _folded.Remove(key); } };
        node.Collapsed += (_, e) => { if (e.OriginalSource == node) { _folded.Add(key); } };
        return node;
    }

    private static TextBlock Header(string label, int unread, bool secondary)
    {
        TextBlock text = new() { Text = unread > 0 ? $"{label}  ({unread})" : label, FontWeight = unread > 0 ? FontWeights.SemiBold : FontWeights.Normal };
        if (secondary)
        {
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        }
        return text;
    }

    private static StackPanel ConsigneHeader(Consigne consigne, TabGroup group)
    {
        bool unread = group.Unread.Contains(consigne.Key);
        StackPanel panel = new() { Orientation = Orientation.Horizontal };
        TextBlock dot = new() { Text = "●", FontSize = 9, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, Visibility = unread ? Visibility.Visible : Visibility.Hidden };
        dot.SetResourceReference(TextBlock.ForegroundProperty, "AccentFillColorDefaultBrush");
        panel.Children.Add(dot);
        panel.Children.Add(new TextBlock { Text = consigne.Title, FontWeight = unread ? FontWeights.SemiBold : FontWeights.Normal, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 210 });
        TextBlock time = new() { Text = "  " + TimeOf(consigne.Updated), VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        time.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        panel.Children.Add(time);
        if (consigne.Status == ConsigneStatus.InReview)
        {
            panel.Children.Add(Badge("en révision"));
        }
        return panel;
    }

    private static Border Badge(string text)
    {
        Border badge = new() { CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 0, 5, 0), Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        badge.SetResourceReference(Border.BackgroundProperty, "SystemFillColorCautionBackgroundBrush");
        badge.Child = new TextBlock { Text = text, FontSize = 11 };
        return badge;
    }

    private void Reselect(IReadOnlyList<TabGroup> groups)
    {
        if (_selected is not ConsigneKey key)
        {
            return;
        }
        TreeViewItem? node = LeavesOf(_tree.Items).FirstOrDefault(leaf => leaf.Tag is Consigne consigne && consigne.Key == key);
        if (node is null)
        {
            _selected = null;
            ShowEmpty(groups.Count == 0 ? "" : "Choisissez une consigne à gauche.");
            return;
        }
        _rebuilding = true;
        node.IsSelected = true;
        _rebuilding = false;
        if (node.Tag is Consigne current && current.Updated != _shownUpdate)
        {
            ShowConsigne(current, groups.First(group => group.Folder == current.Key.Folder));
        }
    }

    private static IEnumerable<TreeViewItem> LeavesOf(ItemCollection items)
    {
        foreach (TreeViewItem item in items.OfType<TreeViewItem>())
        {
            if (item.Tag is Consigne)
            {
                yield return item;
            }
            foreach (TreeViewItem leaf in LeavesOf(item.Items))
            {
                yield return leaf;
            }
        }
    }

    private void OnSelectionChanged()
    {
        if (_rebuilding || _tree.SelectedItem is not TreeViewItem { Tag: Consigne consigne } node)
        {
            return;
        }
        _selected = consigne.Key;
        TabGroup? group = (node.Parent as TreeViewItem)?.Tag as TabGroup;
        ShowConsigne(consigne, group);
    }

    private void ShowConsigne(Consigne consigne, TabGroup? group)
    {
        _shownUpdate = consigne.Updated;
        DockPanel panel = new() { Margin = new Thickness(0, 8, 8, 8) };
        Grid header = DetailHeader(consigne, group);
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);
        MarkdownDocument document = MarkdownReader.Read(consigne.Body);
        FlowDocumentScrollViewer viewer = new()
        {
            Document = ConsigneDocumentBuilder.Build(document, index => _controller.IsTicked(consigne.Key, index), (item, ticked) => Tick(consigne, item, ticked)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsToolBarVisible = false,
        };
        panel.Children.Add(viewer);
        _detail.Content = panel;
        _controller.Open(consigne);
    }

    private Grid DetailHeader(Consigne consigne, TabGroup? group)
    {
        Grid header = new() { Margin = new Thickness(24, 4, 0, 4) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        StackPanel titles = new();
        titles.Children.Add(new TextBlock { Text = consigne.Title, FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        string status = consigne.Status == ConsigneStatus.InReview ? "en révision — ne pas exécuter" : "prête";
        string session = group?.Label ?? consigne.TabTitle;
        TextBlock line = new() { Text = $"{session} · {status} · mise à jour {TimeOf(consigne.Updated)}", Margin = new Thickness(0, 2, 0, 0) };
        line.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        titles.Children.Add(line);
        StackPanel actions = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        Button folder = new() { Content = "Ouvrir le dossier", Margin = new Thickness(0, 0, 6, 0) };
        folder.Click += (_, _) => _controller.OpenFolder(consigne.Key.Folder);
        Button archive = new() { Content = "Archiver" };
        archive.Click += async (_, _) => await ArchiveAsync(consigne).ConfigureAwait(true);
        actions.Children.Add(folder);
        actions.Children.Add(archive);
        Grid.SetColumn(actions, 1);
        header.Children.Add(titles);
        header.Children.Add(actions);
        return header;
    }

    private bool Tick(Consigne consigne, ListItemBlock item, bool ticked)
    {
        if (item.TaskIndex is not int index)
        {
            return false;
        }
        if (_controller.Tick(consigne.Key, index, ticked, item.Callback ? item.Text : null) is string failure)
        {
            MessageBox.Show(this, "La session n'a pas pu être prévenue : la case reste décochée.\n\n" + failure, AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        return true;
    }

    private async System.Threading.Tasks.Task ArchiveAsync(Consigne consigne)
    {
        if (await _controller.ArchiveAsync(consigne.Key).ConfigureAwait(true) is string failure)
        {
            MessageBox.Show(this, "L'archivage a échoué.\n\n" + failure, AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowEmpty(string text)
    {
        TextBlock message = new() { Text = text, Margin = new Thickness(32), TextWrapping = TextWrapping.Wrap };
        message.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        _detail.Content = message;
    }

    private static string TimeOf(DateTimeOffset moment)
    {
        DateTime local = moment.LocalDateTime;
        return local.Date == DateTime.Today ? local.ToString("HH:mm", CultureInfo.CurrentCulture) : local.ToString("d MMM HH:mm", CultureInfo.CurrentCulture);
    }

    private static string ShapeOf(IReadOnlyList<TabGroup> groups)
    {
        return string.Join('\n', groups.Select(group =>
            $"{group.Folder}\t{group.Label}\t{group.IsClosed}\t{group.UnreadCount}\t"
            + string.Join(';', group.Consignes.Select(consigne => $"{consigne.Key.Id},{consigne.Updated.UtcTicks},{consigne.Status},{consigne.Title}"))));
    }
}
