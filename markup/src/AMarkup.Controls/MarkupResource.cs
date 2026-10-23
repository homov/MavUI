using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;

namespace AMarkup;

/// <summary>
/// Helpers for reading Avalonia resources and for creating dynamic resource bindings.
/// </summary>
public static class MarkupResource
{
    /// <summary>
    /// Creates an <see cref="DynamicResourceExtension"/> for the supplied resource key.
    /// </summary>
    public static DynamicResourceExtension Dynamic(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new DynamicResourceExtension(key);
    }

    /// <summary>
    /// Alias for <see cref="Dynamic(object)"/>.
    /// </summary>
    public static DynamicResourceExtension DynamicResource(object key) => Dynamic(key);

    /// <summary>
    /// Adds a dynamic resource binding for the specified property.
    /// </summary>
    public static T DynamicResource<T>(
        this T element,
        AvaloniaProperty property,
        object key)
        where T : AvaloniaObject
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);
        element.Bind(property, Dynamic(key));
        return element;
    }

    /// <summary>
    /// Adds a dynamic resource setter to a typed style.
    /// </summary>
    public static Style<TControl> DynamicResource<TControl>(
        this Style<TControl> style,
        AvaloniaProperty property,
        object key)
        where TControl : StyledElement
    {
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(property);
        return style.Setter(property, Dynamic(key));
    }

    /// <summary>
    /// Finds a resource and casts it to <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// The resource was not found or its value is not compatible with <typeparamref name="T"/>.
    /// </exception>
    public static T FindResource<T>(this IResourceHost host, object key)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(key);

        if (host.TryFindResource<T>(key, out T? value))
        {
            return value;
        }

        throw new KeyNotFoundException(
            $"Resource '{key}' was not found or is not compatible with '{typeof(T).FullName}'.");
    }

    /// <summary>
    /// Finds a resource without throwing when it is missing or has a different type.
    /// </summary>
    public static bool TryFindResource<T>(
        this IResourceHost host,
        object key,
        [NotNullWhen(true)] out T? value)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(key);

        if (ResourceNodeExtensions.TryFindResource(host, key, out object? resource)
            && resource is T typedResource)
        {
            value = typedResource;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Finds a resource or returns <paramref name="defaultValue"/> when it is missing.
    /// </summary>
    public static T? FindResourceOrDefault<T>(
        this IResourceHost host,
        object key,
        T? defaultValue = default)
    {
        return host.TryFindResource<T>(key, out T? value) ? value : defaultValue;
    }
}
