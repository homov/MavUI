using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace Mav.ControlsGallery.Pages;

/// <summary>
/// Lays children out in equal columns no narrower than <see cref="MinItemWidth"/>. Each child fills its
/// column, so a card that wraps onto its own row takes the whole width of the page instead of keeping a
/// fixed width at the left edge.
/// </summary>
public class AdaptiveWrap : Panel
{
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<AdaptiveWrap, double>(nameof(MinItemWidth), 280);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<AdaptiveWrap, double>(nameof(Spacing), 8);

    static AdaptiveWrap()
    {
        AffectsMeasure<AdaptiveWrap>(MinItemWidthProperty, SpacingProperty);
    }

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private (int Columns, double ItemWidth) ColumnsFor(double width)
    {
        var min = Math.Max(1, MinItemWidth);
        if (!double.IsFinite(width) || width <= 0)
            return (1, min);

        var columns = Math.Max(1, (int)Math.Floor((width + Spacing) / (min + Spacing)));
        var itemWidth = Math.Max(0, (width - Spacing * (columns - 1)) / columns);
        return (columns, itemWidth);
    }

    private List<double> RowHeights(int columns)
    {
        var rows = new List<double>();
        for (var i = 0; i < Children.Count; i++)
        {
            var row = i / columns;
            if (rows.Count <= row)
                rows.Add(0);
            rows[row] = Math.Max(rows[row], Children[i].DesiredSize.Height);
        }
        return rows;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var (columns, itemWidth) = ColumnsFor(availableSize.Width);
        foreach (var child in Children)
            child.Measure(new Size(itemWidth, double.PositiveInfinity));

        var rows = RowHeights(columns);
        var height = 0.0;
        foreach (var rowHeight in rows)
            height += rowHeight;
        height += Spacing * Math.Max(0, rows.Count - 1);

        var width = double.IsFinite(availableSize.Width)
            ? availableSize.Width
            : columns * itemWidth + Spacing * (columns - 1);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (columns, itemWidth) = ColumnsFor(finalSize.Width);
        var rows = RowHeights(columns);
        var tops = new double[rows.Count];
        var y = 0.0;
        for (var r = 0; r < rows.Count; r++)
        {
            tops[r] = y;
            y += rows[r] + Spacing;
        }

        for (var i = 0; i < Children.Count; i++)
        {
            var row = i / columns;
            var column = i % columns;
            var x = column * (itemWidth + Spacing);
            Children[i].Arrange(new Rect(x, tops[row], itemWidth, rows[row]));
        }

        return finalSize;
    }
}
