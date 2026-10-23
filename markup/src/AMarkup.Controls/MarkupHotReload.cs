using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Threading;

namespace AMarkup;

/// <summary>
/// Rebuilds registered controls when the runtime applies a hot reload update.
/// </summary>
public static class MarkupHotReload
{
    private static readonly ConditionalWeakTable<AvaloniaObject, ReloadRegistration> Registrations = new();
    private static bool _enabled;

    /// <summary>
    /// Enables hot reload handling. Registration also enables it automatically.
    /// </summary>
    public static void Enable() => _enabled = true;

    /// <summary>
    /// Registers a control and the callback that rebuilds its markup.
    /// </summary>
    public static void Register(AvaloniaObject target, Action rebuild)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(rebuild);

        Enable();
        Registrations.Remove(target);
        Registrations.Add(target, new ReloadRegistration(rebuild));
    }

    /// <summary>
    /// Removes a previous registration.
    /// </summary>
    public static void Unregister(AvaloniaObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Registrations.Remove(target);
    }

    /// <summary>
    /// Called by the runtime before metadata is updated.
    /// </summary>
    public static void ClearCache(Type[]? updatedTypes)
    {
    }

    /// <summary>
    /// Called by the runtime after a hot reload update has been applied.
    /// </summary>
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        if (!_enabled)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            ReloadAll();
            return;
        }

        Dispatcher.UIThread.Post(ReloadAll);
    }

    private static void ReloadAll()
    {
        var reloads = new List<Action>();

        foreach (KeyValuePair<AvaloniaObject, ReloadRegistration> registration in Registrations)
        {
            AvaloniaObject target = registration.Key;

            if (target is IMarkupReloadable reloadable)
            {
                reloads.Add(reloadable.ReloadMarkup);
            }
            else
            {
                reloads.Add(registration.Value.Callback);
            }
        }

        foreach (Action reload in reloads)
        {
            try
            {
                reload();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"AMarkup hot reload failed: {exception}");
            }
        }
    }

    private sealed record ReloadRegistration(Action Callback);
}
