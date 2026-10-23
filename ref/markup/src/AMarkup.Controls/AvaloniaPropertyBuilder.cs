using System;
using System.Linq.Expressions;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AMarkup;

/// <summary>
/// Fluent builder used by generated markup methods to configure Avalonia property bindings.
/// </summary>
public sealed class AvaloniaPropertyBuilder<T> : IAvaloniaPropertyBuilder<T>
{
    private IValueConverter? _converter;
    private Func<IValueConverter?, BindingBase>? _bindingFactory;
    private Type? _sourceValueType;

    public IAvaloniaPropertyBuilder<T> Binding<TExpression, TSource>(
        TSource source,
        Expression<Func<TSource, TExpression>> getter,
        BindingMode? mode = null,
        IValueConverter? converter = null,
        string? stringFormat = null,
        object? fallbackValue = null)
    {
        _bindingFactory = effectiveConverter => CompiledBinding.Create(
            getter,
            source,
            effectiveConverter,
            mode ?? BindingMode.Default,
            fallbackValue: fallbackValue,
            stringFormat: stringFormat);
        _sourceValueType = typeof(TExpression);
        _converter = converter;
        return this;
    }

    public IAvaloniaPropertyBuilder<T> Binding<TExpression, TSource>(
        TSource source,
        Expression<Func<TSource, TExpression>> getter,
        BindingMode? mode,
        IValueConverter? converter,
        Func<TExpression, T> convertDelegate,
        string? stringFormat = null,
        object? fallbackValue = null)
    {
        _bindingFactory = effectiveConverter => CompiledBinding.Create(
            getter,
            source,
            effectiveConverter,
            mode ?? BindingMode.Default,
            fallbackValue: fallbackValue,
            stringFormat: stringFormat);
        _sourceValueType = typeof(TExpression);
        _converter = converter ?? new FuncValueConverter<TExpression, T>(value => convertDelegate(value!));
        return this;
    }

    public IAvaloniaPropertyBuilder<T> Convert<TExpression>(Func<TExpression, T> convertDelegate)
    {
        _sourceValueType = typeof(TExpression);
        _converter = new FuncValueConverter<TExpression, T>(value => convertDelegate(value!));
        return this;
    }

    public void SetBinding(AvaloniaObject element, AvaloniaProperty property, string path)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);

        IValueConverter? converter = _sourceValueType is null
            ? _converter
            : MarkupAutoConverter.GetEffectiveConverter(_sourceValueType, property.PropertyType, _converter);

        BindingBase binding = _bindingFactory is null
            ? new Binding { Path = path, Converter = converter }
            : _bindingFactory(converter);

        element.Bind(property, binding);
    }
}
