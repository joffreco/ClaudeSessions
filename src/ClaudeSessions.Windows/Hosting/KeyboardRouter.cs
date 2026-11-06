using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Windows.Interop;
using ClaudeSessions.Windows.Views;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>
/// Catches the application shortcuts, and Ctrl+wheel for the zoom, before the terminal sees them:
/// the terminal is a native child window, so WPF bindings never fire while it has the focus. Also
/// sends the other wheel messages to the window under the mouse, as Windows Terminal does, so that an
/// inactive window scrolls without a click, and rescales them to the lines per notch chosen in the menu.
/// Also reports every mouse press in the app, the terminals included, whose messages WPF never sees.
/// Characters typed with AltGr go straight to the session: through the terminal control they did not
/// arrive, which { } [ ] on a Canadian Multilingual keyboard showed.
/// </summary>
internal sealed class KeyboardRouter : IDisposable
{
    private static readonly Dictionary<(Key Key, ModifierKeys Modifiers), AppCommand> Shortcuts = new()
    {
        [(Key.T, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.NewTab,
        [(Key.O, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.NewTabIn,
        [(Key.W, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.CloseTab,
        [(Key.R, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.ReopenClosedTab,
        [(Key.N, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.NewWindow,
        [(Key.Tab, ModifierKeys.Control)] = AppCommand.NextTab,
        [(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.PreviousTab,
        [(Key.P, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.SwitchTab,
        [(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.Copy,
        [(Key.V, ModifierKeys.Control | ModifierKeys.Shift)] = AppCommand.Paste,
        [(Key.Enter, ModifierKeys.None)] = AppCommand.ResumeFailed,
        [(Key.OemPlus, ModifierKeys.Control)] = AppCommand.ZoomIn,
        [(Key.Add, ModifierKeys.Control)] = AppCommand.ZoomIn,
        [(Key.OemMinus, ModifierKeys.Control)] = AppCommand.ZoomOut,
        [(Key.Subtract, ModifierKeys.Control)] = AppCommand.ZoomOut,
        [(Key.D0, ModifierKeys.Control)] = AppCommand.ResetZoom,
        [(Key.NumPad0, ModifierKeys.Control)] = AppCommand.ResetZoom,
    };

    private readonly WheelScrollService _wheel;
    private readonly Action _mousePressed;

    /// <summary>Starts watching the keyboard and mouse messages of the UI thread.</summary>
    /// <param name="wheel">Lines per wheel notch.</param>
    /// <param name="mousePressed">Called when a mouse button is pressed in a window of the app; the press still goes on.</param>
    public KeyboardRouter(WheelScrollService wheel, Action mousePressed)
    {
        _wheel = wheel;
        _mousePressed = mousePressed;
        ComponentDispatcher.ThreadPreprocessMessage += OnPreprocessMessage;
    }

    /// <summary>Stops watching the keyboard.</summary>
    public void Dispose()
    {
        ComponentDispatcher.ThreadPreprocessMessage -= OnPreprocessMessage;
    }

    private void OnPreprocessMessage(ref MSG message, ref bool handled)
    {
        if (handled)
        {
            return;
        }
        if (IsMousePress(message.message))
        {
            _mousePressed();
            return;
        }
        if (CommandOf(message) is not AppCommand command)
        {
            if (AltGrTextOf(message) is string text)
            {
                handled = ActiveView()?.TypeText(text) ?? false;
                return;
            }
            RetargetWheel(ref message);
            RescaleWheel(ref message);
            return;
        }
        handled = ActiveView()?.TryExecute(command) ?? false;
    }

    private static SessionWindowView? ActiveView()
    {
        return Application.Current.Windows.OfType<SessionWindowView>().FirstOrDefault(view => view.IsActive);
    }

    /// <summary>Gives the text a key press types with AltGr held; null for any other key, a dead key included.</summary>
    private static string? AltGrTextOf(MSG message)
    {
        if (message.message is not (NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown)
            || NativeMethods.GetKeyState(NativeMethods.VkRightMenu) >= 0
            || NativeMethods.GetKeyState(NativeMethods.VkLeftControl) >= 0)
        {
            return null;
        }
        byte[] state = new byte[256];
        if (!NativeMethods.GetKeyboardState(state))
        {
            return null;
        }
        char[] buffer = new char[8];
        uint scanCode = (uint)(((long)message.lParam >> 16) & 0xFF);
        int length = NativeMethods.ToUnicodeEx((uint)message.wParam, scanCode, state, buffer, buffer.Length, NativeMethods.ToUnicodeKeepKeyboardState, NativeMethods.GetKeyboardLayout(0));
        return length > 0 && !char.IsControl(buffer[0]) ? new string(buffer, 0, length) : null;
    }

    private static bool IsMousePress(int message)
    {
        return message is NativeMethods.WmLeftButtonDown or NativeMethods.WmRightButtonDown or NativeMethods.WmMiddleButtonDown
            or NativeMethods.WmNcLeftButtonDown or NativeMethods.WmNcRightButtonDown;
    }

    private static void RetargetWheel(ref MSG message)
    {
        if (message.message != NativeMethods.WmMouseWheel)
        {
            return;
        }
        long lParam = (long)message.lParam;
        NativeMethods.ScreenPoint cursor = new() { X = (short)lParam, Y = (short)(lParam >> 16) };
        IntPtr under = NativeMethods.WindowFromPoint(cursor);
        if (under != IntPtr.Zero && under != message.hwnd && NativeMethods.GetWindowThreadProcessId(under, IntPtr.Zero) == NativeMethods.GetCurrentThreadId())
        {
            message.hwnd = under;
        }
    }

    private void RescaleWheel(ref MSG message)
    {
        if (message.message != NativeMethods.WmMouseWheel)
        {
            return;
        }
        long wParam = (long)message.wParam;
        if (_wheel.Rescale((short)(wParam >> 16), SystemParameters.WheelScrollLines) is int delta)
        {
            message.wParam = new IntPtr(((long)(ushort)(short)delta << 16) | (wParam & 0xFFFF));
        }
    }

    private static AppCommand? CommandOf(MSG message)
    {
        if (message.message == NativeMethods.WmMouseWheel)
        {
            return Keyboard.Modifiers == ModifierKeys.Control ? WheelZoom(message.wParam) : null;
        }
        if (message.message != NativeMethods.WmKeyDown && message.message != NativeMethods.WmSysKeyDown)
        {
            return null;
        }
        Key key = KeyInterop.KeyFromVirtualKey((int)message.wParam);
        return Shortcuts.TryGetValue((key, Keyboard.Modifiers), out AppCommand command) ? command : null;
    }

    private static AppCommand WheelZoom(IntPtr wParam)
    {
        short delta = (short)((long)wParam >> 16);
        return delta > 0 ? AppCommand.ZoomIn : AppCommand.ZoomOut;
    }
}
