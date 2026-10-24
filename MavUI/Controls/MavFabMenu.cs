using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Expressive FAB Speed-Dial Menu that expands labeled action pills upward from a primary FAB.
/// </summary>
public class MavFabMenu : MavContentView, IEnumerable<MavFabMenuItem>
{
    /// <summary>Distance between the FAB and the bottom pill of the open menu.</summary>
    private const double FabGap = 12;

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavFabMenu, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> ClosedIconProperty =
        AvaloniaProperty.Register<MavFabMenu, string>(nameof(ClosedIcon), MavIcons.Add);

    public static readonly StyledProperty<string> OpenIconProperty =
        AvaloniaProperty.Register<MavFabMenu, string>(nameof(OpenIcon), MavIcons.Close);

    public static readonly StyledProperty<MavFabColor> ColorSchemeProperty =
        AvaloniaProperty.Register<MavFabMenu, MavFabColor>(nameof(ColorScheme), MavFabColor.Primary);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavFabMenu, IEnumerable?>(nameof(ItemsSource));

    private readonly AvaloniaList<MavFabMenuItem> _items = new();
    private readonly MavItemEventWiring<MavFabMenuItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly StackPanel _itemsHost;
    private readonly MavInkSurface _trigger;
    private readonly MavIcon _triggerIcon;
    private IDisposable? _overlay;
    private string _closedIcon = MavIcons.Add;
    private string _openIcon = MavIcons.Close;
    private bool _isOpen;
    private bool _pendingOpen;

    public event EventHandler<int>? ItemClicked;
    public event EventHandler<bool>? OpenChanged;

    static MavFabMenu()
    {
        IsOpenProperty.Changed.AddClassHandler<MavFabMenu>((m, e) =>
        {
            var val = e.GetNewValue<bool>();
            if (m._isOpen == val) return;
            m._isOpen = val;
            m.AnimateOpen(val);
            m.OpenChanged?.Invoke(m, val);
        });
        ClosedIconProperty.Changed.AddClassHandler<MavFabMenu>((m, e) =>
        {
            m._closedIcon = e.GetNewValue<string>() ?? MavIcons.Add;
            if (!m._isOpen) m._triggerIcon.Glyph = m._closedIcon;
        });
        OpenIconProperty.Changed.AddClassHandler<MavFabMenu>((m, e) =>
        {
            m._openIcon = e.GetNewValue<string>() ?? MavIcons.Close;
            if (m._isOpen) m._triggerIcon.Glyph = m._openIcon;
        });
        ItemsSourceProperty.Changed.AddClassHandler<MavFabMenu>((m, e) =>
            m.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    /// <summary>Creates a floating action menu with the add icon.</summary>
    public MavFabMenu() : this(MavIcons.Add)
    {
    }

    /// <summary>Creates a floating action menu with the given icon.</summary>
    public MavFabMenu(string icon = MavIcons.Add)
    {
        _closedIcon = icon;
        ClosedIcon = icon;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _items.CollectionChanged += (_, e) =>
        {
            _wiredItems.HandleCollectionChanged(e);
            foreach (var item in _items)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
                _wiredItems.Add(item, (_, _) => RebuildItems());
            }
            RebuildItems();
        };

        // The pills live in the overlay while the menu is open (see ShowItems), so they never
        // take part in this control's layout: opening the menu must not move the FAB.
        _itemsHost = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            IsVisible = false,
        };

        _triggerIcon = new MavIcon(_closedIcon, 24) { UseThemeDefaultColor = false };
        _trigger = new MavInkSurface
        {
            Width = 56,
            Height = 56,
            CornerRadius = 16,
            Elevation = 3,
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = _triggerIcon,
        };
        _trigger.Clicked += (_, _) => Toggle();

        Child = _trigger;

        // IsOpen can be set before the menu is on screen (a binding, a restored view); the overlay
        // needs a top level, so opening waits for attach.
        AttachedToVisualTree += (_, _) =>
        {
            if (_isOpen && _overlay is null)
                ShowItems();
        };
        DetachedFromVisualTree += (_, _) => DisposeOverlay();

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string ClosedIcon
    {
        get => GetValue(ClosedIconProperty);
        set => SetValue(ClosedIconProperty, value);
    }

    public string Icon
    {
        get => ClosedIcon;
        set => ClosedIcon = value;
    }

    public string OpenIcon
    {
        get => GetValue(OpenIconProperty);
        set => SetValue(OpenIconProperty, value);
    }

    public MavFabColor ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _items.Clear();
        foreach (var item in src)
        {
            if (item is MavFabMenuItem fmi)
                _items.Add(fmi);
            else if (item is not null)
                _items.Add(new MavFabMenuItem(item.ToString() ?? string.Empty, MavIcons.Add));
        }
    }

