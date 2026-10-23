using System;

namespace Avalonia.Controls;

/// <summary>
/// Shorthand helpers for defining <see cref="Grid"/> rows and columns with
/// AXAML-style definition strings.
/// </summary>
public static class GridMarkupExtensions
{
    /// <summary>
    /// Sets the grid columns from an AXAML-style definition string, for example
    /// <c>"Auto, *, 2*"</c> or <c>"120, 1*, 2*"</c>.
    /// </summary>
    public static T Cols<T>(this T grid, string definitions)
        where T : Grid
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(definitions);

        grid.ColumnDefinitions = ColumnDefinitions.Parse(definitions);
        return grid;
    }

    /// <summary>
    /// Sets the grid rows from an AXAML-style definition string, for example
    /// <c>"Auto, *"</c> or <c>"40, 2*, Auto"</c>.
    /// </summary>
    public static T Rows<T>(this T grid, string definitions)
        where T : Grid
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(definitions);

        grid.RowDefinitions = RowDefinitions.Parse(definitions);
        return grid;
    }
}
