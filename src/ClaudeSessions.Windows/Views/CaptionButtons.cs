using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using ClaudeSessions.Windows.Interop;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// Minimize, maximize and close buttons of a window whose tab strip replaces the title bar. The
/// maximize button answers Windows as a native one does, so Windows 11 shows its snap layouts over it.
/// </summary>
internal sealed class CaptionButtons : StackPanel
{
    private const double ButtonWidth = 46;
    private const double GlyphSize = 10;
    private const string MinimizeGlyph = "";
    private const string MaximizeGlyph = "";
    private const string RestoreGlyph = "";
    private const string CloseGlyph = "";
    private static readonly FontFamily GlyphFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    private readonly Window _window;
    private readonly TextBlock _maximizeGlyph = Glyph(MaximizeGlyph);
    private readonly Border _maximize;

    /// <summary>Creates the buttons of a window and follows its state.</summary>
    /// <param name="window">The window the buttons act on.</param>
    public CaptionButtons(Window window)
    {
        _window = window;
        Orientation = Orientation.Horizontal;
        WindowChrome.SetIsHitTestVisibleInChrome(this, true);
        _maximize = CaptionButton(_maximizeGlyph, ChromePalette.CaptionHover, ToggleMaximize);
        Children.Add(CaptionButton(Glyph(MinimizeGlyph), ChromePalette.CaptionHover, () => SystemCommands.MinimizeWindow(window)));
        Children.Add(_maximize);
        Children.Add(CaptionButton(Glyph(CloseGlyph), ChromePalette.CloseHover, () => SystemCommands.CloseWindow(window)));
        window.StateChanged += (_, _) => ShowState();
        window.Loaded += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(window).Handle).AddHook(OnMessage);
        ShowState();
    }

    private void ShowState()
    {
        _maximizeGlyph.Text = _window.WindowState == WindowState.Maximized ? RestoreGlyph : MaximizeGlyph;
    }

    private void ToggleMaximize()
    {
        if (_window.WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(_window);
            return;
        }
        SystemCommands.MaximizeWindow(_window);
    }

    private IntPtr OnMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (message)
        {
            case NativeMethods.WmNcHitTest:
                bool over = IsOverMaximize(lParam);
                _maximize.Background = over ? ChromePalette.CaptionHover : Brushes.Transparent;
                handled = over;
                return over ? NativeMethods.HtMaxButton : IntPtr.Zero;
            case NativeMethods.WmNcLeftButtonDown when wParam == NativeMethods.HtMaxButton:
                handled = true;
                break;
            case NativeMethods.WmNcLeftButtonUp when wParam == NativeMethods.HtMaxButton:
                handled = true;
                ToggleMaximize();
                break;
            case NativeMethods.WmNcMouseLeave:
                _maximize.Background = Brushes.Transparent;
                break;
        }
        return IntPtr.Zero;
    }

    private bool IsOverMaximize(IntPtr lParam)
    {
        if (PresentationSource.FromVisual(_maximize) is null)
        {
            return false;
        }
        long packed = lParam.ToInt64();
        Point screen = new((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF));
        return new Rect(_maximize.RenderSize).Contains(_maximize.PointFromScreen(screen));
    }

    private static Border CaptionButton(TextBlock glyph, Brush hover, Action click)
    {
        Border button = new() { Width = ButtonWidth, Background = Brushes.Transparent, Child = glyph };
        button.MouseEnter += (_, _) => button.Background = hover;
        button.MouseLeave += (_, _) => button.Background = Brushes.Transparent;
        button.MouseLeftButtonUp += (_, _) => click();
        return button;
    }

    private static TextBlock Glyph(string glyph)
    {
        return new TextBlock
        {
            Text = glyph,
            FontFamily = GlyphFont,
            FontSize = GlyphSize,
            Foreground = ChromePalette.Text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
