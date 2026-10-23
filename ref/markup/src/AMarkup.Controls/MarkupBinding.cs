using System;
using System.Linq.Expressions;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AMarkup;

/// <summary>
/// Creates bindings and converts string values used by generated markup methods.
/// </summary>
public static class MarkupBinding
{
    /// <summary>
    /// Converts a value to the requested Avalonia type when a well-known parser exists.
    /// </summary>
    public static object? ParseValue(Type targetType, object? value)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (value is null)
        {
            return null;
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (value is not string text)
        {
            return value;
        }

        if (targetType == typeof(Color))
        {
            return Color.Parse(text);
        }

        if (targetType == typeof(Geometry))
        {
            return Geometry.Parse(text);
        }

        if (targetType == typeof(CornerRadius))
        {
            return CornerRadius.Parse(text);
        }

        if (targetType == typeof(Thickness))
        {
            return Thickness.Parse(text);
        }

        if (targetType == typeof(FontFamily))
        {
            return new FontFamily(text);
        }

        return value;
    }

    /// <summary>
    /// Creates a compiled binding from a typed expression and an explicit source.
    /// When <paramref name="targetProperty"/> is supplied, common primitive and
    /// enum mismatches are converted automatically.
    /// </summary>
    public static CompiledBinding FromExpression<TSource, TValue>(
        TSource source,
        Expression<Func<TSource, TValue>> getter,
        BindingMode? mode = null,
        IValueConverter? converter = null,
        AvaloniaProperty? targetProperty = null,
        string? stringFormat = null,
        object? fallbackValue = null)
    {
        ArgumentNullException.ThrowIfNull(getter);

        return CompiledBinding.Create(
            getter,
            source,
            MarkupAutoConverter.GetEffectiveConverter(typeof(TValue), targetProperty?.PropertyType, converter),
            mode ?? BindingMode.Default,
            fallbackValue: fallbackValue,
            stringFormat: stringFormat);
    }

    /// <summary>
    /// Extracts a property path from a member-access lambda expression for manual
    /// reflection-based bindings. Generated expression bindings use
    /// <see cref="CompiledBinding.Create{TIn, TOut}"/> instead.
    /// </summary>
    public static string GetPropertyPath(LambdaExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var parts = new System.Collections.Generic.Stack<string>();
        Expression? current = expression.Body;

        while (current is not null)
        {
            switch (current)
            {
                case MemberExpression member:
                    parts.Push(member.Member.Name);
                    current = member.Expression;
                    break;
                case UnaryExpression unary:
                    current = unary.Operand;
                    break;
                default:
                    current = null;
                    break;
            }
        }

        return string.Join(".", parts);
    }
}
