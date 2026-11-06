using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Windows.Hosting;
using ClaudeSessions.Windows.Interop;
using ClaudeSessions.Windows.Terminal;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// A window of the application: a strip of tabs, a banner for a failed session, and one terminal
/// per tab, only the selected one visible. A tab starts its session the first time it is shown.
/// </summary>
internal sealed class SessionWindowView : Window
{
    private const double CaptionBarHeight = 36;
    private const uint TaskbarFlashCount = 5;
    private static readonly int?[] WheelLineChoices = [1, 3, 5, 10, null];
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/ClaudeSessions.ico");

    private readonly WindowCoordinator _coordinator;
    private readonly Dictionary<TabId, TabTerminal> _terminals = [];
    private readonly Dictionary<TabId, int> _failures = [];
    private readonly Dictionary<TabId, SessionTitle> _statuses = [];
    private readonly AttentionTracker _attention = new();
    private readonly StackPanel _tabStrip = new() { Orientation = Orientation.Horizontal };
    private readonly Grid _terminalHost = new();
    private readonly Border _banner = new() { Background = ChromePalette.Banner, Padding = new Thickness(10, 6, 10, 6), Visibility = Visibility.Collapsed };
    private readonly TextBlock _bannerText = new() { Foreground = ChromePalette.Text, VerticalAlignment = VerticalAlignment.Center };
    private readonly Border _root = new();
    private readonly TabDragger _dragger;
    private TabRow? _tabRow;
    private TabId? _revealedTab;
    private readonly WindowChrome _chrome = new()
    {
        CaptionHeight = CaptionBarHeight,
        ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness,
        GlassFrameThickness = new Thickness(0, 0, 0, 1),
        CornerRadius = new CornerRadius(0),
        UseAeroCaptionButtons = false,
    };
    private bool _closeAllowed;
    private bool _flashing;

    /// <summary>Creates the view of a window; its terminals start when their tab is shown.</summary>
    /// <param name="coordinator">Runs the use cases behind the window.</param>
    /// <param name="model">The window it shows.</param>
    public SessionWindowView(WindowCoordinator coordinator, SessionWindow model)
    {
        _coordinator = coordinator;
        Model = model;
        _dragger = new TabDragger(_tabStrip, (tabId, index) => _coordinator.MoveTab(this, tabId, index), tabId => _coordinator.SelectTab(this, tabId));
        Background = ChromePalette.Chrome;
        Icon = BitmapFrame.Create(IconUri);
        WindowChrome.SetWindowChrome(this, _chrome);
        WorkAreaMaximizer.Attach(this);
        _root.Child = BuildLayout();
        Content = _root;
        ApplyBounds(model.Bounds);
        foreach (SessionTab tab in model.Tabs)
        {
            AttachTerminal(tab);
        }
        Loaded += (_, _) => Refresh();
        LocationChanged += (_, _) => ReportBounds();
        SizeChanged += (_, _) => ReportBounds();
        StateChanged += (_, _) => ReportBounds();
        StateChanged += (_, _) => FitToScreen();
        SizeChanged += (_, _) => FitToScreen();
        DpiChanged += (_, _) => FitToScreen();
    }

    /// <summary>The window it shows.</summary>
    public SessionWindow Model { get; }

    /// <summary>Tabs whose session process runs right now.</summary>
    public IEnumerable<TabId> RunningTabIds => _terminals.Where(pair => pair.Value.IsRunning).Select(pair => pair.Key);

    /// <summary>Titles of the running tabs whose session is working, by the spinner in their last title.</summary>
    public IEnumerable<string> BusyTabTitles => Model.Tabs
        .Where(tab => _terminals.TryGetValue(tab.Id, out TabTerminal? terminal) && terminal.IsRunning
            && _statuses.TryGetValue(tab.Id, out SessionTitle? status) && status.Status is not null && !status.IsIdle)
        .Select(tab => tab.DisplayTitle);

