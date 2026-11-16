using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using ClaudeSessions.Core.Transcripts.Domain;

namespace ClaudeSessions.Conversations.Views;

/// <summary>
/// Draws a session's status line under the input, in the colors its command chose: one line of text per
/// row, the segments separated by a faint bar, and each percentage as a small pie, the main one of a
/// segment (such as the dumbometer's <c>Smart 7%</c>) larger. The exact value shows in the pie's tooltip.
/// </summary>
internal static class StatusLineView
{
    private const double GaugeSize = 11;
    private const double LabeledGaugeSize = 16;
    private const string DefaultBrushKey = "TextFillColorSecondaryBrush";

    /// <summary>Describes a status line as a string that changes whenever its drawing would.</summary>
    /// <param name="line">The status line, or null.</param>
    /// <returns>A signature of the line.</returns>
    public static string Signature(StatusLine? line)
    {
        if (line is null)
        {
            return "";
        }
        StringBuilder signature = new();
        foreach (StatusRow row in line.Rows)
        {
            foreach (StatusSegment segment in row.Segments)
            {
                foreach (StatusPart part in segment.Parts)
                {
                    signature.Append(part).Append('\u001f');
                }
                signature.Append('\u001e');
            }
            signature.Append('\n');
        }
        return signature.ToString();
    }

    /// <summary>Replaces what the host shows with a status line.</summary>
    /// <param name="host">Panel that receives one text block per row.</param>
    /// <param name="line">The status line.</param>
    public static void Fill(Panel host, StatusLine line)
    {
        host.Children.Clear();
        Brush fallback = host.TryFindResource(DefaultBrushKey) as Brush ?? Brushes.Gray;
        foreach (StatusRow row in line.Rows)
        {
            TextBlock text = new() { TextWrapping = TextWrapping.Wrap };
            text.SetResourceReference(TextBlock.ForegroundProperty, DefaultBrushKey);
            foreach ((StatusSegment segment, int index) in row.Segments.Select((segment, index) => (segment, index)))
            {
                if (index > 0)
                {
                    text.Inlines.Add(new Run("  |  ") { Foreground = Faint(fallback) });
                }
                foreach (StatusPart part in segment.Parts)
                {
                    text.Inlines.Add(InlineOf(part, fallback));
                }
            }
            host.Children.Add(text);
        }
    }

    private static Inline InlineOf(StatusPart part, Brush fallback)
    {
        return part switch
        {
            StatusGauge gauge => new InlineUIContainer(Pie(gauge, BrushOf(gauge.Color, fallback))) { BaselineAlignment = BaselineAlignment.Center },
            StatusText { Dim: true } text => new Run(text.Text) { Foreground = Faint(BrushOf(text.Color, fallback)) },
            StatusText text when text.Color is null => new Run(text.Text),
            StatusText text => new Run(text.Text) { Foreground = BrushOf(text.Color, fallback) },
            _ => new Run(),
        };
    }

    private static Brush BrushOf(StatusColor? color, Brush fallback)
    {
        if (color is not StatusColor rgb)
        {
            return fallback;
        }
        SolidColorBrush brush = new(Color.FromRgb(rgb.Red, rgb.Green, rgb.Blue));
        brush.Freeze();
        return brush;
    }

    private static Brush Faint(Brush brush)
    {
        Brush faint = brush.CloneCurrentValue();
        faint.Opacity = 0.55;
        faint.Freeze();
        return faint;
    }

    // A disc whose filled slice, from twelve o'clock clockwise, is the percentage.
    private static Grid Pie(StatusGauge gauge, Brush brush)
    {
        double size = gauge.Labeled ? LabeledGaugeSize : GaugeSize;
        double radius = size / 2;
        Grid pie = new() { Width = size, Height = size, Margin = new Thickness(1, 0, 1, 0), ToolTip = gauge.Text, Background = Brushes.Transparent };
        Brush track = brush.CloneCurrentValue();
        track.Opacity = 0.25;
        track.Freeze();
        pie.Children.Add(new Ellipse { Fill = track, Stroke = brush, StrokeThickness = 1 });
        if (gauge.Percent >= 100)
        {
            pie.Children.Add(new Ellipse { Fill = brush });
            return pie;
        }
        if (gauge.Percent <= 0)
        {
            return pie;
        }
        double angle = gauge.Percent / 100.0 * 2 * Math.PI;
        Point center = new(radius, radius);
        Point top = new(radius, 0);
        Point end = new(radius + (radius * Math.Sin(angle)), radius - (radius * Math.Cos(angle)));
        PathFigure slice = new() { StartPoint = center, IsClosed = true };
        slice.Segments.Add(new LineSegment(top, false));
        slice.Segments.Add(new ArcSegment(end, new Size(radius, radius), 0, gauge.Percent > 50, SweepDirection.Clockwise, false));
        PathGeometry geometry = new([slice]);
        geometry.Freeze();
        pie.Children.Add(new Path { Data = geometry, Fill = brush });
        return pie;
    }
}
