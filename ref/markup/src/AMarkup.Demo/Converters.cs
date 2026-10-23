using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AMarkup.Demo;

/// <summary>
/// A reusable converter. Most one-off conversions in the demo use the shorter
/// <c>FuncValueConverter</c> form in <see cref="MainWindow"/> instead.
/// </summary>
public sealed class StringToUpperConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text ? text.ToUpper(culture) : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
