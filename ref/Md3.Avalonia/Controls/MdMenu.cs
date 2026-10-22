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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed class MdMenuItem : Control, IEnumerable<MdMenuItem>
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdMenuItem, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MdMenuItem, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<string?> TrailingTextProperty =
        AvaloniaProperty.Register<MdMenuItem, string?>(nameof(TrailingText));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdMenuItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdMenuItem, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MdMenuItem, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsDividerProperty =
        AvaloniaProperty.Register<MdMenuItem, bool>(nameof(IsDivider));

    private AvaloniaList<MdMenuItem>? _subItems;

    static MdMenuItem()
    {
        TextProperty.Changed.AddClassHandler<MdMenuItem>((i, e) =>
            MdAutomation.SetName(i, e.GetNewValue<string>()));
    }

    public MdMenuItem()
    {
        MdAutomation.SetControlType(this, AutomationControlType.MenuItem);
    }

    public MdMenuItem(
        string text,
        string? leadingIcon = null,
        string? trailingText = null,
        Action? onSelected = null,
        bool isEnabled = true,
        bool isSelected = false)
    {
        MdAutomation.SetControlType(this, AutomationControlType.MenuItem);
        Text = text;
        LeadingIcon = leadingIcon;
        TrailingText = trailingText;
        OnSelected = onSelected;
        IsEnabled = isEnabled;
        IsSelected = isSelected;
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public string? TrailingText
    {
        get => GetValue(TrailingTextProperty);
        set => SetValue(TrailingTextProperty, value);
    }

    public Action? OnSelected { get; set; }
    public event EventHandler? Clicked;

    internal void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsDivider
    {
        get => GetValue(IsDividerProperty);
        set => SetValue(IsDividerProperty, value);
    }

    [Content]
    public IList<MdMenuItem>? SubItems
    {
        get
        {
            if (_subItems is null)
            {
                _subItems = new AvaloniaList<MdMenuItem>();
                _subItems.CollectionChanged += (_, _) =>
                {
                    foreach (var s in _subItems)
                        MdVisualTreeHelpers.AttachLogicalChild(this, s);
                };
            }
            return _subItems;
        }
        set
        {
            _subItems = value is null ? null : new AvaloniaList<MdMenuItem>(value);
            if (_subItems is not null)
            {
                foreach (var s in _subItems)
                    MdVisualTreeHelpers.AttachLogicalChild(this, s);
            }
        }
    }

    internal bool HasSubItems => _subItems is { Count: > 0 };

    public void Add(MdMenuItem subItem) => SubItems!.Add(subItem);

    public IEnumerator<MdMenuItem> GetEnumerator() => (_subItems ?? Enumerable.Empty<MdMenuItem>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Material Design 3 anchored popup menu with icons, keyboard shortcuts, checkmarks, dividers, and submenus.
/// </summary>
public class MdMenu : Control, IEnumerable<MdMenuItem>
{
    private readonly AvaloniaList<MdMenuItem> _items = new();
    private IDisposable? _overlayHandle;
    private IDisposable? _subOverlayHandle;
    private Control? _anchor;
    private MdSurface? _card;
    private Control? _subAnchor;
    private MdSurface? _subCard;
    private int _subItemCount;

    public event EventHandler<MdMenuItem>? ItemSelected;
    public event EventHandler? Closed;

    public MdMenu()
    {
        MinWidth = 180;
        MaxHeight = 320;
        _items.CollectionChanged += (_, _) =>
        {
            foreach (var item in _items)
                MdVisualTreeHelpers.AttachLogicalChild(this, item);
        };
    }

    [Content]
    public AvaloniaList<MdMenuItem> Items => _items;

    protected override Size MeasureOverride(Size availableSize) => default;

    public void Add(MdMenuItem item) => _items.Add(item);

    public IEnumerator<MdMenuItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public MdMenu AddItem(
        string text,
        string? leadingIcon = null,
        string? trailingText = null,
        Action? onSelected = null,
        bool isEnabled = true,
        bool isSelected = false)
    {
        _items.Add(new MdMenuItem(text, leadingIcon, trailingText, onSelected, isEnabled, isSelected));
        return this;
    }

    public MdMenu AddSubmenu(string text, Action<MdMenu> buildSubmenu, string? leadingIcon = null)
    {
        var sub = new MdMenu();
        buildSubmenu(sub);
        _items.Add(new MdMenuItem(text, leadingIcon)
        {
            SubItems = sub._items.ToList()
        });
        return this;
    }

    public MdMenu AddDivider()
    {
        _items.Add(new MdMenuItem(string.Empty) { IsDivider = true });
        return this;
    }

    public void Clear() => _items.Clear();

    public void Show(Control anchor)
    {
        Close();

        var overlayRoot = MdOverlay.EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return;

        var anchorBounds = MdOverlay.BoundsInOverlay(anchor, overlayRoot);
        var overlaySize = MdOverlay.GetOverlaySize(anchor);

        _anchor = anchor;
        _card = BuildMenuCard(_items, overlaySize);
        PositionCard(_card, anchorBounds, overlaySize, isSubmenu: false, _items.Count);

        _overlayHandle = MdOverlay.Show(
            anchor,
            _card,
            scrimColor: Colors.Transparent,
            onScrimTap: Close,
            onViewportChanged: Reflow);

        MdMotion.ScaleIn(_card, 0.92, MdMotion.Short3);

        // The first layout pass knows the real card width; re-clamp once it is measured.
        Dispatcher.UIThread.Post(Reflow, DispatcherPriority.Loaded);
    }

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

        var overlayRoot = MdOverlay.EnsureOverlayRoot(_anchor);
        if (overlayRoot is null) return;

        var overlaySize = MdOverlay.GetOverlaySize(_anchor);
        PositionCard(
            _card,
            MdOverlay.BoundsInOverlay(_anchor, overlayRoot),
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
            MdOverlay.BoundsInOverlay(_subAnchor, overlayRoot),
            overlaySize,
            isSubmenu: true,
            _subItemCount);
    }

    private MdSurface BuildMenuCard(IReadOnlyList<MdMenuItem> items, Size overlaySize)
    {
        var t = MdTheme.Current;
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
                var div = new MdDivider { Margin = new Thickness(0, 6) };
                stack.Children.Add(div);
                continue;
            }

            var row = BuildRow(item, t, overlaySize);
            stack.Children.Add(row);
        }

        var maxCardWidth = Math.Max(120, Math.Min(320, overlaySize.Width - 24));
        var maxCardHeight = Math.Max(120, Math.Min(MaxHeight, overlaySize.Height - 24));

        var scroll = new MdScrollViewer
        {
            MaxHeight = maxCardHeight,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = stack,
        };

        return new MdSurface
        {
            MaxWidth = maxCardWidth,
            Radius = 12,
            Elevation = 3,
            FillColor = c.SurfaceContainer,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Content = scroll,
        };
    }

    /// <summary>
    /// Places the menu card next to its anchor, flipping above it when there is no room
    /// below. Re-run on every resize/rotation so an open menu stays inside the viewport.
    /// </summary>
    private void PositionCard(
        MdSurface card,
        Rect anchorBounds,
        Size overlaySize,
        bool isSubmenu,
        int itemCount)
    {
        var insets = MdOverlay.SafeAreaOf(isSubmenu ? _subAnchor : _anchor);
        var viewportWidth = Math.Max(0, overlaySize.Width - insets.Left - insets.Right);
        var viewportHeight = Math.Max(0, overlaySize.Height - insets.Top - insets.Bottom);

        var maxCardWidth = Math.Max(120, Math.Min(320, viewportWidth - 16));
        var maxCardHeight = Math.Max(120, Math.Min(MaxHeight, viewportHeight - 16));
        if (card.Content is MdScrollViewer scroll)
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
        if (card.Content is MdScrollViewer { Content: Control rows })
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

    private Control BuildRow(MdMenuItem item, MdTheme t, Size overlaySize)
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
            ? MdIcons.Check
            : item.LeadingIcon;
        if (!string.IsNullOrEmpty(leadGlyph))
        {
            var ic = new MdIcon(leadGlyph, 20)
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
        lbl.Apply(MdTypeRole.LabelLarge, t.Typography);
        lbl.Foreground = MdTheme.BrushOf(fg);
        rowGrid.Add(lbl, 1, 0);

        if (item.HasSubItems && item.SubItems is { Count: > 0 })
        {
            var arrow = new MdIcon(MdIcons.ChevronRight, 20)
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
            trail.Apply(MdTypeRole.LabelSmall, t.Typography);
            trail.Foreground = MdTheme.BrushOf(iconCol);
            rowGrid.Add(trail, 2, 0);
        }

        var surface = new MdInkSurface
        {
            MinHeight = 44,
            CornerRadius = 0,
            Padding = new Thickness(16, 0),
            IsEnabled = item.IsEnabled,
            FillColor = item.IsSelected ? c.SecondaryContainer : Colors.Transparent,
            RippleColor = fg,
            Content = rowGrid,
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

    private void ShowSubmenu(Control rowAnchor, IReadOnlyList<MdMenuItem> subItems, Size overlaySize)
    {
        _subOverlayHandle?.Dispose();
        _subOverlayHandle = null;

        var overlayRoot = MdOverlay.EnsureOverlayRoot(rowAnchor);
        if (overlayRoot is null) return;

        var bounds = MdOverlay.BoundsInOverlay(rowAnchor, overlayRoot);
        var card = BuildMenuCard(subItems, overlaySize);
        _subAnchor = rowAnchor;
        _subCard = card;
        _subItemCount = subItems.Count;
        PositionCard(card, bounds, overlaySize, isSubmenu: true, subItems.Count);
        _subOverlayHandle = MdOverlay.Show(rowAnchor, card, passThroughScrim: true);
        MdMotion.ScaleIn(card, 0.94, MdMotion.Short3);
        Dispatcher.UIThread.Post(Reflow, DispatcherPriority.Loaded);
    }
}
