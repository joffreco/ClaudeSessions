using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ClaudeSessions.Conversations.Views;

/// <summary>
/// The images pasted in the field, waiting to be sent: each is saved as a PNG in a WSL folder the sessions
/// can read, and shown as a thumbnail with a button to remove it. Claude Code reads an image from its path in
/// the message, so sending adds the paths to the text.
/// </summary>
internal sealed class PastedImages
{
    private const double ThumbnailHeight = 64;
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"];

    private readonly Func<string, string> _windowsPathOf;
    private readonly string _wslFolder;
    private List<string> _paths = [];

    /// <summary>Creates the list, empty.</summary>
    /// <param name="windowsPathOf">Turns a WSL path into the path Windows uses to reach it.</param>
    /// <param name="wslFolder">The WSL folder where pasted images are saved.</param>
    public PastedImages(Func<string, string> windowsPathOf, string wslFolder)
    {
        _windowsPathOf = windowsPathOf;
        _wslFolder = wslFolder.TrimEnd('/');
    }

    /// <summary>Raised when an image is added or removed.</summary>
    public event Action? Changed;

    /// <summary>Gets the row of thumbnails, to place above the field; hidden while empty.</summary>
    public WrapPanel Strip { get; } = new() { Margin = new Thickness(0, 0, 0, 6), Visibility = Visibility.Collapsed };

    /// <summary>Gets the WSL paths of the images waiting to be sent.</summary>
    public IReadOnlyList<string> Paths => _paths;

    /// <summary>Takes what is pasted or dropped when it is an image, or image files, and saves it.</summary>
    /// <param name="data">The pasted or dropped data.</param>
    /// <param name="fromClipboard">True for a paste, where a bitmap is read from the clipboard; a drop only brings files.</param>
    /// <returns>True when images were taken, and the data must not reach the field as text.</returns>
    public bool TryPaste(IDataObject data, bool fromClipboard = true)
    {
        List<string> added = [];
        if (data.GetData(DataFormats.FileDrop) is string[] files)
        {
            foreach (string file in files.Where(file => ImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
            {
                string? saved = Save(new BitmapImage(new Uri(file)));
                if (saved is not null)
                {
                    added.Add(saved);
                }
            }
        }
        else if (fromClipboard && Clipboard.ContainsImage() && Clipboard.GetImage() is BitmapSource image && Save(image) is string saved)
        {
            added.Add(saved);
        }
        if (added.Count == 0)
        {
            return false;
        }
        _paths.AddRange(added);
        Redraw();
        return true;
    }

    /// <summary>Hands over the images waiting, to keep them with a tab's draft, and empties the list.</summary>
    /// <returns>Their WSL paths.</returns>
    public List<string> Take()
    {
        List<string> taken = _paths;
        _paths = [];
        Redraw();
        return taken;
    }

    /// <summary>Puts back the images of a tab's draft.</summary>
    /// <param name="paths">Their WSL paths.</param>
    public void Restore(List<string> paths)
    {
        _paths = paths;
        Redraw();
    }

    /// <summary>Adds the images' paths to a message, one per line after it, as Claude Code reads them.</summary>
    /// <param name="text">The message, possibly empty.</param>
    /// <returns>The message to send.</returns>
    public string AppendTo(string text)
    {
        if (_paths.Count == 0)
        {
            return text;
        }
        string paths = string.Join("\n", _paths);
        return text.Length == 0 ? paths : text + "\n\n" + paths;
    }

    // Saves an image as PNG in the WSL folder and returns its WSL path; null when it cannot be written.
    private string? Save(BitmapSource image)
    {
        string name = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}.png";
        string wslPath = $"{_wslFolder}/{name}";
        try
        {
            string folder = _windowsPathOf(_wslFolder);
            Directory.CreateDirectory(folder);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using FileStream stream = File.Create(Path.Combine(folder, name));
            encoder.Save(stream);
            return wslPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    private void Redraw()
    {
        Strip.Children.Clear();
        foreach (string path in _paths)
        {
            Strip.Children.Add(Thumbnail(path));
        }
        Strip.Visibility = _paths.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        Changed?.Invoke();
    }

    private Grid Thumbnail(string path)
    {
        Grid grid = new() { Margin = new Thickness(0, 0, 6, 0), ToolTip = path };
        try
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(_windowsPathOf(path));
            bitmap.DecodePixelHeight = (int)ThumbnailHeight * 2;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            grid.Children.Add(new Image { Source = bitmap, Height = ThumbnailHeight });
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or UriFormatException)
        {
            grid.Children.Add(new TextBlock { Text = "[image]", VerticalAlignment = VerticalAlignment.Center, Height = ThumbnailHeight });
        }
        Button remove = new() { Content = "✕", Padding = new Thickness(4, 0, 4, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, ToolTip = "Retirer l'image" };
        remove.Click += (_, _) =>
        {
            _paths.Remove(path);
            Redraw();
        };
        grid.Children.Add(remove);
        return grid;
    }
}
