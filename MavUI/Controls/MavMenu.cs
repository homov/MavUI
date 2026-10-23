using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 anchored popup menu with icons, keyboard shortcuts, checkmarks, dividers, and submenus.
/// </summary>
public class MavMenu : Control, IEnumerable<MavMenuItem>
{
    private readonly AvaloniaList<MavMenuItem> _items = new();
    private IDisposable? _overlayHandle;
    private IDisposable? _subOverlayHandle;
    private Control? _anchor;
    private MavSurface? _card;
    private Control? _subAnchor;
    private MavSurface? _subCard;
    private int _subItemCount;

    public event EventHandler<MavMenuItem>? ItemSelected;
    public event EventHandler? Closed;

    /// <summary>Creates an empty menu.</summary>
    public MavMenu()
    {
        MinWidth = 180;
        MaxHeight = 320;
        _items.CollectionChanged += (_, _) =>
        {
            foreach (var item in _items)
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
        };
    }

    [Content]
    public AvaloniaList<MavMenuItem> Items => _items;

    /// <summary>
    /// Measures the menu during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>
    /// Adds an item to the menu.
    /// </summary>
    public void Add(MavMenuItem item) => _items.Add(item);

    /// <summary>
    /// Returns an enumerator over the menu items.
    /// </summary>
    public IEnumerator<MavMenuItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Adds an action entry and returns the menu for chaining.</summary>
    public MavMenu AddItem(
        string text,
        string? leadingIcon = null,
        string? trailingText = null,
        Action? onSelected = null,
        bool isEnabled = true,
        bool isSelected = false)
    {
        _items.Add(new MavMenuItem(text, leadingIcon, trailingText, onSelected, isEnabled, isSelected));
        return this;
    }

    /// <summary>Adds an entry that opens a nested menu built by <paramref name="buildSubmenu"/>.</summary>
    public MavMenu AddSubmenu(string text, Action<MavMenu> buildSubmenu, string? leadingIcon = null)
    {
        var sub = new MavMenu();
        buildSubmenu(sub);
        _items.Add(new MavMenuItem(text, leadingIcon)
        {
            SubItems = sub._items.ToList()
        });
        return this;
    }

    /// <summary>Adds a divider line between entries.</summary>
    public MavMenu AddDivider()
    {
        _items.Add(new MavMenuItem(string.Empty) { IsDivider = true });
        return this;
    }

    /// <summary>
    /// Removes all items from the menu.
    /// </summary>
    public void Clear() => _items.Clear();

    /// <summary>Opens the menu next to <paramref name="anchor"/>.</summary>
    public void Show(Control anchor)
    {
        Close();

        var overlayRoot = MavOverlay.EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return;

        var anchorBounds = MavOverlay.BoundsInOverlay(anchor, overlayRoot);
        var overlaySize = MavOverlay.GetOverlaySize(anchor);

        _anchor = anchor;
        _card = BuildMenuCard(_items, overlaySize);
        PositionCard(_card, anchorBounds, overlaySize, isSubmenu: false, _items.Count);

        _overlayHandle = MavOverlay.Show(
            anchor,
            _card,
            scrimColor: Colors.Transparent,
            onScrimTap: Close,
            onViewportChanged: Reflow);

        MavMotion.ScaleIn(_card, 0.92, MavMotion.Short3);

        // The first layout pass knows the real card width; re-clamp once it is measured.
        Dispatcher.UIThread.Post(Reflow, DispatcherPriority.Loaded);
    }

