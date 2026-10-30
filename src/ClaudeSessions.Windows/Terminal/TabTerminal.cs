using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ClaudeSessions.Adapters;
using Microsoft.Terminal.Wpf;

namespace ClaudeSessions.Windows.Terminal;

/// <summary>
/// The terminal of one tab: a Windows Terminal control connected to the session process. Each
/// launch gets a fresh control and a fresh process; events are raised on the UI thread.
/// </summary>
internal sealed class TabTerminal : Border
{
    private const string BracketedPasteStart = "\u001b[200~";
    private const string BracketedPasteEnd = "\u001b[201~";

    private readonly string _fontFamily;
    private short _fontSize;
    private TerminalControl? _control;
    private TerminalControl? _connectedControl;
    private ConPtyConnection? _connection;

    /// <summary>Creates an empty terminal; nothing runs until <see cref="Launch"/>.</summary>
    /// <param name="fontFamily">Font of the terminal.</param>
    /// <param name="fontSize">Font size, in points.</param>
    public TabTerminal(string fontFamily, short fontSize)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        Background = new SolidColorBrush(ColorOf(TerminalThemes.BackgroundRgb));
    }

    /// <summary>Raised when the session sets a new title.</summary>
    public event Action<string>? TitleChanged;

    /// <summary>Raised when the process ends on its own, with its exit code.</summary>
    public event Action<int>? Exited;

    /// <summary>True once a process was launched and until it ends or is killed.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>True once the tab has launched a process at least once.</summary>
    public bool WasLaunched { get; private set; }

    /// <summary>Starts the session process in a fresh terminal, ending any previous one.</summary>
    /// <param name="commandLine">Full command line of the process.</param>
    public void Launch(string commandLine)
    {
        Kill();
        OscTitleParser titles = new();
        ConPtyConnection connection = new(commandLine);
        connection.OutputObserved += data => OnOutputObserved(titles, data);
        connection.Exited += code => Dispatcher.BeginInvoke(() => OnExited(connection, code));
        connection.StartFailed += reason => Dispatcher.BeginInvoke(() => OnExited(connection, -1));
        TerminalControl control = new() { Focusable = true };
        control.Loaded += (_, _) => Connect(control, connection);
        _connection = connection;
        _control = control;
        Child = control;
        IsRunning = true;
        WasLaunched = true;
    }

    /// <summary>Ends the process without raising <see cref="Exited"/>.</summary>
    public void Kill()
    {
        _connection?.Dispose();
        _connection = null;
        IsRunning = false;
    }

    /// <summary>Changes the font size, at once when the terminal runs, else at its next launch.</summary>
    /// <param name="fontSize">The size, in points.</param>
    public void SetFontSize(short fontSize)
    {
        _fontSize = fontSize;
        if (_control is TerminalControl control && ReferenceEquals(control, _connectedControl) && _connection is not null)
        {
            ApplyFont(control, _connection);
        }
    }

    /// <summary>Gives the keyboard focus to the terminal.</summary>
    public void FocusTerminal()
    {
        _control?.Focus();
    }

    /// <summary>Copies the selected text to the clipboard.</summary>
    public void CopySelection()
    {
        string selected = _control?.GetSelectedText() ?? "";
        if (selected.Length > 0)
        {
            Clipboard.SetText(selected);
        }
    }

    /// <summary>Pastes the clipboard text into the session, as a bracketed paste.</summary>
    public void PasteClipboard()
    {
        if (_connection is not null && Clipboard.ContainsText())
        {
            _connection.WriteInput(BracketedPasteStart + Clipboard.GetText() + BracketedPasteEnd);
        }
    }

    private void Connect(TerminalControl control, ConPtyConnection connection)
    {
        if (!ReferenceEquals(_connection, connection))
        {
            return;
        }
        HideScrollBar(control);
        ApplyFont(control, connection);
        control.Connection = connection;
        _connectedControl = control;
        if (control.IsVisible && Window.GetWindow(control)?.IsActive == true)
        {
            control.Focus();
        }
    }

    private void ApplyFont(TerminalControl control, ConPtyConnection connection)
    {
        control.SetTheme(TerminalThemes.Campbell(), _fontFamily, _fontSize, ColorOf(TerminalThemes.BackgroundRgb));
        if (control.Rows > 0 && control.Columns > 0)
        {
            connection.Resize((uint)control.Rows, (uint)control.Columns);
        }
    }

    private static void HideScrollBar(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollBar scrollBar)
            {
                scrollBar.Visibility = Visibility.Collapsed;
                ShrinkGridColumn(scrollBar);
                return;
            }
            HideScrollBar(child);
        }
    }

    private static void ShrinkGridColumn(ScrollBar scrollBar)
    {
        if (scrollBar.Parent is Grid grid && Grid.GetColumn(scrollBar) < grid.ColumnDefinitions.Count)
        {
            grid.ColumnDefinitions[Grid.GetColumn(scrollBar)].Width = GridLength.Auto;
        }
    }

    private void OnOutputObserved(OscTitleParser titles, string data)
    {
        string? title = titles.Feed(data);
        if (title is not null)
        {
            Dispatcher.BeginInvoke(() => TitleChanged?.Invoke(title));
        }
    }

    private void OnExited(ConPtyConnection connection, int exitCode)
    {
        if (!ReferenceEquals(_connection, connection))
        {
            return;
        }
        IsRunning = false;
        Exited?.Invoke(exitCode);
    }

    private static Color ColorOf(uint rgb)
    {
        return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
