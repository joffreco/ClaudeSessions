using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClaudeSessions.Adapters;
using ClaudeSessions.Conversations.Hosting;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Application;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Conversations.Views;

/// <summary>
/// The window, laid out as Consignes: the tabs of the open ClaudeSessions windows on the left, grouped by
/// window; on the right the selected tab's conversation, with a field at the bottom to write to its session.
/// The sessions keep running in ClaudeSessions, which types the messages written here into the tab.
/// </summary>
internal sealed class MainWindow : Window
{
    private const string AppTitle = "Conversations";
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/Conversations.ico");

    /// <summary>How often the tab list is read again: ClaudeSessions saves it on each change.</summary>
    private static readonly TimeSpan TabsPeriod = TimeSpan.FromSeconds(3);

    /// <summary>How often the shown conversation is read again; each read takes only what was added.</summary>
    private static readonly TimeSpan ConversationPeriod = TimeSpan.FromSeconds(1);

    private const double MinZoom = 0.5;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.1;

    /// <summary>Height of a line of the conversation's text, for the distance one wheel notch scrolls.</summary>
    private const double WheelLineHeight = 20;

    private readonly ZoomFile _zoomFile;
    private readonly ScaleTransform _scale = new(1, 1);
    private double _zoom;
    private readonly ConversationTabs _tabs;
    private readonly ITranscriptSource _transcripts;
    private readonly ISessionCommands _commands;
    private readonly IStatusLineSource _statusLines;
    private readonly StackPanel _statusLine = new() { Margin = new Thickness(2, 6, 0, 0), Visibility = Visibility.Collapsed };
    private string _statusSignature = "";
    private HashSet<TabId>? _selectNewTabAmong;
    private PendingPrompt? _pending;
    private int _shownEntries;
    private IReadOnlyList<SkillInfo> _skills = [];
    private readonly ListBox _suggestionList = new() { MaxHeight = 320, BorderThickness = new Thickness(0) };
    private readonly Popup _suggestions = new() { Placement = PlacementMode.Top, StaysOpen = false, AllowsTransparency = true };

    /// <summary>Most commands and skills suggested at once.</summary>
    private const int MaxSuggestions = 12;

