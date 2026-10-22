using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Mav.Theming;

namespace Mav.Platform;

/// <summary>
/// Keeps the platform system bars in sync with <see cref="MavTheme"/> and manages
/// the edge-to-edge preference on mobile hosts.
/// </summary>
/// <remarks>
/// The color follows <see cref="MavColorRole.Surface"/>; the icon appearance follows
/// <see cref="MavTheme.IsDark"/>. Edge-to-edge means the application draws behind the
/// status and navigation bars; <c>MavNavigationPage</c> consumes the matching
/// <see cref="IInsetsManager.SafeAreaPadding"/> so content stays inside the safe area.
/// </remarks>
public static class MavSystemBars
{
    /// <summary>
    /// Applies the current theme's surface color and bar icon appearance to
    /// <paramref name="topLevel"/>, or to the last known top level when omitted.
    /// Safe to call before a mobile window exists: it then does nothing.
    /// </summary>
    public static void Apply(TopLevel? topLevel = null)
    {
        topLevel ??= MavOverlay.ResolveTopLevel(null);
        if (topLevel?.InsetsManager is not { } insets)
            return;

        var theme = MavTheme.Current;

        try
        {
            insets.SystemBarColor = theme.Color.Surface;
        }
        catch (Exception)
        {
            // Some platforms reject color changes before the native window exists.
        }

        ApplyIconTheme(insets, theme.IsDark);
    }

    /// <summary>
    /// Enables edge-to-edge rendering (transparent system bars over the app surface)
    /// and applies the current theme to <paramref name="topLevel"/>.
    /// </summary>
    public static void EnableEdgeToEdge(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);

        if (topLevel.InsetsManager is { } insets)
        {
            try
            {
                insets.DisplayEdgeToEdgePreference = true;
            }
            catch (Exception)
            {
                // Older platforms without edge-to-edge support keep the default layout.
            }
        }

        Apply(topLevel);
    }

    /// <summary>
    /// Attaches system-bar synchronization to <paramref name="root"/>: applies the
    /// theme when the control enters the visual tree and again on every theme change,
    /// then stops when the control is detached or the returned handle is disposed.
    /// </summary>
    /// <param name="root">Usually the <c>MavNavigationPage</c> returned by the mobile host.</param>
    /// <param name="edgeToEdge">
    /// When true (default) the app draws behind the system bars and relies on safe-area
    /// insets. Set to false to keep the platform's default layout with opaque bars.
    /// </param>
    public static IDisposable Attach(Control root, bool edgeToEdge = true)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new Attachment(root, edgeToEdge);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "SystemBarTheme is a property of Avalonia's Android insets manager; " +
                        "the platform assembly is not trimmed by this library.")]
    private static void ApplyIconTheme(IInsetsManager insets, bool dark)
    {
        // SystemBarTheme is Android-specific and not exposed by IInsetsManager, so the
        // property is set through its public runtime surface. Avalonia itself also maps
        // RequestedThemeVariant to this property; doing it here keeps the bars correct
        // when only the seed changes.
        var property = insets.GetType().GetProperty(
            "SystemBarTheme",
            BindingFlags.Instance | BindingFlags.Public);

        if (property is null || !property.CanWrite || !property.PropertyType.IsEnum)
            return;

        // Light = light bar background with dark icons; Dark = dark bars with light icons.
        var name = dark ? "Dark" : "Light";
        if (!Enum.TryParse(property.PropertyType, name, out var value))
            return;

        try
        {
            property.SetValue(insets, value);
        }
        catch (Exception)
        {
            // The platform may not have a window controller yet.
        }
    }

    private sealed class Attachment : IDisposable
    {
        private readonly Control _root;
        private readonly bool _edgeToEdge;
        private bool _themeSubscribed;
        private bool _disposed;

        /// <summary>
        /// Attaches the system bar handling to the root control.
        /// </summary>
        internal Attachment(Control root, bool edgeToEdge)
        {
            _root = root;
            _edgeToEdge = edgeToEdge;

            _root.AttachedToVisualTree += OnAttached;
            _root.DetachedFromVisualTree += OnDetached;

            if (TopLevel.GetTopLevel(_root) is not null)
                AttachToTopLevel();
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => AttachToTopLevel();

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => DetachTheme();

        private void AttachToTopLevel()
        {
            if (_disposed)
                return;

            if (!_themeSubscribed)
            {
                MavTheme.Changed += OnThemeChanged;
                _themeSubscribed = true;
            }

            if (TopLevel.GetTopLevel(_root) is not { } topLevel)
                return;

            ApplyTo(topLevel);

            // The native window may not have its final attributes during the first
            // attach (the activity is still being created). Re-apply once the layout
            // and the window are ready, otherwise Android keeps the default bar color.
            Dispatcher.UIThread.Post(() =>
            {
                if (_disposed)
                    return;
                if (TopLevel.GetTopLevel(_root) is { } current)
                    ApplyTo(current);
            }, DispatcherPriority.Loaded);
        }

        private void ApplyTo(TopLevel topLevel)
        {
            if (_edgeToEdge)
                EnableEdgeToEdge(topLevel);
            else
                Apply(topLevel);
        }

        private void OnThemeChanged(object? sender, MavTheme theme)
        {
            if (_disposed)
                return;

            var topLevel = TopLevel.GetTopLevel(_root);
            if (topLevel is null)
                return;

            ApplyTo(topLevel);
        }

        private void DetachTheme()
        {
            if (!_themeSubscribed)
                return;

            MavTheme.Changed -= OnThemeChanged;
            _themeSubscribed = false;
        }

        /// <summary>
        /// Detaches the system bar handling from the root control.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _root.AttachedToVisualTree -= OnAttached;
            _root.DetachedFromVisualTree -= OnDetached;
            DetachTheme();
        }
    }
}
