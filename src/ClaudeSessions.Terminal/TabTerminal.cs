using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ClaudeSessions.Adapters;
using Microsoft.Terminal.Wpf;

namespace ClaudeSessions.Terminal;

/// <summary>
/// The terminal of one tab: a Windows Terminal control connected to the session process. Each
/// launch gets a fresh control and a fresh process; events are raised on the UI thread. A sleeping
/// terminal has neither: it shows a placeholder until it is launched again. The app that hosts it
/// says whether the dark mode is in force and paints its background, which shows before the control loads.
/// </summary>
public sealed class TabTerminal : Border
{
    private const string BracketedPasteStart = "\u001b[200~";
    private const string BracketedPasteEnd = "\u001b[201~";
    private const string RedrawKey = "\u000c";

    private readonly string _fontFamily;
    private readonly Func<bool> _isDark;
    private short _fontSize;
    private TerminalControl? _control;
    private TerminalControl? _connectedControl;
    private ConPtyConnection? _connection;
    private bool _redrawPending;
    private DateTimeOffset _lastTouched;

    /// <summary>Creates an empty terminal; nothing runs until <see cref="Launch"/>.</summary>
    /// <param name="fontFamily">Font of the terminal.</param>
    /// <param name="fontSize">Font size, in points.</param>
    /// <param name="isDark">Tells whether the dark mode is in force, read at each launch and each repaint.</param>
    /// <param name="background">Background shown around and before the control; a brush the app repaints follows the mode.</param>
    public TabTerminal(string fontFamily, short fontSize, Func<bool> isDark, Brush background)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _isDark = isDark;
        Background = background;
        IsVisibleChanged += (_, _) => RedrawIfPending();
    }

    /// <summary>Raised when the session sets a new title.</summary>
    public event Action<string>? TitleChanged;

    /// <summary>Raised when the process ends on its own, with its exit code.</summary>
    public event Action<int>? Exited;

    /// <summary>True once a process was launched and until it ends or is killed.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>True once the tab has launched a process, until it is put to sleep.</summary>
    public bool WasLaunched { get; private set; }

    /// <summary>True from the moment the tab is put to sleep until its next launch.</summary>
    public bool IsSleeping { get; private set; }

    /// <summary>Last launch, keystroke or look of the user at the terminal.</summary>
    public DateTimeOffset LastTouched => _lastTouched;

    /// <summary>Starts the session process in a fresh terminal, ending any previous one.</summary>
    /// <param name="commandLine">Full command line of the process.</param>
    public void Launch(string commandLine)
    {
        Kill();
        OscTitleParser titles = new();
        ConPtyConnection connection = new(commandLine);
        bool titled = false;
        connection.OutputObserved += data =>
        {
            if (OnOutputObserved(titles, data) && !titled)
            {
                titled = true;
                FocusOnceReady(connection);
            }
        };
        connection.Exited += code => Dispatcher.BeginInvoke(() => OnExited(connection, code));
        connection.StartFailed += reason => Dispatcher.BeginInvoke(() => OnExited(connection, -1));
        connection.InputWritten += Touch;
        TerminalControl control = new() { Focusable = true };
        control.Loaded += (_, _) => Connect(control, connection);
        _connection = connection;
        _control = control;
        Child = control;
        IsRunning = true;
        WasLaunched = true;
        IsSleeping = false;
        Touch();
    }

    /// <summary>
    /// Puts the tab to sleep: ends the process and destroys the terminal control, so that both free
    /// their memory, and shows a placeholder until the next <see cref="Launch"/>.
    /// </summary>
    /// <param name="placeholder">What the tab shows while it sleeps.</param>
    public void Sleep(UIElement placeholder)
    {
        Kill();
        DestroyControl();
        Child = placeholder;
        WasLaunched = false;
        IsSleeping = true;
    }

    /// <summary>Records that the user is looking at the terminal.</summary>
    public void MarkSeen()
    {
        Touch();
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

    /// <summary>Repaints the running terminal in the theme of the mode in force; a later launch takes it anyway.</summary>
    public void Repaint()
    {
        SetFontSize(_fontSize);
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

    /// <summary>
    /// Types a message into the session and submits it with Enter. A message on several lines goes as a
    /// bracketed paste, so that its line breaks stay in the message instead of submitting each line. A slash
    /// command or a ! command on one line is typed key by key, as Claude Code only recognises them typed.
    /// </summary>
    /// <param name="text">The message.</param>
    /// <returns>True when a session runs and received it.</returns>
    public bool SubmitPrompt(string text)
    {
        if (!IsRunning || _connection is null)
        {
            return false;
        }
        string message = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool typed = !message.Contains('\n', StringComparison.Ordinal) && (message.StartsWith('/') || message.StartsWith('!'));
        _connection.WriteInput(typed ? message : BracketedPasteStart + message + BracketedPasteEnd);
        _connection.WriteInput("\r");
        return true;
    }

    /// <summary>Sends typed text to the session.</summary>
    /// <param name="text">The text.</param>
    /// <returns>True when a session runs and received it.</returns>
    public bool TypeText(string text)
    {
        if (!IsRunning || _connection is null)
        {
            return false;
        }
        _connection.WriteInput(text);
        return true;
    }

    /// <summary>
    /// Asks the session to repaint its whole screen (Ctrl+L), at once when the terminal is shown, else
    /// when it next is. Needed after a remote desktop reconnection, which leaves the control black.
    /// </summary>
    public void RequestRedraw()
    {
        _redrawPending = true;
        RedrawIfPending();
    }

    private void Touch()
    {
        _lastTouched = DateTimeOffset.UtcNow;
    }

    private void DestroyControl()
    {
        if (_control is not TerminalControl control)
        {
            return;
        }
        Child = null;
        DisposeHosts(control);
        _control = null;
        _connectedControl = null;
    }

    private static void DisposeHosts(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is HwndHost host)
            {
                host.Dispose();
                continue;
            }
            DisposeHosts(child);
        }
    }

    private void RedrawIfPending()
    {
        if (!_redrawPending || !IsVisible)
        {
            return;
        }
        _redrawPending = false;
        if (IsRunning && _connection is not null && _control is not null && ReferenceEquals(_control, _connectedControl))
        {
            _connection.WriteInput(RedrawKey);
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
        FocusOnceReady(connection);
    }

    /// <summary>
    /// Gives the keyboard to a new terminal once its native window exists: at Loaded it may not yet, and
    /// a focus given then reaches WPF but not the terminal. Asked again at the first title, when Claude
    /// Code shows its prompt. Only the shown terminal of the active window takes it, so the focus is never
    /// taken from another tab or window.
    /// </summary>
    /// <param name="connection">Connection of the launch the focus belongs to; a later launch cancels it.</param>
    private void FocusOnceReady(ConPtyConnection connection)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (ReferenceEquals(_connection, connection) && _control is TerminalControl control && control.IsVisible && Window.GetWindow(control)?.IsActive == true)
            {
                control.Focus();
            }
        });
    }

    private void ApplyFont(TerminalControl control, ConPtyConnection connection)
    {
        bool dark = _isDark();
        control.SetTheme(TerminalThemes.For(dark), _fontFamily, _fontSize, ColorOf(TerminalThemes.BackgroundOf(dark)));
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

    private bool OnOutputObserved(OscTitleParser titles, string data)
    {
        string? title = titles.Feed(data);
        if (title is null)
        {
            return false;
        }
        Dispatcher.BeginInvoke(() => TitleChanged?.Invoke(title));
        return true;
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