    /// <summary>How many quick reads follow a send, and how far apart.</summary>
    private const int FollowRounds = 32;
    private static readonly TimeSpan FollowDelay = TimeSpan.FromMilliseconds(250);
    private readonly Func<string, string> _windowsPathOf;
    private readonly PastedImages _images;
    private readonly Dictionary<TabId, List<string>> _draftImages = [];
    private readonly TreeView _tree = new() { BorderThickness = new Thickness(0) };
    private readonly ContentControl _detail = new();
    private readonly FlowDocumentScrollViewer _viewer = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, IsToolBarVisible = false };
    private readonly TextBlock _detailTitle = new() { FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _detailLine = new() { Margin = new Thickness(0, 2, 0, 0) };
    private readonly TextBox _prompt = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 64, MaxHeight = 220, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly Button _send = new() { Content = "Envoyer", Padding = new Thickness(16, 6, 16, 6), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(8, 0, 0, 0), IsEnabled = false };
    private readonly TextBlock _status = new() { Margin = new Thickness(2, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
    private readonly DockPanel _conversationPanel;
    private readonly DispatcherTimer _tabsTimer = new() { Interval = TabsPeriod };
    private readonly DispatcherTimer _conversationTimer = new() { Interval = ConversationPeriod };
    private readonly Dictionary<TabId, ConversationTab> _current = [];
    private readonly Dictionary<TabId, string> _drafts = [];
    private readonly HashSet<int> _foldedWindows = [];
    private string _shape = "";
    private TabId? _selected;
    private SessionId? _shownSession;
    private FlowDocument _document = ConversationDocumentBuilder.NewDocument();
    private readonly List<(string Signature, List<Block> Blocks)> _drawn = [];
    private int _firstShown;
    private Paragraph? _empty;
    private bool _rebuilding;
    private bool _readingTabs;
    private bool _readingConversation;
    private bool _sending;
    private readonly TextBlock _activityText = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly StackPanel _activityLine = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(26, 6, 24, 0), MaxWidth = ConversationDocumentBuilder.ColumnWidth, Visibility = Visibility.Collapsed };
    private readonly DispatcherTimer _activityTimer = new() { Interval = ActivityPeriod };
    private readonly Dictionary<TabId, TreeViewItem> _leaves = [];
    private readonly Dictionary<TabId, FrameworkElement> _tabSpinners = [];
    private readonly HashSet<TabId> _working = [];
    private readonly HashSet<TabId> _finishedUnseen = [];
    private bool _readingActivity;
    private readonly TextBlock _pinnedText = new() { TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.WordEllipsis, MaxHeight = 60 };
    private readonly Border _pinned;
    private readonly Grid _pinnedLayer = new() { VerticalAlignment = VerticalAlignment.Top };
    private Border? _pinnedSource;
    private bool _stickToBottom = true;

    /// <summary>How often every tab's transcript is checked for a turn under way or just ended.</summary>
    private static readonly TimeSpan ActivityPeriod = TimeSpan.FromSeconds(1);

    /// <summary>Background of a tab whose turn ended while nobody watched it, a translucent green that reads in both modes.</summary>
    private static readonly SolidColorBrush FinishedTab = Frozen(Color.FromArgb(0x55, 0x16, 0xC6, 0x0C));

    /// <summary>Badge of the taskbar button when a turn ended while the window was not active: a green dot with a dark ring.</summary>
    private static readonly DrawingImage FinishedBadge = GreenBadge();

    /// <summary>Creates the window.</summary>
    /// <param name="tabs">Lists the tabs and their current session.</param>
    /// <param name="transcripts">Reads the conversations.</param>
    /// <param name="commands">Hands messages, new windows and new tabs to ClaudeSessions.</param>
    /// <param name="statusLines">Reads the status line Claude Code last showed for a session.</param>
    /// <param name="windowsPathOf">Turns a WSL path into the path Windows uses to reach it.</param>
    /// <param name="zoomFile">Keeps the zoom of the window between two runs.</param>
    /// <param name="sessionImagesFolder">The WSL folder where pasted images are saved for the sessions.</param>
    public MainWindow(
        ConversationTabs tabs,
        ITranscriptSource transcripts,
        ISessionCommands commands,
        IStatusLineSource statusLines,
        Func<string, string> windowsPathOf,
        ZoomFile zoomFile,
        string sessionImagesFolder)
    {
        _tabs = tabs;
        _transcripts = transcripts;
        _commands = commands;
        _statusLines = statusLines;
        _windowsPathOf = windowsPathOf;
        _images = new PastedImages(windowsPathOf, sessionImagesFolder);
        _images.Changed += OnPromptChanged;
        DataObject.AddPastingHandler(_prompt, OnPaste);
        _prompt.AllowDrop = true;
        _prompt.PreviewDragOver += (_, e) =>
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        };
        _prompt.PreviewDrop += (_, e) => e.Handled = _images.TryPaste(e.Data, fromClipboard: false);
        _zoomFile = zoomFile;
        _zoom = Math.Clamp(zoomFile.Load(), MinZoom, MaxZoom);
        Title = AppTitle;
        Width = 1100;
        Height = 820;
        MinWidth = 640;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = BitmapFrame.Create(IconUri);
        _detailLine.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        _status.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        TextElement.SetFontFamily(_statusLine, ClaudeSessions.Ui.MarkdownDocumentBuilder.CodeFont);
        TextElement.SetFontSize(_statusLine, 12);
        _pinnedText.FontFamily = ClaudeSessions.Ui.MarkdownDocumentBuilder.BodyFont;
        _pinnedText.FontSize = ClaudeSessions.Ui.MarkdownDocumentBuilder.BodySize;
        _pinnedText.MaxHeight = Math.Ceiling(_pinnedText.FontSize * 1.34 * 3);
        _pinned = new Border
        {
            Child = _pinnedText,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = ConversationDocumentBuilder.BubbleMaxWidth,
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 10, 16, 10),
            Cursor = Cursors.Hand,
            Visibility = Visibility.Collapsed,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.25 },
        };
        _pinnedLayer.Children.Add(_pinned);
        _conversationPanel = ConversationPanel();
        // The whole window zooms, not only the conversation: the document's own zoom stays at 100 %.
        _viewer.MinZoom = 100;
        _viewer.MaxZoom = 100;
        _viewer.PreviewMouseWheel += OnConversationWheel;
        _viewer.SizeChanged += (_, _) => _document.PagePadding = ConversationDocumentBuilder.CenteredPadding(_viewer.ActualWidth);
        Grid layout = Layout();
        layout.LayoutTransform = _scale;
        Content = layout;
        ApplyZoom(_zoom);
        ShowEmpty("Lecture des onglets…");
        _tree.SelectedItemChanged += async (_, _) => await OnSelectionChangedAsync().ConfigureAwait(true);
        Border suggestionFrame = new() { Child = _suggestionList, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(4) };
        suggestionFrame.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
        suggestionFrame.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
        _suggestions.Child = suggestionFrame;
        _suggestions.PlacementTarget = _prompt;
        _suggestionList.PreviewMouseLeftButtonUp += (_, _) => AcceptSuggestion();
        _prompt.TextChanged += (_, _) => OnPromptChanged();
        _prompt.PreviewKeyDown += async (_, e) => await OnPromptKeyAsync(e).ConfigureAwait(true);
        _send.Click += async (_, _) => await SendAsync().ConfigureAwait(true);
        _tabsTimer.Tick += async (_, _) => await RefreshTabsAsync().ConfigureAwait(true);
        _conversationTimer.Tick += async (_, _) => await RefreshConversationAsync().ConfigureAwait(true);
        _activityTimer.Tick += async (_, _) => await RefreshActivityAsync().ConfigureAwait(true);
        Activated += (_, _) => MarkSeen();
        Closed += (_, _) =>
        {
            _tabsTimer.Stop();
            _conversationTimer.Stop();
            _activityTimer.Stop();
            if (VoiceTyping.IsOn)
            {
                VoiceTyping.Toggle();
            }
        };
    }

    /// <summary>Reads the tabs and starts following them.</summary>
    /// <returns>A task that completes after the first read.</returns>
    public async Task StartAsync()
    {
        await RefreshTabsAsync().ConfigureAwait(true);
        _tabsTimer.Start();
        _conversationTimer.Start();
        _activityTimer.Start();
    }

    private Grid Layout()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320), MinWidth = 200 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        DockPanel left = new() { Margin = new Thickness(8, 8, 0, 8) };
        StackPanel toolbar = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        toolbar.Children.Add(MenuButton());
        Button newWindow = new() { Content = "Nouvelle fenêtre", Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(10, 4, 10, 4), ToolTip = "Ouvre une fenêtre de ClaudeSessions avec un onglet (Ctrl+Maj+N)" };
        newWindow.Click += async (_, _) => await OpenWindowAsync().ConfigureAwait(true);
        toolbar.Children.Add(newWindow);
        DockPanel.SetDock(toolbar, Dock.Top);
        left.Children.Add(toolbar);
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

    // The ☰ menu, grouped as in ClaudeSessions; built when opened, so that it reflects the selected tab.
    private Button MenuButton()
    {
        Button button = new() { Content = "☰", Padding = new Thickness(10, 4, 10, 4), ToolTip = "Menu" };
        button.Click += (_, _) => OpenMenu(button);
        return button;
    }

    private void OpenMenu(Button anchor)
    {
        bool hasTab = _selected is TabId;
        ContextMenu menu = new() { PlacementTarget = anchor, Placement = PlacementMode.Bottom };
        menu.Items.Add(MenuItem("Nouvel onglet", "Ctrl+Maj+T", () => _ = OpenTabAsync(SelectedWindow()), hasTab));
        menu.Items.Add(MenuItem("Nouvelle fenêtre", "Ctrl+Maj+N", () => _ = OpenWindowAsync()));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Afficher le terminal", "Ctrl+Maj+G", () => _ = ShowTerminalAsync(), hasTab));
        menu.Items.Add(MenuItem("Écrire à la session", "Ctrl+L", FocusPrompt, hasTab));
        menu.Items.Add(MenuItem("Aller à un onglet…", "Ctrl+Maj+P", FocusTabs));
        menu.Items.Add(MenuItem("Ouvrir le dossier de l'onglet", "", OpenSelectedFolder, hasTab));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Ouvrir ClaudeSessions", "", () => Launch(ClaudeSessionsExecutable, "ClaudeSessions")));
        menu.Items.Add(MenuItem("Consignes", "", () => Launch(AppPaths.ConsignesExecutable, "Consignes")));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Quitter", "", Close));
        menu.IsOpen = true;
    }

    private static MenuItem MenuItem(string header, string gesture, Action action, bool enabled = true)
    {
        MenuItem item = new() { Header = header, InputGestureText = gesture, IsEnabled = enabled };
        item.Click += (_, _) => action();
        return item;
    }

    private static string ClaudeSessionsExecutable => Path.Combine(AppContext.BaseDirectory, "ClaudeSessions.exe");

    // A second launch of ClaudeSessions or Consignes brings the running one to the front.
    private void Launch(string executable, string name)
    {
        string? failure = null;
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory });
            failure = process is null ? $"{name} n'a pas démarré." : null;
        }
        catch (Win32Exception exception)
        {
            failure = $"{name} n'a pas pu démarrer : {exception.Message}";
        }
        if (failure is not null)
        {
            MessageBox.Show(this, failure, AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FocusPrompt()
    {
        if (_selected is TabId && ReferenceEquals(_detail.Content, _conversationPanel))
        {
            _prompt.Focus();
        }
    }

    private void FocusTabs()
    {
        if (_tree.SelectedItem is TreeViewItem selected)
        {
            selected.Focus();
        }
        else
        {
            _tree.Focus();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        ModifierKeys modifiers = Keyboard.Modifiers;
        if (e.Key == Key.L && modifiers == ModifierKeys.Control)
        {
            FocusPrompt();
            e.Handled = true;
        }
        else if (e.Key == Key.P && modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            FocusTabs();
            e.Handled = true;
        }
        else if (e.Key == Key.G && modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _ = ShowTerminalAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.N && modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _ = OpenWindowAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.T && modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _ = OpenTabAsync(SelectedWindow());
            e.Handled = true;
        }
        else if (modifiers == ModifierKeys.None && e.Key is Key.PageUp or Key.PageDown && ScrollerOf(_viewer) is ScrollViewer scroll)
        {
            // Page Up and Page Down scroll the conversation a page at a time, wherever the focus is,
            // the field and the tab list included.
            if (e.Key == Key.PageUp)
            {
                scroll.PageUp();
            }
            else
            {
                scroll.PageDown();
            }
            e.Handled = true;
        }
        else if (modifiers == ModifierKeys.Control && e.Key is Key.OemPlus or Key.Add or Key.OemMinus or Key.Subtract or Key.D0 or Key.NumPad0)
        {
            ApplyZoom(e.Key switch
            {
                Key.OemPlus or Key.Add => _zoom + ZoomStep,
                Key.OemMinus or Key.Subtract => _zoom - ZoomStep,
                _ => ZoomFile.Default,
            });
            e.Handled = true;
        }
        base.OnPreviewKeyDown(e);
    }

    /// <summary>Zooms the whole window with Ctrl and the wheel, as ClaudeSessions zooms its terminals.</summary>
    /// <param name="e">The wheel event.</param>
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            ApplyZoom(_zoom + Math.Sign(e.Delta) * ZoomStep);
            e.Handled = true;
        }
        base.OnPreviewMouseWheel(e);
    }

    private void ApplyZoom(double zoom)
    {
        _zoom = Math.Round(Math.Clamp(zoom, MinZoom, MaxZoom), 2);
        _scale.ScaleX = _zoom;
        _scale.ScaleY = _zoom;
        _zoomFile.Save(_zoom);
    }

    // The wheel scrolls the conversation wherever the mouse is: over a code block or a tool's output,
    // the inner text box would otherwise take the wheel and the conversation would not move.
    private void OnConversationWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None || ScrollerOf(_viewer) is not ScrollViewer scroll)
        {
            return;
        }
        double notches = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        scroll.ScrollToVerticalOffset(scroll.VerticalOffset - notches * SystemParameters.WheelScrollLines * WheelLineHeight);
        e.Handled = true;
    }

    // The header, the conversation, and the field to write to the session at the bottom.
    private DockPanel ConversationPanel()
    {
        DockPanel panel = new() { Margin = new Thickness(0, 8, 8, 8) };
        Grid header = new() { Margin = new Thickness(24, 4, 0, 4) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        StackPanel titles = new();
        titles.Children.Add(_detailTitle);
        titles.Children.Add(_detailLine);
        StackPanel actions = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        Button terminal = new() { Content = "Terminal", Margin = new Thickness(0, 0, 6, 0), ToolTip = "Affiche le terminal de l'onglet dans ClaudeSessions, pour ses menus et ses questions (Ctrl+Maj+G)" };
        terminal.Click += async (_, _) => await ShowTerminalAsync().ConfigureAwait(true);
        Button folder = new() { Content = "Ouvrir le dossier" };
        folder.Click += (_, _) => OpenSelectedFolder();
        actions.Children.Add(terminal);
        actions.Children.Add(folder);
        Grid.SetColumn(actions, 1);
        header.Children.Add(titles);
        header.Children.Add(actions);
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);
        // The field lines up with the conversation's column, centered under it.
        DockPanel composer = new() { Margin = new Thickness(24, 8, 24, 0), MaxWidth = ConversationDocumentBuilder.ColumnWidth };
        // As in the terminal, the status line of Claude Code sits under the input.
        DockPanel.SetDock(_statusLine, Dock.Bottom);
        composer.Children.Add(_statusLine);
        DockPanel.SetDock(_status, Dock.Bottom);
        composer.Children.Add(_status);
        DockPanel.SetDock(_send, Dock.Right);
        composer.Children.Add(_send);
        DockPanel.SetDock(_images.Strip, Dock.Top);
        composer.Children.Add(_images.Strip);
        composer.Children.Add(_prompt);
        DockPanel.SetDock(composer, Dock.Bottom);
        panel.Children.Add(composer);
        // While Claude works, a line under the conversation says so, as the spinner of the terminal does.
        _activityText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        FrameworkElement spinner = Spinner.Create(12);
        spinner.Margin = new Thickness(0, 0, 8, 0);
        _activityLine.Children.Add(spinner);
        _activityLine.Children.Add(_activityText);
        DockPanel.SetDock(_activityLine, Dock.Bottom);
        panel.Children.Add(_activityLine);
        // The last message of the user stays pinned at the top once it scrolled out, as in the Claude app.
        _pinnedText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        _pinned.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorTertiaryBrush");
        _pinned.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
        _pinned.MouseLeftButtonUp += (_, _) => _pinnedSource?.BringIntoView();
        _pinned.PreviewMouseWheel += OnConversationWheel;
        _viewer.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnConversationScrolled));
        Grid conversation = new();
        conversation.Children.Add(_viewer);
        conversation.Children.Add(_pinnedLayer);
        panel.Children.Add(conversation);
        return panel;
    }

    // The conversation follows its bottom as long as the user leaves it there. Only a scroll that moved the view
    // without the content or the window changing size is the user's; when the content grows (a reply, an image
    // decoded late, the activity line shown) or the view shrinks, the view goes back to the bottom if it followed it.
    // A scroll inside a block (a code block, a message) bubbles up here too and is not the conversation's.
    private void OnConversationScrolled(object sender, ScrollChangedEventArgs e)
    {
        if (ScrollerOf(_viewer) is ScrollViewer scroll && ReferenceEquals(e.OriginalSource, scroll))
        {
            if (e.ExtentHeightChange == 0 && e.ViewportHeightChange == 0)
            {
                _stickToBottom = scroll.VerticalOffset >= scroll.ScrollableHeight - 8;
            }
            else if (_stickToBottom && scroll.VerticalOffset < scroll.ScrollableHeight)
            {
                scroll.ScrollToEnd();
            }
        }
        UpdatePinned();
    }

    // Shows the last message of the user at the top while its bubble is above the visible part, hides it otherwise.
    private void UpdatePinned()
    {
        Border? last = LastBubble();
        _pinnedSource = last;
        bool above = last is not null && last.IsVisible
            && last.TranslatePoint(new Point(0, last.ActualHeight), _viewer).Y < 4;
        if (!above)
        {
            _pinned.Visibility = Visibility.Collapsed;
            return;
        }
        string text = last!.Child is TextBox box ? box.Text : "";
        _pinnedText.Text = text;
        _pinned.ToolTip = text;
        Thickness padding = ConversationDocumentBuilder.CenteredPadding(_viewer.ActualWidth);
        _pinnedLayer.Margin = new Thickness(padding.Left, 6, padding.Right, 0);
        _pinned.Visibility = Visibility.Visible;
    }

    private Border? LastBubble()
    {
        for (Block? block = _document.Blocks.LastBlock; block is not null; block = block.PreviousBlock)
        {
            if (block is BlockUIContainer { Child: Border { Tag: ConversationDocumentBuilder.BubbleTag } bubble })
            {
                return bubble;
            }
        }
        return null;
    }

    // The activity line of the shown tab, from its whole conversation.
    private void ShowActivity(Conversation? conversation)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (TurnActivity.Of(conversation, now) is TurnProgress progress)
        {
            _activityText.Text = ActivityText.Of(progress, now);
            _activityLine.Visibility = Visibility.Visible;
        }
        else
        {
            _activityLine.Visibility = Visibility.Collapsed;
        }
    }

    // Every tab's state, from the end of its transcript: a spinner while Claude works, green once a turn
    // ended unseen, until the tab is shown in the active window.
    private async Task RefreshActivityAsync()
    {
        if (_readingActivity)
        {
            return;
        }
        _readingActivity = true;
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (ConversationTab tab in _current.Values.ToList())
            {
                Conversation? recent = await _transcripts.ReadRecentAsync(tab.Session).ConfigureAwait(true);
                bool working = TurnActivity.Of(recent, now) is not null;
                bool wasWorking = _working.Contains(tab.Id);
                if (working)
                {
                    _working.Add(tab.Id);
                    _finishedUnseen.Remove(tab.Id);
                }
                else if (wasWorking)
                {
                    _working.Remove(tab.Id);
                    if (!IsWatched(tab.Id))
                    {
                        _finishedUnseen.Add(tab.Id);
                        if (!IsActive)
                        {
                            TaskbarItemInfo ??= new System.Windows.Shell.TaskbarItemInfo();
                            TaskbarItemInfo.Overlay = FinishedBadge;
                        }
                    }
                }
            }
            _working.IntersectWith(_current.Keys);
            _finishedUnseen.IntersectWith(_current.Keys);
            PaintTabStates();
        }
        finally
        {
            _readingActivity = false;
        }
    }

    private bool IsWatched(TabId tabId)
    {
        return IsActive && _selected == tabId;
    }

    private void PaintTabStates()
    {
        foreach ((TabId tabId, TreeViewItem leaf) in _leaves)
        {
            if (_tabSpinners.TryGetValue(tabId, out FrameworkElement? spinner))
            {
                spinner.Visibility = _working.Contains(tabId) ? Visibility.Visible : Visibility.Collapsed;
            }
            if (leaf.Header is Border header)
            {
                header.Background = _finishedUnseen.Contains(tabId) ? FinishedTab : Brushes.Transparent;
            }
        }
    }

    // The shown tab in the active window has been seen: its green goes, and the badge once no window needs it.
    private void MarkSeen()
    {
        if (IsActive && _selected is TabId tabId && _finishedUnseen.Remove(tabId))
        {
            PaintTabStates();
        }
        if (IsActive && TaskbarItemInfo is not null)
        {
            TaskbarItemInfo.Overlay = null;
        }
    }

    private static SolidColorBrush Frozen(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    private static DrawingImage GreenBadge()
    {
        GeometryDrawing dot = new(Frozen(Color.FromRgb(0x16, 0xC6, 0x0C)), new Pen(Frozen(Color.FromRgb(0x0C, 0x0C, 0x0C)), 2), new EllipseGeometry(new Point(8, 8), 7, 7));
        DrawingImage image = new(dot);
        image.Freeze();
        return image;
    }

    private async Task RefreshTabsAsync()
    {
        if (_readingTabs)
        {
            return;
        }
        _readingTabs = true;
        try
        {
            IReadOnlyList<ConversationTab>? tabs = await _tabs.ReadAsync().ConfigureAwait(true);
            if (tabs is null)
            {
                if (_current.Count == 0)
                {
                    ShowEmpty("Le fichier des onglets de ClaudeSessions est illisible pour le moment.");
                }
                return;
            }
            _current.Clear();
            foreach (ConversationTab tab in tabs)
            {
                _current[tab.Id] = tab;
            }
            string shape = string.Join("\n", tabs.Select(tab => $"{tab.Id}|{tab.Title}|{tab.Cwd}|{tab.Window}"));
            if (shape != _shape)
            {
                _shape = shape;
                Rebuild(tabs);
            }
            if (tabs.Count == 0)
            {
                ShowEmpty("Aucun onglet ouvert dans ClaudeSessions.");
            }
            else if (_selected is null)
            {
                ShowEmpty("Choisissez un onglet à gauche.");
            }
        }
        finally
        {
            _readingTabs = false;
        }
    }

    // One node per window, as Consignes has one per session; the selected tab stays selected.
    private void Rebuild(IReadOnlyList<ConversationTab> tabs)
    {
        // A tab opened from here becomes the selected one as soon as it shows up.
        ConversationTab? opened = _selectNewTabAmong is null ? null : tabs.FirstOrDefault(tab => !_selectNewTabAmong.Contains(tab.Id));
        TabId? wanted = opened?.Id ?? _selected;
        if (opened is not null)
        {
            _selectNewTabAmong = null;
        }
        TreeViewItem? keep = null;
        _rebuilding = true;
        try
        {
            _tree.Items.Clear();
            _leaves.Clear();
            _tabSpinners.Clear();
            foreach (IGrouping<int, ConversationTab> window in tabs.GroupBy(tab => tab.Window))
            {
                TreeViewItem node = WindowNode(window.Key, window.First().WindowId);
                foreach (ConversationTab tab in window)
                {
                    TreeViewItem leaf = new() { Header = TabHeader(tab), Tag = tab.Id, ToolTip = tab.Cwd };
                    _leaves[tab.Id] = leaf;
                    node.Items.Add(leaf);
                    if (tab.Id == wanted)
                    {
                        keep = leaf;
                    }
                }
                _tree.Items.Add(node);
            }
            if (keep is not null)
            {
                keep.IsSelected = true;
            }
            else
            {
                _selected = null;
            }
        }
        finally
        {
            _rebuilding = false;
        }
        if (opened is not null && keep is not null)
        {
            keep.BringIntoView();
            _ = OnSelectionChangedAsync();
        }
    }

    private TreeViewItem WindowNode(int window, WindowId windowId)
    {
        TextBlock header = new() { Text = $"Fenêtre {window}", FontWeight = FontWeights.SemiBold };
        ContextMenu menu = new();
        menu.Items.Add(MenuItem("Nouvel onglet dans cette fenêtre", "", () => _ = OpenTabAsync(windowId)));
        TreeViewItem node = new() { Header = header, IsExpanded = !_foldedWindows.Contains(window), ContextMenu = menu };
        node.Expanded += (_, e) => { if (e.OriginalSource == node) { _foldedWindows.Remove(window); } };
        node.Collapsed += (_, e) => { if (e.OriginalSource == node) { _foldedWindows.Add(window); } };
        return node;
    }

    // The tab's name and folder, after a spinner shown while Claude works; the frame turns green once a turn ended unseen.
    private Border TabHeader(ConversationTab tab)
    {
        StackPanel panel = new() { Orientation = Orientation.Horizontal };
        FrameworkElement spinner = Spinner.Create(10);
        spinner.Margin = new Thickness(0, 0, 6, 0);
        spinner.Visibility = _working.Contains(tab.Id) ? Visibility.Visible : Visibility.Collapsed;
        _tabSpinners[tab.Id] = spinner;
        panel.Children.Add(spinner);
        panel.Children.Add(new TextBlock { Text = tab.Title, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 200 });
        TextBlock folder = new() { Text = "  " + FolderName(tab.Cwd), VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        folder.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        panel.Children.Add(folder);
        return new Border
        {
            Child = panel,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 1, 6, 1),
            Background = _finishedUnseen.Contains(tab.Id) ? FinishedTab : Brushes.Transparent,
        };
    }

    private async Task OnSelectionChangedAsync()
    {
        if (_rebuilding || _tree.SelectedItem is not TreeViewItem { Tag: TabId tabId })
        {
            return;
        }
        if (_selected is TabId previous)
        {
            _drafts[previous] = _prompt.Text;
            _draftImages[previous] = _images.Take();
        }
        _selected = tabId;
        _prompt.Text = _drafts.GetValueOrDefault(tabId, "");
        _images.Restore(_draftImages.GetValueOrDefault(tabId) ?? []);
        _status.Text = "";
        _shownSession = null;
        _activityLine.Visibility = Visibility.Collapsed;
        MarkSeen();
        await RefreshConversationAsync().ConfigureAwait(true);
    }

    private async Task RefreshConversationAsync()
    {
        if (_readingConversation || _selected is not TabId selected || !_current.TryGetValue(selected, out ConversationTab? tab))
        {
            return;
        }
        _readingConversation = true;
        try
        {
            Conversation? conversation = await _transcripts.ReadAsync(tab.Session).ConfigureAwait(true);
            StatusLine? statusLine = await _statusLines.ReadAsync(tab.Session).ConfigureAwait(true);
            if (_selected == selected)
            {
                ShowConversation(tab, conversation);
                _skills = conversation?.Skills ?? [];
                ShowStatusLine(statusLine);
                ShowActivity(conversation);
            }
        }
        finally
        {
            _readingConversation = false;
        }
    }

    // Redrawn only when it changed, so a tooltip open on a gauge stays.
    private void ShowStatusLine(StatusLine? statusLine)
    {
        string signature = StatusLineView.Signature(statusLine);
        if (signature == _statusSignature)
        {
            return;
        }
        _statusSignature = signature;
        if (statusLine is null)
        {
            _statusLine.Children.Clear();
            _statusLine.Visibility = Visibility.Collapsed;
            return;
        }
        StatusLineView.Fill(_statusLine, statusLine);
        _statusLine.Visibility = Visibility.Visible;
    }

    // Redraws only when something was added; stays at the bottom when it was there.
    private void ShowConversation(ConversationTab tab, Conversation? conversation)
    {
        Title = $"{tab.Title} — {AppTitle}";
        _detailTitle.Text = tab.Title;
        int prompts = conversation?.Entries.OfType<UserPrompt>().Count() ?? 0;
        _detailLine.Text = $"{FolderName(tab.Cwd)} · session {tab.Session.ToString()[..8]} · {prompts} message{(prompts > 1 ? "s" : "")}";
        if (!ReferenceEquals(_detail.Content, _conversationPanel))
        {
            _detail.Content = _conversationPanel;
        }
        IReadOnlyList<ConversationEntry> entries = conversation?.Entries ?? [];
        bool otherSession = tab.Session != _shownSession;
        // A branch that went back before the first drawn entry, after /rewind, starts over.
        bool reset = otherSession || entries.Count < _firstShown;
        if (reset)
        {
            Reset(tab.Session, entries.Count);
            _stickToBottom = true;
        }
        bool changed = Update(entries);
        _shownEntries = entries.Count;
        if (!changed && !reset)
        {
            return;
        }
        SettlePending(entries);
        if (_stickToBottom)
        {
            // Once the new blocks are laid out, not before: scrolling earlier lands in the middle.
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => ScrollerOf(_viewer)?.ScrollToEnd());
        }
    }

    // Starts a new document, keeping the last entries only.
    private void Reset(SessionId session, int count)
    {
        _shownSession = session;
        _document = ConversationDocumentBuilder.NewDocument();
        _document.PagePadding = ConversationDocumentBuilder.CenteredPadding(_viewer.ActualWidth);
        _viewer.Document = _document;
        _drawn.Clear();
        _empty = null;
        _pending = null;
        _firstShown = Math.Max(0, count - ConversationDocumentBuilder.MaxEntries);
        if (_firstShown > 0)
        {
            _document.Blocks.Add(ConversationDocumentBuilder.NoticeLine($"Les {_firstShown} premières entrées ne sont pas affichées ; elles restent dans le terminal."));
        }
    }

    // Redraws only the entries that changed, usually the last one and the new ones, and leaves the rest
    // of the document as it is: the scroll position and what the user unfolded stay. Returns whether
    // anything changed.
    private bool Update(IReadOnlyList<ConversationEntry> entries)
    {
        int first = FirstChange(entries);
        if (first < 0)
        {
            return false;
        }
        for (int index = _drawn.Count - 1; index >= first; index--)
        {
            foreach (Block block in _drawn[index].Blocks)
            {
                _document.Blocks.Remove(block);
            }
            _drawn.RemoveAt(index);
        }
        if (_empty is not null)
        {
            _document.Blocks.Remove(_empty);
            _empty = null;
        }
        for (int index = _firstShown + first; index < entries.Count; index++)
        {
            bool final = ConversationDocumentBuilder.IsFinal(entries, index);
            List<Block> blocks = ConversationDocumentBuilder.Draw(entries[index], final, _windowsPathOf);
            _document.Blocks.AddRange(blocks);
            _drawn.Add((ConversationDocumentBuilder.Signature(entries[index], final), blocks));
        }
        if (_drawn.Count == 0)
        {
            _empty = ConversationDocumentBuilder.NoticeLine("Aucune conversation enregistrée pour cet onglet.");
            _document.Blocks.Add(_empty);
        }
        return true;
    }

    // The position, among the drawn entries, of the first one that differs from the conversation; -1 when
    // they all match and nothing was added.
    private int FirstChange(IReadOnlyList<ConversationEntry> entries)
    {
        int shown = Math.Max(0, entries.Count - _firstShown);
        int common = Math.Min(shown, _drawn.Count);
        for (int index = 0; index < common; index++)
        {
            int position = _firstShown + index;
            if (ConversationDocumentBuilder.Signature(entries[position], ConversationDocumentBuilder.IsFinal(entries, position)) != _drawn[index].Signature)
            {
                return index;
            }
        }
        return shown != _drawn.Count || (_drawn.Count == 0 && _empty is null) ? common : -1;
    }

    private void OnPromptChanged()
    {
        _send.IsEnabled = !_sending && (_prompt.Text.Trim().Length > 0 || _images.Paths.Count > 0);
        UpdateSuggestions();
    }

    // An image pasted in the field, or image files, become thumbnails instead of text.
    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (_images.TryPaste(e.DataObject))
        {
            e.CancelCommand();
        }
    }

    // Enter sends, Shift+Enter starts a new line, as in the Claude app. While suggestions show, the arrows
    // move in them, Tab or Enter completes the command and Escape closes them.
    private async Task OnPromptKeyAsync(KeyEventArgs e)
    {
        // A text box disables its paste command when the clipboard holds no text, so an image never reaches
        // the pasting handler: Ctrl+V and Shift+Insert are caught here first.
        bool paste = (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) || (e.Key == Key.Insert && Keyboard.Modifiers == ModifierKeys.Shift);
        if (paste && (Clipboard.ContainsImage() || Clipboard.ContainsFileDropList()) && Clipboard.GetDataObject() is IDataObject data && _images.TryPaste(data))
        {
            e.Handled = true;
            return;
        }
        // Space in the empty field turns voice typing on, as in Claude Code; it stays on after a send, until
        // Space is pressed again.
        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None && (VoiceTyping.IsOn || _prompt.Text.Length == 0))
        {
            VoiceTyping.Toggle();
            _status.Text = VoiceTyping.IsOn
                ? "Saisie vocale activée : parle, puis Entrée pour envoyer. Espace pour l'arrêter."
                : "Saisie vocale arrêtée.";
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape && VoiceTyping.IsOn)
        {
            // Escape closes the voice typing panel of Windows.
            VoiceTyping.Forget();
            _status.Text = "Saisie vocale arrêtée.";
        }
        if (_suggestions.IsOpen && HandleSuggestionKey(e))
        {
            return;
        }
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }
        e.Handled = true;
        await SendAsync().ConfigureAwait(true);
    }

    // Built-in commands and the session's skills whose name starts with, then contains, what follows the slash.
    private void UpdateSuggestions()
    {
        string text = _prompt.Text;
        if (!text.StartsWith('/') || text.Any(char.IsWhiteSpace))
        {
            _suggestions.IsOpen = false;
            return;
        }
        string typed = text[1..];
        List<SkillInfo> all = [.. _skills, .. SlashCommands.BuiltIn.Where(command => !_skills.Any(skill => skill.Name == command.Name))];
        List<SkillInfo> matches =
        [
            .. all.Where(item => item.Name.StartsWith(typed, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            .. all.Where(item => !item.Name.StartsWith(typed, StringComparison.OrdinalIgnoreCase) && item.Name.Contains(typed, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
        ];
        if (matches.Count == 0 || (matches.Count == 1 && matches[0].Name.Equals(typed, StringComparison.OrdinalIgnoreCase)))
        {
            _suggestions.IsOpen = false;
            return;
        }
        _suggestionList.Items.Clear();
        foreach (SkillInfo item in matches.Take(MaxSuggestions))
        {
            _suggestionList.Items.Add(SuggestionItem(item, typed));
        }
        _suggestionList.SelectedIndex = 0;
        _suggestions.Width = _prompt.ActualWidth;
        _suggestions.IsOpen = true;
    }

    // "/name" with the typed part in bold, then the description on one line.
    private static ListBoxItem SuggestionItem(SkillInfo item, string typed)
    {
        TextBlock name = new() { FontFamily = ClaudeSessions.Ui.MarkdownDocumentBuilder.CodeFont };
        int at = item.Name.IndexOf(typed, StringComparison.OrdinalIgnoreCase);
        if (typed.Length > 0 && at >= 0)
        {
            name.Inlines.Add(new Run("/" + item.Name[..at]));
            name.Inlines.Add(new Bold(new Run(item.Name.Substring(at, typed.Length))));
            name.Inlines.Add(new Run(item.Name[(at + typed.Length)..]));
        }
        else
        {
            name.Inlines.Add(new Run("/" + item.Name));
        }
        string firstLine = item.Description.Split('\n')[0];
        TextBlock description = new() { Text = firstLine, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(12, 0, 0, 0) };
        description.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        DockPanel row = new();
        row.Children.Add(name);
        row.Children.Add(description);
        return new ListBoxItem { Content = row, Tag = item.Name, ToolTip = item.Description.Length > 0 ? item.Description : null };
    }

    private bool HandleSuggestionKey(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _suggestionList.SelectedIndex = (_suggestionList.SelectedIndex + 1) % _suggestionList.Items.Count;
                break;
            case Key.Up:
                _suggestionList.SelectedIndex = (_suggestionList.SelectedIndex - 1 + _suggestionList.Items.Count) % _suggestionList.Items.Count;
                break;
            case Key.Tab:
            case Key.Enter when !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift):
                AcceptSuggestion();
                break;
            case Key.Escape:
                _suggestions.IsOpen = false;
                break;
            default:
                return false;
        }
        _suggestionList.ScrollIntoView(_suggestionList.SelectedItem);
        e.Handled = true;
        return true;
    }

    private void AcceptSuggestion()
    {
        if (_suggestionList.SelectedItem is ListBoxItem { Tag: string name })
        {
            _prompt.Text = "/" + name + " ";
            _prompt.CaretIndex = _prompt.Text.Length;
        }
        _suggestions.IsOpen = false;
        _prompt.Focus();
    }

    private async Task SendAsync()
    {
        string text = _images.AppendTo(_prompt.Text.Trim());
        if (_sending || text.Length == 0 || _selected is not TabId tabId)
        {
            return;
        }
        _sending = true;
        _send.IsEnabled = false;
        _status.Text = "Envoi…";
        try
        {
            PromptOutcome outcome = await _commands.SendPromptAsync(tabId, text).ConfigureAwait(true);
            _status.Text = Describe(outcome);
            if (outcome is PromptOutcome.Sent or PromptOutcome.SentToMenu)
            {
                _prompt.Clear();
                _images.Take();
                _drafts.Remove(tabId);
                _draftImages.Remove(tabId);
                if (outcome == PromptOutcome.Sent && !text.StartsWith('/'))
                {
                    ShowPending(tabId, text);
                }
                await FollowSentAsync(tabId).ConfigureAwait(true);
            }
        }
        finally
        {
            _sending = false;
            OnPromptChanged();
        }
    }

    // The message just sent, shown at once in grey until Claude Code writes it to the transcript: at the
    // start of the turn, or at the end of the current one when the session is busy.
    private void ShowPending(TabId tabId, string text)
    {
        RemovePending();
        Paragraph label = new(new Run("envoi… le message s'affichera dès que Claude Code l'aura pris en compte")) { Margin = new Thickness(0, 4, 0, 0), FontSize = 11, TextAlignment = TextAlignment.Right };
        BlockUIContainer bubble = ConversationDocumentBuilder.Bubble(text, pending: true);
        bubble.Margin = new Thickness(0, 16, 0, 0);
        label.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
        _pending = new PendingPrompt(tabId, text, _shownEntries, [bubble, label]);
        // Sending means wanting to see what comes back.
        _stickToBottom = true;
        _document.Blocks.AddRange(_pending.Blocks);
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => ScrollerOf(_viewer)?.ScrollToEnd());
    }

    private void RemovePending()
    {
        if (_pending is null)
        {
            return;
        }
        foreach (Block block in _pending.Blocks)
        {
            _document.Blocks.Remove(block);
        }
        _pending = null;
    }

    // Keeps the pending message last while new entries are drawn, and drops it once its prompt shows up.
    private void SettlePending(IReadOnlyList<ConversationEntry> entries)
    {
        if (_pending is null)
        {
            return;
        }
        if (_pending.TabId != _selected || entries.Skip(_pending.EntriesAtSend).OfType<UserPrompt>().Any(prompt => prompt.Text.Trim() == _pending.Text))
        {
            RemovePending();
            return;
        }
        foreach (Block block in _pending.Blocks)
        {
            _document.Blocks.Remove(block);
        }
        _document.Blocks.AddRange(_pending.Blocks);
    }

    // Reads the transcript often for a few seconds after a send, so that the message and the reply show up fast.
    private async Task FollowSentAsync(TabId tabId)
    {
        for (int round = 0; round < FollowRounds && _selected == tabId; round++)
        {
            await RefreshConversationAsync().ConfigureAwait(true);
            if (_pending is null && round > 0)
            {
                return;
            }
            await Task.Delay(FollowDelay).ConfigureAwait(true);
        }
    }

    private async Task ShowTerminalAsync()
    {
        if (_selected is TabId tabId)
        {
            Report(await _commands.ShowTabAsync(tabId).ConfigureAwait(true));
        }
    }

    // Asks ClaudeSessions for a new window; its tab is selected here once it shows up.
    private async Task OpenWindowAsync()
    {
        _selectNewTabAmong = [.. _current.Keys];
        Report(await _commands.OpenWindowAsync().ConfigureAwait(true));
        await RefreshTabsAsync().ConfigureAwait(true);
    }

    // Asks ClaudeSessions for a new tab in a window, in the folder of the window's selected tab.
    private async Task OpenTabAsync(WindowId? windowId)
    {
        if (windowId is not WindowId window)
        {
            return;
        }
        _selectNewTabAmong = [.. _current.Keys];
        Report(await _commands.OpenTabAsync(window).ConfigureAwait(true));
        await RefreshTabsAsync().ConfigureAwait(true);
    }

    private WindowId? SelectedWindow()
    {
        return _selected is TabId tabId && _current.TryGetValue(tabId, out ConversationTab? tab) ? tab.WindowId : null;
    }

    private void Report(WindowOutcome outcome)
    {
        if (outcome == WindowOutcome.Done)
        {
            return;
        }
        _selectNewTabAmong = null;
        string message = outcome == WindowOutcome.NoSuchWindow
            ? "Cette fenêtre n'est plus ouverte dans ClaudeSessions."
            : "ClaudeSessions ne répond pas : vérifie qu'il est ouvert et à jour.";
        MessageBox.Show(this, message, AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static string Describe(PromptOutcome outcome)
    {
        return outcome switch
        {
            PromptOutcome.Sent => $"Envoyé à {DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture)}.",
            PromptOutcome.SentToMenu => "Cette commande ouvre un menu : il s'affiche dans ClaudeSessions, au premier plan, où tu fais ton choix.",
            PromptOutcome.AwaitingApproval => "La session attend une autorisation : réponds d'abord dans ClaudeSessions, puis renvoie le message.",
            PromptOutcome.NotRunning => "La session de cet onglet ne tourne pas (onglet en veille, ou pas encore affiché depuis le démarrage) : ouvre-le dans ClaudeSessions, puis renvoie le message.",
            PromptOutcome.NoSuchTab => "Cet onglet n'est plus ouvert dans ClaudeSessions.",
            _ => "ClaudeSessions ne répond pas : vérifie qu'il est ouvert et à jour.",
        };
    }

    private void OpenSelectedFolder()
    {
        if (_selected is TabId tabId && _current.TryGetValue(tabId, out ConversationTab? tab) && tab.Cwd.Length > 0)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", _windowsPathOf(tab.Cwd)) { UseShellExecute = true })?.Dispose();
        }
    }

    private void ShowEmpty(string text)
    {
        TextBlock message = new() { Text = text, Margin = new Thickness(32), TextWrapping = TextWrapping.Wrap };
        message.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        _detail.Content = message;
    }

    private static string FolderName(string cwd)
    {
        string name = Path.GetFileName(cwd.TrimEnd('/'));
        return name.Length > 0 ? name : cwd;
    }

    private static ScrollViewer? ScrollerOf(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollViewer scroll)
            {
                return scroll;
            }
            if (ScrollerOf(child) is ScrollViewer nested)
            {
                return nested;
            }
        }
        return null;
    }

    // A message sent but not yet in the transcript, with the number of entries there were when it was sent.
    private sealed record PendingPrompt(TabId TabId, string Text, int EntriesAtSend, IReadOnlyList<Block> Blocks);
}
