using System;
using Avalonia;
using Avalonia.Controls;

namespace AMarkup;

/// <summary>
/// Coordinates lazy markup construction and hot reload for a control.
/// </summary>
/// <remarks>
/// The lifecycle builds the control tree when the owner is attached to a visual tree
/// (or opened, for <see cref="Window"/>), registers the owner for hot reload, and
/// rebuilds the tree when the runtime applies an update.
/// </remarks>
public sealed class MarkupViewLifecycle
{
    private readonly Control _owner;
    private readonly Func<Control> _build;
    private readonly Action<Control?> _setContent;
    private readonly bool _rebuildOnReattach;
    private bool _initialized;
    private bool _reloading;

    /// <summary>
    /// Initializes a new lifecycle for <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The control whose content is managed by this lifecycle.</param>
    /// <param name="build">Creates a fresh control tree.</param>
    /// <param name="setContent">Assigns or clears the owner's content.</param>
    /// <param name="rebuildOnReattach">
    /// When <see langword="true"/>, the content is rebuilt after the owner is detached
    /// and attached again. This guarantees fresh markup if hot reload happened while
    /// the owner was detached, at the cost of rebuilding the tree on every reattach.
    /// </param>
    public MarkupViewLifecycle(
        Control owner,
        Func<Control> build,
        Action<Control?> setContent,
        bool rebuildOnReattach = false)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _setContent = setContent ?? throw new ArgumentNullException(nameof(setContent));
        _rebuildOnReattach = rebuildOnReattach;

        if (owner is Window window)
        {
            // A Window is a root control and does not use the normal visual-tree
            // attach/detach lifecycle of child controls.
            window.Opened += OnWindowOpened;
            window.Closed += OnWindowClosed;
        }
        else
        {
            owner.AttachedToVisualTree += OnAttached;
            owner.DetachedFromVisualTree += OnDetached;
        }
    }

    /// <summary>
    /// Gets whether the content has been built.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Builds the content if it has not been built yet.
    /// </summary>
    /// <remarks>
    /// This is normally called automatically when the owner is attached to a visual
    /// tree or opened. It can also be called explicitly when immediate initialization
    /// is required.
    /// </remarks>
    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            _setContent(_build());
        }
        catch
        {
            _initialized = false;
            throw;
        }
    }

    /// <summary>
    /// Clears the current content and rebuilds it.
    /// </summary>
    public void Reload()
    {
        if (_reloading)
        {
            return;
        }

        _reloading = true;

        try
        {
            _setContent(null);
            _initialized = false;
            Initialize();

            _owner.InvalidateMeasure();
            _owner.InvalidateArrange();
            _owner.InvalidateVisual();
        }
        finally
        {
            _reloading = false;
        }
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        MarkupHotReload.Register(_owner, Reload);
        Initialize();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        MarkupHotReload.Unregister(_owner);

        if (_rebuildOnReattach)
        {
            _initialized = false;
        }
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        MarkupHotReload.Register(_owner, Reload);
        Initialize();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        MarkupHotReload.Unregister(_owner);
    }
}