    /// <summary>Runs a command on this window.</summary>
    /// <param name="command">The command.</param>
    /// <returns>True when the command applied and the key must not reach the terminal.</returns>
    public bool TryExecute(AppCommand command)
    {
        TabId? selected = Model.SelectedTabId;
        switch (command)
        {
            case AppCommand.ResumeFailed:
                if (selected is not TabId failed || !_failures.ContainsKey(failed))
                {
                    return false;
                }
                _coordinator.Relaunch(this, failed);
                return true;
            case AppCommand.NewTab:
                _coordinator.NewTab(this, SelectedCwd());
                return true;
            case AppCommand.NewTabIn:
                _coordinator.NewTabIn(this, SelectedCwd());
                return true;
            case AppCommand.CloseTab:
                if (selected is TabId closing)
                {
                    _coordinator.CloseTab(this, closing);
                }
                return true;
            case AppCommand.ReopenClosedTab:
                _coordinator.ReopenClosedTab(this);
                return true;
            case AppCommand.NewWindow:
                _coordinator.NewWindow();
                return true;
            case AppCommand.NextTab:
            case AppCommand.PreviousTab:
                Model.SelectNeighbour(command == AppCommand.NextTab ? 1 : -1);
                Refresh();
                return true;
            case AppCommand.Copy:
                SelectedTerminal()?.CopySelection();
                return true;
            case AppCommand.Paste:
                SelectedTerminal()?.PasteClipboard();
                return true;
            case AppCommand.ZoomIn:
            case AppCommand.ZoomOut:
            case AppCommand.ResetZoom:
                _coordinator.Zoom(command);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Shows a tab added to the model.</summary>
    /// <param name="tab">The tab.</param>
    public void ShowAddedTab(SessionTab tab)
    {
        AttachTerminal(tab);
        Refresh();
    }

    /// <summary>Drops the terminal of a tab removed from the model.</summary>
    /// <param name="tabId">The tab.</param>
    public void DropTab(TabId tabId)
    {
        if (_terminals.Remove(tabId, out TabTerminal? terminal))
        {
            terminal.Kill();
            _terminalHost.Children.Remove(terminal);
        }
        _failures.Remove(tabId);
        _statuses.Remove(tabId);
        _attention.Forget(tabId);
        Refresh();
    }

    /// <summary>Starts, or starts again, the session of a tab.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="commandLine">Command line of its process.</param>
    public void LaunchTab(TabId tabId, string commandLine)
    {
        _failures.Remove(tabId);
        _statuses.Remove(tabId);
        _attention.Forget(tabId);
        _terminals[tabId].Launch(commandLine);
        Refresh();
    }

    /// <summary>Shows the banner of a tab whose session failed.</summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="exitCode">Exit code of its process.</param>
    public void ShowFailure(TabId tabId, int exitCode)
    {
        _failures[tabId] = exitCode;
        _statuses.Remove(tabId);
        _attention.Forget(tabId);
        Refresh();
    }

    /// <summary>
    /// Shows the state mark the session of a tab put in its last title. A session that finishes working
    /// while nobody watches it colors its tab, and flashes the window when the window is not active.
    /// </summary>
    /// <param name="tabId">The tab.</param>
    /// <param name="title">Its last title.</param>
    public void ShowStatus(TabId tabId, SessionTitle title)
    {
        _statuses[tabId] = title;
        bool watched = tabId == Model.SelectedTabId && IsActive;
        if (_attention.Observe(tabId, title, watched) && !IsActive)
        {
            FlashTaskbarButton();
        }
        Refresh();
    }

    /// <summary>Redraws the tab strip and shows the selected terminal, starting its session if needed.</summary>
    public void Refresh()
    {
        TabId? selected = Model.SelectedTabId;
        if (selected is TabId watched && IsActive)
        {
            _attention.Seen(watched);
        }
        StopFlashingWhenSeen();
        RebuildTabStrip();
        foreach ((TabId tabId, TabTerminal terminal) in _terminals)
        {
            terminal.Visibility = tabId == selected ? Visibility.Visible : Visibility.Collapsed;
        }
        UpdateBanner(selected);
        Title = "Claude — " + (selected is TabId id ? Model.FindTab(id)?.DisplayTitle : "");
        if (selected is not TabId shown || !IsLoaded)
        {
            return;
        }
        TabTerminal current = _terminals[shown];
        if (!current.WasLaunched)
        {
            current.Launch(_coordinator.CommandLineFor(shown));
        }
        if (IsActive)
        {
            current.FocusTerminal();
        }
    }

    /// <summary>Gives every terminal of the window a new font size.</summary>
    /// <param name="fontSize">The size, in points.</param>
    public void SetFontSize(short fontSize)
    {
        foreach (TabTerminal terminal in _terminals.Values)
        {
            terminal.SetFontSize(fontSize);
        }
    }

    /// <summary>Asks every session of the window to repaint its screen, each when its tab is shown.</summary>
    public void RedrawAll()
    {
        foreach (TabTerminal terminal in _terminals.Values)
        {
            terminal.RequestRedraw();
        }
    }

    /// <summary>Stops every session of the window without raising their exit events.</summary>
    public void KillAll()
    {
        foreach (TabTerminal terminal in _terminals.Values)
        {
            terminal.Kill();
        }
    }

    /// <summary>Closes the window for real, without asking the coordinator again.</summary>
    public void CloseNow()
    {
        _closeAllowed = true;
        Close();
    }

    /// <summary>Brings the window to the front.</summary>
    public void BringToFront()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
    }

    /// <summary>
    /// Gives the keyboard back to the selected terminal when the window becomes active, stops the
    /// flashing of its taskbar button, and ends the wait of the selected tab, which the user now sees.
    /// </summary>
    /// <param name="e">The activation event.</param>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        StopFlashingWhenSeen();
        if (Model.SelectedTabId is not TabId selected)
        {
            return;
        }
        if (_attention.Seen(selected))
        {
            RebuildTabStrip();
        }
        if (_terminals.TryGetValue(selected, out TabTerminal? terminal))
        {
            terminal.FocusTerminal();
        }
    }

