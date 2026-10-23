using System;
using Avalonia;

namespace AMarkup;

/// <summary>
/// Hot reload registration helpers.
/// </summary>
public static class MarkupHotReloadExtensions
{
    /// <summary>
    /// Registers <paramref name="target"/> so its markup is rebuilt after a hot reload update.
    /// </summary>
    public static T UseMarkupHotReload<T>(this T target, Action rebuild)
        where T : AvaloniaObject
    {
        MarkupHotReload.Register(target, rebuild);
        return target;
    }

    /// <summary>
    /// Enables AMarkup hot reload handling for this application builder.
    /// </summary>
    public static AppBuilder UseMarkupHotReload(this AppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        MarkupHotReload.Enable();
        return builder;
    }
}