    [Content]
    public AvaloniaList<MavFabMenuItem> Items => _items;

    /// <summary>Adds an item and returns the menu for chaining.</summary>
    public MavFabMenu AddItem(string label, string icon, Action? onClick = null)
    {
        _items.Add(new MavFabMenuItem(label, icon, onClick));
        return this;
    }

    /// <summary>
    /// Adds an action item to the menu.
    /// </summary>
    public void Add(MavFabMenuItem item) => _items.Add(item);

    /// <summary>
    /// Returns an enumerator over the menu items.
    /// </summary>
    public IEnumerator<MavFabMenuItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The pill container while the menu is open. Exposed for tests.</summary>
    internal StackPanel ItemsHost => _itemsHost;

    /// <summary>
    /// Opens the menu if it is closed, or closes it if it is open.
    /// </summary>
    public void Toggle() => IsOpen = !IsOpen;
    /// <summary>
    /// Opens the menu.
    /// </summary>
    public void Open() => IsOpen = true;
    /// <summary>
    /// Closes the menu.
    /// </summary>
    public void Close() => IsOpen = false;

    private void RebuildItems()
    {
        _itemsHost.Children.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            var idx = i;
            var item = _items[i];
            var pill = BuildItemPill(item, idx);
            _itemsHost.Children.Add(pill);
        }
        ApplyTheme(MavTheme.Current);
    }

    private MavInkSurface BuildItemPill(MavFabMenuItem item, int index)
    {
        var icon = new MavIcon(item.Icon, 20) { UseThemeDefaultColor = false };
        var label = new TextBlock
        {
            Text = item.Label,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(icon);
        row.Children.Add(label);

        var pill = new MavInkSurface
        {
            MinHeight = 44,
            CornerRadius = 22,
            Padding = new Thickness(16, 0, 20, 0),
            Elevation = 2,
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = row,
        };
        pill.Clicked += (_, _) =>
        {
            IsOpen = false;
            item.OnClick?.Invoke();
            item.RaiseClicked();
            if (item.Command is { } cmd && cmd.CanExecute(item.CommandParameter))
                cmd.Execute(item.CommandParameter);
            ItemClicked?.Invoke(this, index);
        };
        return pill;
    }

    private void AnimateOpen(bool open)
    {
        _triggerIcon.Glyph = open ? _openIcon : _closedIcon;
        MavMotion.Animate(this, "fab_rot", _triggerIcon.GetRotation(), open ? 90 : 0, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _triggerIcon.SetRotation(v);
        });
        ApplyTheme(MavTheme.Current);

        if (open)
            ShowItems();
        else
            HideItems();
    }

    /// <summary>
    /// Shows the action pills above the FAB in the overlay. They are not part of the menu's own
    /// layout: with them in a vertical stack above the trigger, opening the menu made the control
    /// taller, and a parent that lays children from the top (a wrap panel, a grid cell) pushed the
    /// FAB down while the pills appeared over where it used to be.
    /// </summary>
    private void ShowItems()
    {
        if (_overlay is not null) return;

        if (MavOverlay.EnsureOverlayRoot(_trigger) is null)
        {
            _pendingOpen = true;
            return;
        }

        _pendingOpen = false;
        RebuildItems();

        _itemsHost.IsVisible = true;
        _itemsHost.Opacity = 0;
        _itemsHost.HorizontalAlignment = HorizontalAlignment.Left;
        _itemsHost.VerticalAlignment = VerticalAlignment.Top;
        _itemsHost.SetTranslationY(12);
        PlaceItems();

        IDisposable? handle = null;
        handle = MavOverlay.Show(
            _trigger,
            _itemsHost,
            scrimColor: Colors.Transparent,
            onScrimTap: () => IsOpen = false,
            onViewportChanged: PlaceItems,
            // The overlay can also close on its own (its page is removed, the top level closes).
            // Reset the fields the next Open relies on, but only for this handle.
            onClosed: () =>
            {
                if (!ReferenceEquals(_overlay, handle))
                    return;

                _overlay = null;
                _itemsHost.IsVisible = false;
                _itemsHost.Opacity = 1;
                _itemsHost.SetTranslationY(0);
            });
        _overlay = handle;

        MavMotion.Animate(this, "fab_items", 0, 1, MavMotion.Medium1, MavMotion.CubicOut, v =>
        {
            _itemsHost.Opacity = v;
            _itemsHost.SetTranslationY(12 * (1 - v));
        });
    }

    /// <summary>Fades the pills out and releases the overlay.</summary>
    private void HideItems()
    {
        if (_overlay is null)
        {
            _pendingOpen = false;
            return;
        }

        var handle = _overlay;
        _overlay = null;
        MavMotion.Animate(this, "fab_items", _itemsHost.Opacity, 0, MavMotion.Short3, MavMotion.CubicIn, v =>
        {
            _itemsHost.Opacity = v;
            _itemsHost.SetTranslationY(8 * (1 - v));
        }, () =>
        {
            handle.Dispose();
            _itemsHost.IsVisible = false;
            _itemsHost.Opacity = 1;
            _itemsHost.SetTranslationY(0);
        });
    }

    /// <summary>Closes the overlay without waiting for the fade (page teardown).</summary>
    private void DisposeOverlay()
    {
        _overlay?.Dispose();
        _overlay = null;
        _pendingOpen = false;
        _itemsHost.IsVisible = false;
        _itemsHost.Opacity = 1;
        _itemsHost.SetTranslationY(0);
    }

    /// <summary>
    /// Puts the pill stack just above the trigger, right edges aligned, clamped to the safe area.
    /// Re-run when the viewport changes (rotation, keyboard) so an open menu stays on screen.
    /// </summary>
    private void PlaceItems()
    {
        var overlayRoot = MavOverlay.EnsureOverlayRoot(_trigger);
        if (overlayRoot is null) return;

        var bounds = MavOverlay.BoundsInOverlay(_trigger, overlayRoot);
        var overlaySize = MavOverlay.GetOverlaySize(_trigger);
        var insets = MavOverlay.SafeAreaOf(_trigger);

        var availableWidth = Math.Max(80, overlaySize.Width - insets.Left - insets.Right - 16);
        _itemsHost.Measure(new Size(availableWidth, double.PositiveInfinity));
        var desired = _itemsHost.DesiredSize;

        var left = Math.Max(insets.Left + 8, bounds.Right - desired.Width);
        var top = Math.Max(insets.Top + 8, bounds.Y - desired.Height - FabGap);
        _itemsHost.Margin = new Thickness(left, top, 0, 0);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _trigger.FillColor = _isOpen ? c.Primary : c.PrimaryContainer;
        _trigger.RippleColor = _isOpen ? c.OnPrimary : c.OnPrimaryContainer;
        _trigger.CornerRadius = _isOpen ? 28 : 16;
        _triggerIcon.Color = _isOpen ? c.OnPrimary : c.OnPrimaryContainer;

        foreach (var child in _itemsHost.Children.OfType<MavInkSurface>())
        {
            child.FillColor = c.SecondaryContainer;
            child.RippleColor = c.OnSecondaryContainer;
            if (child.Child is StackPanel row)
            {
                foreach (var el in row.Children)
                {
                    if (el is MavIcon ic) ic.Color = c.OnSecondaryContainer;
                    else if (el is TextBlock lbl)
                    {
                        lbl.Apply(MavTypeRole.LabelLarge, t.Typography);
                        lbl.Foreground = MavTheme.BrushOf(c.OnSecondaryContainer);
                    }
                }
            }
        }
    }
}
