using System;
using System.Linq.Expressions;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AMarkup;

/// <summary>
/// Configures a binding for an Avalonia property.
/// </summary>
public interface IAvaloniaPropertyBuilder<T>
{
    /// <summary>
    /// Binds from a typed expression with an explicit source.
    /// </summary>
    IAvaloniaPropertyBuilder<T> Binding<TExpression, TSource>(
        TSource source,
        Expression<Func<TSource, TExpression>> getter,
        BindingMode? mode = null,
        IValueConverter? converter = null,
        string? stringFormat = null,
        object? fallbackValue = null);

    /// <summary>
    /// Binds from a typed expression with an explicit source and value converter.
    /// </summary>
    IAvaloniaPropertyBuilder<T> Binding<TExpression, TSource>(
        TSource source,
        Expression<Func<TSource, TExpression>> getter,
        BindingMode? mode,
        IValueConverter? converter,
        Func<TExpression, T> convertDelegate,
        string? stringFormat = null,
        object? fallbackValue = null);

    /// <summary>
    /// Adds a value converter to the current binding.
    /// </summary>
    IAvaloniaPropertyBuilder<T> Convert<TExpression>(Func<TExpression, T> convertDelegate);
}
