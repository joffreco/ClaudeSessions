using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ClaudeSessions.Conversations.Views;

/// <summary>A small ring with a gap that turns, in secondary color, the mark of a session at work.</summary>
internal static class Spinner
{
    private static readonly Duration Turn = new(TimeSpan.FromSeconds(1));

    /// <summary>Creates a spinner; it turns as long as it is shown.</summary>
    /// <param name="size">Its side, in device-independent pixels.</param>
    /// <returns>The spinner.</returns>
    public static FrameworkElement Create(double size)
    {
        double thickness = Math.Max(1.5, size / 7);
        double radius = (size - thickness) / 2;
        Point center = new(size / 2, size / 2);
        // Three quarters of a circle, from twelve o'clock clockwise to nine o'clock.
        PathFigure arc = new() { StartPoint = new Point(center.X, center.Y - radius), IsClosed = false };
        arc.Segments.Add(new ArcSegment(new Point(center.X - radius, center.Y), new Size(radius, radius), 0, true, SweepDirection.Clockwise, true));
        PathGeometry geometry = new([arc]);
        geometry.Freeze();
        RotateTransform rotation = new(0, size / 2, size / 2);
        Path ring = new()
        {
            Data = geometry,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = size,
            Height = size,
            RenderTransform = rotation,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ring.SetResourceReference(Shape.StrokeProperty, "TextFillColorSecondaryBrush");
        rotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, Turn) { RepeatBehavior = RepeatBehavior.Forever });
        return ring;
    }
}
