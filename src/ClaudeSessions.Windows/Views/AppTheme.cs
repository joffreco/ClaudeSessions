using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ClaudeSessions.Windows.Interop;

namespace ClaudeSessions.Windows.Views;

/// <summary>
/// Light or dark look of the app, after the app mode of Windows. The chrome of the session windows follows a switch at
/// once; menus and dialogs take the Fluent theme of the mode in force when they open.
/// </summary>
internal static class AppTheme
{
    private const string FluentLightUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Light.xaml";
    private const string FluentDarkUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Dark.xaml";
    private const string BackgroundKey = "ApplicationBackgroundBrush";
    private const string ForegroundKey = "TextFillColorPrimaryBrush";

    private static ResourceDictionary? _fluentLight;
    private static ResourceDictionary? _fluentDark;
    private static bool? _dark;

    /// <summary>True when the dark mode is in force.</summary>
    public static bool IsDark => _dark ?? true;

    /// <summary>Puts the light or the dark mode in force; does nothing when it already is.</summary>
    /// <param name="dark">True for the dark mode.</param>
    public static void Apply(bool dark)
    {
        if (_dark == dark)
        {
            return;
        }
        _dark = dark;
        ChromePalette.Apply(dark);
    }

    /// <summary>Gives an element and its content, such as a menu, the Fluent look of the mode in force.</summary>
    /// <param name="element">The element.</param>
    public static void Dress(FrameworkElement element)
    {
        element.Resources.MergedDictionaries.Add(Fluent());
    }

    /// <summary>Gives a dialog the Fluent look of the mode in force, with its background, text and title bar.</summary>
    /// <param name="window">The dialog.</param>
    public static void DressDialog(Window window)
    {
        Dress(window);
        window.SetResourceReference(Control.BackgroundProperty, BackgroundKey);
        window.SetResourceReference(Control.ForegroundProperty, ForegroundKey);
        bool dark = IsDark;
        window.SourceInitialized += (_, _) => PaintTitleBar(window, dark);
    }

    private static ResourceDictionary Fluent()
    {
        return IsDark
            ? _fluentDark ??= new ResourceDictionary { Source = new Uri(FluentDarkUri) }
            : _fluentLight ??= new ResourceDictionary { Source = new Uri(FluentLightUri) };
    }

    private static void PaintTitleBar(Window window, bool dark)
    {
        int value = dark ? 1 : 0;
        _ = NativeMethods.DwmSetWindowAttribute(new WindowInteropHelper(window).Handle, NativeMethods.DwmUseImmersiveDarkMode, ref value, Marshal.SizeOf<int>());
    }
}
