using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Side of the anchor on which a <see cref="MavPopover"/> opens.</summary>
public enum MavPopoverPlacement
{
    Auto,
    Bottom,
    Top,
    Left,
    Right,
}

/// <summary>
/// Material Design 3 Anchored Popover / Flyout card that displays arbitrary <see cref="Control"/> content
/// next to a target anchor with automatic viewport boundary clamping.
/// </summary>
public class MavPopover : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MavPopover, string?>(nameof(Title));

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MavPopover, Control?>(nameof(Content));

    public static readonly StyledProperty<MavPopoverPlacement> PlacementProperty =
        AvaloniaProperty.Register<MavPopover, MavPopoverPlacement>(nameof(Placement), MavPopoverPlacement.Auto);

    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<MavPopover, Thickness>(nameof(Padding), new Thickness(16));

    public static readonly StyledProperty<bool> DismissOnOutsideTapProperty =
        AvaloniaProperty.Register<MavPopover, bool>(nameof(DismissOnOutsideTap), true);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavPopover, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private IDisposable? _handle;
    private bool _syncingIsOpen;
    private Control? _anchor;
    private MavSurface? _card;
    private StackPanel? _bodyStack;

    static MavPopover()
    {
        ContentProperty.Changed.AddClassHandler<MavPopover>((p, e) =>
            MavVisualTreeHelpers.AttachLogicalChild(p, e.GetNewValue<Control?>()));
        IsOpenProperty.Changed.AddClassHandler<MavPopover>((p, e) =>
        {
            if (p._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                p.Show((p.Parent as Control) ?? p);
            else
                p.Close();
        });
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public MavPopoverPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public bool DismissOnOutsideTap
    {
        get => GetValue(DismissOnOutsideTapProperty);
        set => SetValue(DismissOnOutsideTapProperty, value);
    }

    public bool IsOpen
    {
        get => _handle is not null || GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public event EventHandler? Opened;
    public event EventHandler? Closed;

    /// <summary>Creates an empty popover.</summary>
    public MavPopover() : this(null, null)
    {
    }

    /// <summary>Creates a popover with optional content, title and placement.</summary>
    public MavPopover(Control? content = null, string? title = null, MavPopoverPlacement placement = MavPopoverPlacement.Auto)
    {
        MinWidth = 180;
        MaxWidth = 320;
        Content = content;
        Title = title;
        Placement = placement;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Opens the popover next to <paramref name="anchor"/>.</summary>
    public void Show(Control anchor)
    {
        Close();
        if (Content is null) return;

        var overlayRoot = MavOverlay.EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return;

        var t = MavTheme.Current;
        var c = t.Color;

        MavVisualTreeHelpers.DetachFromParent(Content);

        var bodyStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
        };

        if (!string.IsNullOrEmpty(Title))
        {
            var titleLbl = new TextBlock
            {
                Text = Title,
                TextWrapping = TextWrapping.Wrap,
                Foreground = MavTheme.BrushOf(c.OnSurface),
            };
            titleLbl.Apply(MavTypeRole.TitleSmall, t.Typography);
            bodyStack.Children.Add(titleLbl);
        }

        bodyStack.Children.Add(Content);

        var bodyHost = new MavScrollViewer
        {
            Content = bodyStack,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        var card = new MavSurface
        {
            Radius = 16,
            Elevation = 3,
            Padding = Padding,
            FillColor = c.SurfaceContainerHigh,
            BorderStrokeColor = c.OutlineVariant.WithAlpha(0.5f),
            BorderStrokeThickness = 1,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Content = bodyHost,
        };

        _anchor = anchor;
        _card = card;
        _bodyStack = bodyStack;

        PlaceCard();

        IDisposable? handle = null;
        handle = MavOverlay.Show(
            anchor,
            card,
            Colors.Transparent,
            DismissOnOutsideTap ? Close : null,
            onViewportChanged: PlaceCard,
            onClosed: () => OnOverlayClosed(handle));
        _handle = handle;

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;

        MavMotion.ScaleIn(card, 0.94, MavMotion.Short3);
        Opened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Recomputes the card geometry for the current overlay size, safe area and anchor
    /// position. Called on open and again after every resize/rotation/keyboard change.
    /// </summary>
    private void PlaceCard()
    {
        if (_card is null || _bodyStack is null || _anchor is null) return;

        var overlayRoot = MavOverlay.EnsureOverlayRoot(_anchor);
        if (overlayRoot is null) return;

        var bounds = MavOverlay.BoundsInOverlay(_anchor, overlayRoot);
        var overlaySize = MavOverlay.GetOverlaySize(_anchor);
        var insets = MavOverlay.SafeAreaOf(_anchor);

        const double gap = 8.0;
        const double edgeMargin = 12.0;

        // Keep the card inside narrow phone viewports. Long bodies scroll instead of
        // growing past the overlay, which also keeps the placement math predictable.
        var viewportWidth = Math.Max(0, overlaySize.Width - insets.Left - insets.Right);
        var viewportHeight = Math.Max(0, overlaySize.Height - insets.Top - insets.Bottom);
        var maxCardWidth = Math.Max(96, viewportWidth - edgeMargin * 2);
        var maxCardHeight = Math.Max(120, viewportHeight - edgeMargin * 2);
        var cardMinWidth = Math.Min(MinWidth, maxCardWidth);
        var cardMaxWidth = Math.Max(cardMinWidth, Math.Min(MaxWidth, maxCardWidth));
        var bodyMaxHeight = Math.Max(64, maxCardHeight - Padding.Top - Padding.Bottom);

        if (_card.Content is MavScrollViewer host)
            host.MaxHeight = bodyMaxHeight;
        _card.MinWidth = cardMinWidth;
        _card.MaxWidth = cardMaxWidth;

        // Measure the body instead of the card: the scroll host only reports its desired
        // size once it is attached and templated, so measuring the card here would return 0.
        var paddingWidth = Padding.Left + Padding.Right;
        var paddingHeight = Padding.Top + Padding.Bottom;
        var bodyAvailableWidth = Math.Max(64, maxCardWidth - paddingWidth);
        _bodyStack.Measure(new Size(bodyAvailableWidth, double.PositiveInfinity));
        var desiredW = Math.Clamp(
            _bodyStack.DesiredSize.Width + paddingWidth,
            cardMinWidth,
            cardMaxWidth);
        _bodyStack.Measure(new Size(Math.Max(64, desiredW - paddingWidth), double.PositiveInfinity));
        var desiredH = Math.Clamp(_bodyStack.DesiredSize.Height + paddingHeight, 64, maxCardHeight);

        var effective = Placement;
        if (effective == MavPopoverPlacement.Auto)
        {
            effective = (bounds.Bottom + gap + desiredH <= overlaySize.Height - insets.Bottom - edgeMargin)
                ? MavPopoverPlacement.Bottom
                : (bounds.Y - gap - desiredH >= insets.Top + edgeMargin ? MavPopoverPlacement.Top : MavPopoverPlacement.Bottom);
        }

        double x, y;
        switch (effective)
        {
            case MavPopoverPlacement.Top:
                x = bounds.X + (bounds.Width - desiredW) / 2.0;
                y = bounds.Y - desiredH - gap;
                break;
            case MavPopoverPlacement.Left:
                x = bounds.X - desiredW - gap;
                y = bounds.Y + (bounds.Height - desiredH) / 2.0;
                break;
            case MavPopoverPlacement.Right:
                x = bounds.Right + gap;
                y = bounds.Y + (bounds.Height - desiredH) / 2.0;
                break;
            default:
                x = bounds.X;
                y = bounds.Bottom + gap;
                break;
        }

        var minX = insets.Left + edgeMargin;
        var minY = insets.Top + edgeMargin;
        x = Math.Clamp(x, minX, Math.Max(minX, overlaySize.Width - insets.Right - desiredW - edgeMargin));
        y = Math.Clamp(y, minY, Math.Max(minY, overlaySize.Height - insets.Bottom - desiredH - edgeMargin));

        // The scroll host wants all available width; pin the measured width so the card
        // keeps the clamped right margin on narrow phones. The width is re-measured on
        // every resize, so it does not stick to the size it had when it was opened.
        _card.Width = desiredW;
        _card.MaxHeight = Math.Max(120, overlaySize.Height - insets.Bottom - y - edgeMargin);
        _card.Margin = new Thickness(x, y, 0, 0);
    }

    /// <summary>Opens the popover next to <paramref name="anchor"/> if it is closed, or closes it if it is open.</summary>
    public void Toggle(Control anchor)
    {
        if (_handle is not null) Close();
        else Show(anchor);
    }

    /// <summary>Closes the popover.</summary>
    public void Close()
    {
        var handle = _handle;
        if (handle is null)
            return;

        _handle = null;
        handle.Dispose();
        FinishClose();
    }

    private void OnOverlayClosed(IDisposable? handle)
    {
        if (handle is null || !ReferenceEquals(_handle, handle))
            return;

        _handle = null;
        FinishClose();
    }

    private void FinishClose()
    {
        _anchor = null;
        _card = null;
        _bodyStack = null;
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Creates a popover with the given content, shows it next to <paramref name="anchor"/> and returns it.</summary>
    public static MavPopover Show(
        Control anchor,
        Control content,
        string? title = null,
        MavPopoverPlacement placement = MavPopoverPlacement.Auto)
    {
        var pop = new MavPopover(content, title, placement);
        pop.Show(anchor);
        return pop;
    }
}
