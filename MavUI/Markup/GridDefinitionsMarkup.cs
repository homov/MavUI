using System;
using System.Globalization;
using Avalonia.Controls;

namespace Mav.Markup;

/// <summary>
/// Row and column definitions of a <see cref="Grid"/> written as a short string, for example
/// <c>new Grid().RowDefinitions("Auto,*").ColumnDefinitions("2*,100")</c>.
/// Each item is <c>Auto</c>, <c>*</c>, a star weight such as <c>2*</c>, or a pixel size such as <c>100</c>.
/// </summary>
public static class GridDefinitionsMarkup
{
    /// <summary>Adds row definitions from a comma-separated list of sizes.</summary>
    public static Grid RowDefinitions(this Grid grid, string definitions)
    {
        foreach (var length in Parse(definitions))
            grid.RowDefinitions.Add(new RowDefinition { Height = length });
        return grid;
    }

    /// <summary>Adds column definitions from a comma-separated list of sizes.</summary>
    public static Grid ColumnDefinitions(this Grid grid, string definitions)
    {
        foreach (var length in Parse(definitions))
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = length });
        return grid;
    }

    private static GridLength[] Parse(string definitions)
    {
        var parts = definitions.Split(',');
        var result = new GridLength[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            result[i] = ParseOne(parts[i].Trim());
        return result;
    }

    private static GridLength ParseOne(string item)
    {
        if (item.Length == 0)
            throw new FormatException("Empty grid definition.");

        if (string.Equals(item, "Auto", StringComparison.OrdinalIgnoreCase))
            return new GridLength(1, GridUnitType.Auto);

        if (item == "*")
            return new GridLength(1, GridUnitType.Star);

        if (item.EndsWith("*", StringComparison.Ordinal))
        {
            var weight = double.Parse(item.Substring(0, item.Length - 1), CultureInfo.InvariantCulture);
            return new GridLength(weight, GridUnitType.Star);
        }

        return new GridLength(double.Parse(item, CultureInfo.InvariantCulture), GridUnitType.Pixel);
    }
}