    /// <summary>Hands the closing to the coordinator, which decides whether the window closes now.</summary>
    /// <param name="e">The closing event.</param>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_closeAllowed)
        {
            e.Cancel = _coordinator.OnWindowClosing(this);
        }
        if (!e.Cancel)
        {
            KillAll();
        }
        base.OnClosing(e);
    }

    private DockPanel BuildLayout()
    {
        Button add = ChromeButton("+", "Nouvel onglet (Ctrl+Maj+T)");
        add.Click += (_, _) => TryExecute(AppCommand.NewTab);
        Button menu = ChromeButton("☰", "Menu");
        menu.Click += (_, _) => OpenMenu(menu);
        CaptionButtons caption = new(this);
        _tabRow = new TabRow(_tabStrip, ChromeButton("◀", "Onglets précédents"), ChromeButton("▶", "Onglets suivants"), add, menu);
        DockPanel strip = new() { LastChildFill = true, Height = CaptionBarHeight };
        DockPanel.SetDock(caption, Dock.Right);
        strip.Children.Add(caption);
        strip.Children.Add(_tabRow);
        _banner.Child = BuildBannerContent();
        DockPanel layout = new();
        DockPanel.SetDock(strip, Dock.Top);
        DockPanel.SetDock(_banner, Dock.Top);
        layout.Children.Add(strip);
        layout.Children.Add(_banner);
        layout.Children.Add(_terminalHost);
        return layout;
    }

    private DockPanel BuildBannerContent()
    {
        Button resume = BannerButton("Reprendre (Entrée)", tabId => _coordinator.Relaunch(this, tabId));
        Button renew = BannerButton("Nouvelle session ici", tabId => _coordinator.RenewSession(this, tabId));
        Button close = BannerButton("Fermer (Ctrl+Maj+W)", tabId => _coordinator.CloseTab(this, tabId));
        StackPanel buttons = new() { Orientation = Orientation.Horizontal };
        buttons.Children.Add(resume);
        buttons.Children.Add(renew);
        buttons.Children.Add(close);
        DockPanel content = new();
        DockPanel.SetDock(buttons, Dock.Right);
        content.Children.Add(buttons);
        content.Children.Add(_bannerText);
        return content;
    }

    private Button BannerButton(string text, System.Action<TabId> action)
    {
        Button button = new() { Content = text, Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(8, 2, 8, 2) };
        button.Click += (_, _) =>
        {
            if (Model.SelectedTabId is TabId tabId)
            {
                action(tabId);
            }
        };
        return button;
    }

    private static Button ChromeButton(string text, string tooltip)
    {
        return new Button
        {
            Content = text,
            ToolTip = tooltip,
            Foreground = ChromePalette.Text,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 4, 10, 4),
            Focusable = false,
        };
    }

    private void OpenMenu(Button anchor)
    {
        ContextMenu menu = new() { PlacementTarget = anchor };
        menu.Items.Add(MenuItem("Nouvel onglet", "Ctrl+Maj+T", () => TryExecute(AppCommand.NewTab)));
        menu.Items.Add(MenuItem("Nouvel onglet dans…", "Ctrl+Maj+O", () => TryExecute(AppCommand.NewTabIn)));
        menu.Items.Add(MenuItem("Nouvelle fenêtre", "Ctrl+Maj+N", () => TryExecute(AppCommand.NewWindow)));
        menu.Items.Add(MenuItem("Rouvrir l'onglet fermé", "Ctrl+Maj+R", () => TryExecute(AppCommand.ReopenClosedTab)));
        menu.Items.Add(new Separator());
        menu.Items.Add(ClosedWindowsMenu());
        menu.Items.Add(WheelLinesMenu());
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Vérifier les mises à jour", "", () => _coordinator.Update(this)));
        menu.Items.Add(MenuItem("Quitter", "", () => _coordinator.Quit()));
        menu.IsOpen = true;
    }

    private MenuItem ClosedWindowsMenu()
    {
        MenuItem closedWindows = new() { Header = "Fenêtres fermées" };
        foreach (SessionWindow window in _coordinator.ClosedWindows())
        {
            string label = string.Join(", ", window.Tabs.Select(tab => tab.DisplayTitle));
            MenuItem entry = new() { Header = label.Length == 0 ? "(vide)" : label };
            WindowId windowId = window.Id;
            entry.Items.Add(MenuItem("Rouvrir", "", () => _coordinator.ReopenWindow(windowId)));
            entry.Items.Add(MenuItem("Oublier", "", () => _coordinator.ForgetWindow(windowId)));
            closedWindows.Items.Add(entry);
        }
        closedWindows.IsEnabled = closedWindows.Items.Count > 0;
        return closedWindows;
    }

    private MenuItem WheelLinesMenu()
    {
        MenuItem wheel = new() { Header = "Lignes par cran de molette" };
        foreach (int? lines in WheelLineChoices)
        {
            string label = lines is int count ? $"{count} {(count == 1 ? "ligne" : "lignes")}" : $"Réglage Windows ({SystemParameters.WheelScrollLines})";
            MenuItem entry = MenuItem(label, "", () => _coordinator.SetWheelLines(lines));
            entry.IsChecked = _coordinator.WheelLines == lines;
            wheel.Items.Add(entry);
        }
        return wheel;
    }

    private static MenuItem MenuItem(string header, string gesture, System.Action action)
    {
        MenuItem item = new() { Header = header, InputGestureText = gesture };
        item.Click += (_, _) => action();
        return item;
    }

    private void AttachTerminal(SessionTab tab)
    {
        TabTerminal terminal = _coordinator.CreateTerminal();
        TabId tabId = tab.Id;
        terminal.TitleChanged += title => _coordinator.UpdateTitle(this, tabId, title);
        terminal.Exited += exitCode => _coordinator.OnTabExited(this, tabId, exitCode);
        _terminals[tabId] = terminal;
        _terminalHost.Children.Add(terminal);
    }

    private void RebuildTabStrip()
    {
        _tabStrip.Children.Clear();
        foreach (SessionTab tab in Model.Tabs)
        {
            _tabStrip.Children.Add(TabHeader(tab));
        }
        _tabRow?.LayoutTabs();
        RevealSelectedTab();
    }

    private void RevealSelectedTab()
    {
        TabId? selected = Model.SelectedTabId;
        if (selected == _revealedTab)
        {
            return;
        }
        _revealedTab = selected;
        FrameworkElement? header = _tabStrip.Children.OfType<FrameworkElement>().FirstOrDefault(child => child.Tag is TabId tabId && tabId == selected);
        if (header is not null)
        {
            _tabRow?.Reveal(header);
        }
    }

    private Border TabHeader(SessionTab tab)
    {
        TabId tabId = tab.Id;
        string prefix = _failures.ContainsKey(tabId) ? "⚠ " : "";
        StackPanel label = new() { Orientation = Orientation.Horizontal };
        if (_statuses.TryGetValue(tabId, out SessionTitle? status) && status.Status is string mark)
        {
            label.Children.Add(new TextBlock { Text = mark, Foreground = status.IsIdle ? ChromePalette.IdleMark : ChromePalette.Text, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
        }
        label.Children.Add(new TextBlock { Text = prefix + tab.DisplayTitle, Foreground = ChromePalette.Text, FontWeight = tabId == Model.SelectedTabId ? FontWeights.SemiBold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center });
        Border header = new()
        {
            Child = label,
            Background = TabBackground(tabId),
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 4, 2, 0),
            CornerRadius = new CornerRadius(4, 4, 0, 0),
            ClipToBounds = true,
            ToolTip = tab.DisplayTitle + "\n" + tab.Cwd,
            Tag = tabId,
        };
        header.PreviewMouseLeftButtonDown += (_, e) => _dragger.Press(tabId, e);
        header.MouseLeftButtonUp += (_, _) => _coordinator.SelectTab(this, tabId);
        header.MouseUp += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _coordinator.CloseTab(this, tabId);
            }
        };
        return header;
    }

    private Brush TabBackground(TabId tabId)
    {
        if (tabId == Model.SelectedTabId)
        {
            return ChromePalette.SelectedTab;
        }
        return _attention.IsWaiting(tabId) ? ChromePalette.WaitingTab : ChromePalette.IdleTab;
    }

    private void FlashTaskbarButton()
    {
        _flashing = true;
        CallFlashWindow(NativeMethods.FlashTray, TaskbarFlashCount);
    }

    private void StopFlashingWhenSeen()
    {
        if (_flashing && (IsActive || !Model.Tabs.Any(tab => _attention.IsWaiting(tab.Id))))
        {
            _flashing = false;
            CallFlashWindow(NativeMethods.FlashStop, 0);
        }
    }

    private void CallFlashWindow(uint flags, uint count)
    {
        NativeMethods.FlashInfo info = new()
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.FlashInfo>(),
            Window = new WindowInteropHelper(this).Handle,
            Flags = flags,
            Count = count,
        };
        if (info.Window != IntPtr.Zero)
        {
            NativeMethods.FlashWindowEx(ref info);
        }
    }

    private void UpdateBanner(TabId? selected)
    {
        if (selected is TabId tabId && _failures.TryGetValue(tabId, out int exitCode))
        {
            _bannerText.Text = $"Session terminée (code {exitCode}). Si le dossier {Model.FindTab(tabId)?.Cwd} n'existe plus, choisis « Nouvelle session ici » ou ferme l'onglet.";
            _banner.Visibility = Visibility.Visible;
            return;
        }
        _banner.Visibility = Visibility.Collapsed;
    }

    private TabTerminal? SelectedTerminal()
    {
        return Model.SelectedTabId is TabId tabId ? _terminals.GetValueOrDefault(tabId) : null;
    }

    private string SelectedCwd()
    {
        return Model.SelectedTabId is TabId tabId ? Model.FindTab(tabId)?.Cwd ?? _coordinator.DefaultCwd : _coordinator.DefaultCwd;
    }

    private void ApplyBounds(WindowBounds bounds)
    {
        WindowBounds visible = ScreenBounds.KeepVisible(bounds);
        Left = visible.Left;
        Top = visible.Top;
        Width = visible.Width;
        Height = visible.Height;
        WindowState = visible.Maximized ? WindowState.Maximized : WindowState.Normal;
    }

    private void FitToScreen()
    {
        Thickness overflow = WindowState == WindowState.Maximized ? WorkAreaMaximizer.Overflow(this) : new Thickness(0);
        if (_root.Padding != overflow)
        {
            _root.Padding = overflow;
            _chrome.CaptionHeight = CaptionBarHeight + overflow.Top;
        }
    }

    private void ReportBounds()
    {
        if (!IsLoaded)
        {
            return;
        }
        Rect normal = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        _coordinator.UpdateBounds(this, new WindowBounds(normal.Left, normal.Top, normal.Width, normal.Height, WindowState == WindowState.Maximized));
    }
}
