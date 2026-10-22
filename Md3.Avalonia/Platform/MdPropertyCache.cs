using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Concurrent;

namespace Md3.Avalonia.Platform;

/// <summary>
/// Caches <see cref="PropertyInfo"/> lookups by (type, property name) so hot paths
/// (table rows, list items, surface formatting) avoid reflection on every render.
/// </summary>
/// <remarks>
/// Trimming/AOT: model properties reached through <c>DisplayMemberPath</c> or
/// <c>MdDataColumn.PropertyName</c> are application types and must be rooted by the app.
/// Prefer <c>ItemTemplate</c>/<c>ItemFactory</c> and <c>ValueSelector</c> in trimmed apps.
/// </remarks>
internal static class MdPropertyCache
{
    private const int MaxEntries = 1024;
    private static readonly ConcurrentDictionary<PropertyKey, PropertyInfo?> Cache = new();

    public static PropertyInfo? Get(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = new PropertyKey(type, name);
        if (Cache.TryGetValue(key, out var cached)) return cached;
        var prop = type.GetProperty(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (Cache.Count < MaxEntries) Cache[key] = prop;
        return prop;
    }

    private readonly record struct PropertyKey(Type Type, string Name);
}
