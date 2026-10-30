using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// The tab row of the title bar, laid out as in Windows Terminal: the tabs share the room equally,
/// between <see cref="MinTabWidth"/> and <see cref="MaxTabWidth"/>; once they no longer fit, they
/// scroll with arrow buttons and the wheel instead of a scroll bar. Some room is always left after
/// the + and menu buttons, to drag the window.
/// </summary>
internal sealed class TabRow : DockPanel
{
    /// <summary>Narrowest width of a tab, margin included.</summary>
    public const double MinTabWidth = 80;

    /// <summary>Widest width of a tab, margin included.</summary>
    public const double MaxTabWidth = 240;

    private const double ArrowWidth = 28;
    private const double DragRoom = 40;

    private readonly Panel _tabs;
    private readonly ScrollViewer _scroller;
    private readonly Button _back;
    private readonly Button _forward;
    private readonly Button _add;
    private readonly Button _menu;
    private double _slot = MaxTabWidth;

    /// <summary>Builds the row around the tab headers and the title bar buttons.</summary>
    /// <param name="tabs">The panel of the tab headers.</param>
    /// <param name="back">Scrolls the tabs to the left; shown only when they overflow.</param>
    /// <param name="forward">Scrolls the tabs to the right; shown only when they overflow.</param>
    /// <param name="add">The new tab button.</param>
    /// <param name="menu">The menu button.</param>
    public TabRow(Panel tabs, Button back, Button forward, Button add, Button menu)
    {
        _tabs = tabs;
        _back = back;
        _forward = forward;
        _add = add;
        _menu = menu;
        _scroller = new ScrollViewer
        {
            Content = tabs,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Focusable = false,
        };
        LastChildFill = false;
        back.Width = ArrowWidth;
        forward.Width = ArrowWidth;
        back.Click += (_, _) => ScrollBy(-1);
        forward.Click += (_, _) => ScrollBy(1);
        _scroller.PreviewMouseWheel += OnMouseWheel;
        foreach (UIElement element in new UIElement[] { back, _scroller, forward, add, menu })
        {
            SetDock(element, Dock.Left);
            WindowChrome.SetIsHitTestVisibleInChrome(element, true);
            Children.Add(element);
        }
        SizeChanged += (_, _) => LayoutTabs();
    }

    /// <summary>Sizes the tab headers to the room of the row and shows the arrows when they overflow.</summary>
    public void LayoutTabs()
    {
        int count = _tabs.Children.Count;
        double room = ActualWidth - _add.DesiredSize.Width - _menu.DesiredSize.Width - DragRoom;
        bool overflow = count > 0 && count * MinTabWidth > room;
        if (overflow)
        {
            room -= 2 * ArrowWidth;
        }
        _slot = overflow || count == 0 ? MinTabWidth : Math.Min(MaxTabWidth, room / count);
        foreach (FrameworkElement header in _tabs.Children.OfType<FrameworkElement>())
        {
            header.Width = Math.Max(0, _slot - header.Margin.Left - header.Margin.Right);
        }
        _scroller.MaxWidth = Math.Max(0, room);
        _back.Visibility = overflow ? Visibility.Visible : Visibility.Collapsed;
        _forward.Visibility = _back.Visibility;
    }

    /// <summary>Scrolls a tab header into view once the row is laid out.</summary>
    /// <param name="header">The header to show.</param>
    public void Reveal(FrameworkElement header)
    {
        Dispatcher.BeginInvoke(header.BringIntoView, DispatcherPriority.Loaded);
    }

    private void ScrollBy(int tabs)
    {
        _scroller.ScrollToHorizontalOffset(_scroller.HorizontalOffset + tabs * _slot);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        ScrollBy(e.Delta > 0 ? -1 : 1);
        e.Handled = true;
    }
}