    /// <summary>Closes the menu and any open submenu.</summary>
    public void Close()
    {
        _subOverlayHandle?.Dispose();
        _subOverlayHandle = null;
        _subAnchor = null;
        _subCard = null;
        if (_overlayHandle is not null)
        {
            _overlayHandle.Dispose();
            _overlayHandle = null;
            _anchor = null;
            _card = null;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Reflow()
    {
        if (_overlayHandle is null || _anchor is null || _card is null) return;

        var overlayRoot = MavOverlay.EnsureOverlayRoot(_anchor);
        if (overlayRoot is null) return;

        var overlaySize = MavOverlay.GetOverlaySize(_anchor);
        PositionCard(
            _card,
            MavOverlay.BoundsInOverlay(_anchor, overlayRoot),
            overlaySize,
            isSubmenu: false,
            _items.Count);

        if (_subOverlayHandle is null || _subCard is null || _subAnchor is null)
            return;

        if (TopLevel.GetTopLevel(_subAnchor) is null)
        {
            _subOverlayHandle.Dispose();
            _subOverlayHandle = null;
            _subAnchor = null;
            _subCard = null;
            return;
        }

        PositionCard(
            _subCard,
            MavOverlay.BoundsInOverlay(_subAnchor, overlayRoot),
            overlaySize,
            isSubmenu: true,
            _subItemCount);
    }

    private MavSurface BuildMenuCard(IReadOnlyList<MavMenuItem> items, Size overlaySize)
    {
        var t = MavTheme.Current;
        var c = t.Color;

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            Margin = new Thickness(0, 6),
        };

        foreach (var item in items)
        {
            if (item.IsDivider)
            {
                var div = new MavDivider { Margin = new Thickness(0, 6) };
                stack.Children.Add(div);
                continue;
            }

            var row = BuildRow(item, t, overlaySize);
            stack.Children.Add(row);
        }

        var maxCardWidth = Math.Max(120, Math.Min(320, overlaySize.Width - 24));
        var maxCardHeight = Math.Max(120, Math.Min(MaxHeight, overlaySize.Height - 24));

        var scroll = new MavScrollViewer
        {
            MaxHeight = maxCardHeight,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = stack,
        };

        return new MavSurface
        {
            MaxWidth = maxCardWidth,
            Radius = 12,
            Elevation = 3,
            FillColor = c.SurfaceContainer,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = scroll,
        };
    }

    /// <summary>
    /// Places the menu card next to its anchor, flipping above it when there is no room
    /// below. Re-run on every resize/rotation so an open menu stays inside the viewport.
    /// </summary>
    private void PositionCard(
        MavSurface card,
        Rect anchorBounds,
        Size overlaySize,
        bool isSubmenu,
        int itemCount)
    {
        var insets = MavOverlay.SafeAreaOf(isSubmenu ? _subAnchor : _anchor);
        var viewportWidth = Math.Max(0, overlaySize.Width - insets.Left - insets.Right);
        var viewportHeight = Math.Max(0, overlaySize.Height - insets.Top - insets.Bottom);

        var maxCardWidth = Math.Max(120, Math.Min(320, viewportWidth - 16));
        var maxCardHeight = Math.Max(120, Math.Min(MaxHeight, viewportHeight - 16));
        if (card.Content is MavScrollViewer scroll)
            scroll.MaxHeight = maxCardHeight;

        var minWidth = Math.Min(Math.Max(MinWidth, anchorBounds.Width), maxCardWidth);
        card.MinWidth = minWidth;
        card.MaxWidth = maxCardWidth;

        // Once the card has been laid out, its real width can exceed the anchor width
        // (long labels). Use it for the edge clamp so the card never hangs over the
        // right or bottom edge of the viewport.
        // Measure the rows with the new caps so the edge clamp uses the width the card
        // will actually get (MinWidth alone is too small for long labels).
        var contentWidth = 0.0;
        if (card.Content is MavScrollViewer { Content: Control rows })
        {
            rows.Measure(new Size(Math.Max(64, maxCardWidth), double.PositiveInfinity));
            contentWidth = rows.DesiredSize.Width;
        }
        var cardWidth = Math.Clamp(contentWidth, minWidth, maxCardWidth);
        var cardHeight = Math.Min(maxCardHeight, itemCount * 44.0 + 16.0);

        var minX = insets.Left + 8;
        var minY = insets.Top + 8;
        double x = isSubmenu
            ? anchorBounds.Right + 4
            : anchorBounds.X;
        x = Math.Clamp(
            x,
            minX,
            Math.Max(minX, overlaySize.Width - insets.Right - cardWidth - 8));
        double y = isSubmenu
            ? anchorBounds.Y
            : (anchorBounds.Bottom + cardHeight + 8 <= viewportHeight + insets.Top
                ? anchorBounds.Bottom + 4
                : anchorBounds.Y - cardHeight - 4);

        y = Math.Clamp(
            y,
            minY,
            Math.Max(minY, overlaySize.Height - insets.Bottom - cardHeight - 8));

        card.Margin = new Thickness(x, y, 0, 0);
    }

    private Control BuildRow(MavMenuItem item, MavTheme t, Size overlaySize)
    {
        var c = t.Color;
        var fg = item.IsEnabled
            ? (item.IsSelected ? c.OnSecondaryContainer : c.OnSurface)
            : c.OnSurface.WithAlpha(t.StateLayer.DisabledContent);
        var iconCol = item.IsEnabled ? c.OnSurfaceVariant : fg;

        var rowGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var leadGlyph = item.IsSelected && string.IsNullOrEmpty(item.LeadingIcon)
            ? MavIcons.Check
            : item.LeadingIcon;
        if (!string.IsNullOrEmpty(leadGlyph))
        {
            var ic = new MavIcon(leadGlyph, 20)
            {
                UseThemeDefaultColor = false,
                Color = item.IsSelected ? c.OnSecondaryContainer : iconCol,
            };
            rowGrid.Add(ic, 0, 0);
        }

        var lbl = new TextBlock
        {
            Text = item.Text,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };
        lbl.Apply(MavTypeRole.LabelLarge, t.Typography);
        lbl.Foreground = MavTheme.BrushOf(fg);
        rowGrid.Add(lbl, 1, 0);

        if (item.HasSubItems && item.SubItems is { Count: > 0 })
        {
            var arrow = new MavIcon(MavIcons.ChevronRight, 20)
            {
                UseThemeDefaultColor = false,
                Color = iconCol,
            };
            rowGrid.Add(arrow, 2, 0);
        }
        else if (!string.IsNullOrEmpty(item.TrailingText))
        {
            var trail = new TextBlock
            {
                Text = item.TrailingText,
                VerticalAlignment = VerticalAlignment.Center,
            };
            trail.Apply(MavTypeRole.LabelSmall, t.Typography);
            trail.Foreground = MavTheme.BrushOf(iconCol);
            rowGrid.Add(trail, 2, 0);
        }

        var surface = new MavInkSurface
        {
            MinHeight = 44,
            CornerRadius = 0,
            Padding = new Thickness(16, 0),
            IsEnabled = item.IsEnabled,
            FillColor = item.IsSelected ? c.SecondaryContainer : Colors.Transparent,
            RippleColor = fg,
            Child = rowGrid,
        };

        surface.Clicked += (_, _) =>
        {
            if (item.HasSubItems && item.SubItems is { Count: > 0 } subs)
            {
                ShowSubmenu(surface, subs.ToList(), overlaySize);
                return;
            }

            Close();
            item.OnSelected?.Invoke();
            item.RaiseClicked();
            if (item.Command is { } cmd && cmd.CanExecute(item.CommandParameter))
                cmd.Execute(item.CommandParameter);
            ItemSelected?.Invoke(this, item);
        };

        return surface;
    }

    private void ShowSubmenu(Control rowAnchor, IReadOnlyList<MavMenuItem> subItems, Size overlaySize)
    {
        _subOverlayHandle?.Dispose();
        _subOverlayHandle = null;

        var overlayRoot = MavOverlay.EnsureOverlayRoot(rowAnchor);
        if (overlayRoot is null) return;

        var bounds = MavOverlay.BoundsInOverlay(rowAnchor, overlayRoot);
        var card = BuildMenuCard(subItems, overlaySize);
        _subAnchor = rowAnchor;
        _subCard = card;
        _subItemCount = subItems.Count;
        PositionCard(card, bounds, overlaySize, isSubmenu: true, subItems.Count);
        _subOverlayHandle = MavOverlay.Show(rowAnchor, card, passThroughScrim: true);
        MavMotion.ScaleIn(card, 0.94, MavMotion.Short3);
        Dispatcher.UIThread.Post(Reflow, DispatcherPriority.Loaded);
    }
}
