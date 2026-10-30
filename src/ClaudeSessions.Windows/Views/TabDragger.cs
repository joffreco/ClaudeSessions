using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// Reorders the tabs of a strip by dragging them with the mouse, as in Windows Terminal. The strip
/// holds the mouse while a tab is dragged, so its headers can be rebuilt at each move. Each header
/// carries its <see cref="TabId"/> in <see cref="FrameworkElement.Tag"/>.
/// </summary>
internal sealed class TabDragger
{
    private readonly Panel _strip;
    private readonly Action<TabId, int> _move;
    private readonly Action<TabId> _select;
    private TabId? _pressed;
    private Point _pressedAt;
    private bool _dragging;

    /// <summary>Watches a strip for tab drags.</summary>
    /// <param name="strip">The panel holding the tab headers.</param>
    /// <param name="move">Moves a tab to a place, counted from 0.</param>
    /// <param name="select">Selects a tab, once it is dropped.</param>
    public TabDragger(Panel strip, Action<TabId, int> move, Action<TabId> select)
    {
        _strip = strip;
        _move = move;
        _select = select;
        strip.MouseMove += OnMouseMove;
        strip.MouseLeftButtonUp += OnMouseUp;
        strip.LostMouseCapture += (_, _) => End();
    }

    /// <summary>Notes the tab under a left press; it is dragged once the mouse moves far enough.</summary>
    /// <param name="tabId">The pressed tab.</param>
    /// <param name="e">The press.</param>
    public void Press(TabId tabId, MouseButtonEventArgs e)
    {
        _pressed = tabId;
        _pressedAt = e.GetPosition(_strip);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_pressed is not TabId tabId || e.LeftButton != MouseButtonState.Pressed)
        {
            End();
            return;
        }
        Point position = e.GetPosition(_strip);
        if (!_dragging && !StartDrag(position))
        {
            return;
        }
        _move(tabId, PlaceAt(tabId, position.X));
    }

    private bool StartDrag(Point position)
    {
        if (Math.Abs(position.X - _pressedAt.X) < SystemParameters.MinimumHorizontalDragDistance)
        {
            return false;
        }
        _dragging = true;
        _strip.CaptureMouse();
        return true;
    }

    private int PlaceAt(TabId dragged, double x)
    {
        return _strip.Children.OfType<FrameworkElement>().Count(header => header.Tag is TabId other && other != dragged && header.TranslatePoint(new Point(header.ActualWidth / 2, 0), _strip).X < x);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        TabId? dropped = _dragging ? _pressed : null;
        End();
        if (dropped is TabId tabId)
        {
            _select(tabId);
            e.Handled = true;
        }
    }

    private void End()
    {
        bool wasDragging = _dragging;
        _dragging = false;
        _pressed = null;
        if (wasDragging && _strip.IsMouseCaptured)
        {
            _strip.ReleaseMouseCapture();
        }
    }
}
