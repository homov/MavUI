using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Modal Bottom Sheet with drag handle, optional title, swipe-down dismissal, and slide-up entrance.
/// </summary>
public class MavBottomSheet : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MavBottomSheet, string?>(nameof(Title));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavBottomSheet, Control?>(nameof(Body));

    public static readonly StyledProperty<bool> ShowDragHandleProperty =
        AvaloniaProperty.Register<MavBottomSheet, bool>(nameof(ShowDragHandle), true);

    public static readonly StyledProperty<bool> DismissOnScrimTapProperty =
        AvaloniaProperty.Register<MavBottomSheet, bool>(nameof(DismissOnScrimTap), true);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavBottomSheet, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private IDisposable? _handle;
    private MavSurface? _sheetCard;
    private MavScrollViewer? _bodyScroll;
    private double _dragStartY;
    private bool _dragging;
    private bool _syncingIsOpen;

    static MavBottomSheet()
    {
        BodyProperty.Changed.AddClassHandler<MavBottomSheet>((s, e) =>
            MavVisualTreeHelpers.AttachLogicalChild(s, e.GetNewValue<Control?>()));
        IsOpenProperty.Changed.AddClassHandler<MavBottomSheet>((s, e) =>
        {
            if (s._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                s.Show(s.Parent as Visual ?? s);
            else
                s.Close();
        });
    }

    /// <summary>Creates an empty bottom sheet.</summary>
    public MavBottomSheet()
    {
        MaxWidth = 640;
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public bool ShowDragHandle
    {
        get => GetValue(ShowDragHandleProperty);
        set => SetValue(ShowDragHandleProperty, value);
    }

    public bool DismissOnScrimTap
    {
        get => GetValue(DismissOnScrimTapProperty);
        set => SetValue(DismissOnScrimTapProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public event EventHandler? Closed;

    /// <summary>
    /// Measures the sheet during the layout pass; its size is set by the overlay host.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Shows the sheet above the content, anchored to <paramref name="anchor"/> for safe-area and overlay placement.</summary>
    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MavTheme.Current;
        var c = t.Color;
        var overlaySize = MavOverlay.GetOverlaySize(anchor);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
        };

        var headerStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Background = Brushes.Transparent,
        };

        Border? pill = null;
        if (ShowDragHandle)
        {
            pill = new Border
            {
                Width = 32,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 4),
            };
            headerStack.Children.Add(pill);
        }

        TextBlock? titleLbl = null;
        if (!string.IsNullOrEmpty(Title))
        {
            titleLbl = new TextBlock
            {
                Text = Title,
                Margin = new Thickness(24, ShowDragHandle ? 0 : 16, 24, 4),
            };
            titleLbl.Apply(MavTypeRole.TitleLarge, t.Typography);
            headerStack.Children.Add(titleLbl);
        }

        if (headerStack.Children.Count > 0)
        {
            var dragArea = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0, 4),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = headerStack,
            };
            dragArea.PointerPressed += OnDragPressed;
            dragArea.PointerMoved += OnDragMoved;
            dragArea.PointerReleased += OnDragReleased;
            stack.Children.Add(dragArea);
        }

        if (Body is not null)
        {
            MavVisualTreeHelpers.DetachFromParent(Body);
            var scroll = new MavScrollViewer
            {
                MaxHeight = Math.Max(220, overlaySize.Height * 0.72),
                Padding = new Thickness(24, 4, 24, 24),
                Content = Body,
            };
            _bodyScroll = scroll;
            stack.Children.Add(scroll);
        }

        _sheetCard = new MavSurface
        {
            MaxWidth = MaxWidth,
            Elevation = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Child = new MavSafeArea(stack, MavSafeAreaEdges.Bottom | MavSafeAreaEdges.Horizontal),
        };
        _sheetCard.SetCornerRadii(28, 28, 0, 0);
        MavAutomation.SetName(_sheetCard, Title);

        var pushAnchor = anchor ?? (this.Parent as Visual) ?? this;

        // The sheet stretches with the overlay; only the body cap and the max width have
        // to follow the viewport (rotation, keyboard, safe-area changes).
        void Reflow()
        {
            var card = _sheetCard;
            if (card is null) return;

            var size = MavOverlay.GetOverlaySize(pushAnchor);
            var insets = MavOverlay.SafeAreaOf(pushAnchor);
            var viewportWidth = Math.Max(0, size.Width - insets.Left - insets.Right);
            var viewportHeight = Math.Max(0, size.Height - insets.Top - insets.Bottom);

            card.MaxWidth = Math.Min(MaxWidth, viewportWidth);
            if (_bodyScroll is not null)
                _bodyScroll.MaxHeight = Math.Max(220, viewportHeight * 0.72);
        }

        Reflow();

        MavTheme.Observe(_sheetCard, (surf, theme) =>
        {
            var col = theme.Color;
            surf.FillColor = col.SurfaceContainerLow;
            if (pill is not null)
                pill.Background = MavTheme.BrushOf(col.OnSurfaceVariant.WithAlpha(0.4f));
            if (titleLbl is not null)
                titleLbl.Foreground = MavTheme.BrushOf(col.OnSurface);
        });

        _handle = MavOverlay.Show(
            pushAnchor,
            _sheetCard,
            scrimColor: c.Scrim.WithAlpha(0.32f),
            onScrimTap: () =>
            {
                if (DismissOnScrimTap) Close();
            },
            onViewportChanged: Reflow);

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;

        var card = _sheetCard;
        card.SetTranslationY(260);
        MavMotion.Animate(card, "bs_in", 260, 0, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            card.SetTranslationY(v);
        });
    }

    /// <summary>Hides the sheet with the exit animation and removes it from the overlay layer.</summary>
    public void Close()
    {
        if (_handle is null) return;
        var h = _handle;
        var card = _sheetCard;
        _handle = null;
        _sheetCard = null;
        _bodyScroll = null;
        _dragging = false;

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;

        if (card is not null)
        {
            MavMotion.Cancel(card, "bs_in");
            MavMotion.Cancel(card, "bs_snap");
            MavMotion.Animate(card, "bs_out", card.GetTranslationY(), 260, MavMotion.Short4, MavMotion.CubicIn, v =>
            {
                card.SetTranslationY(v);
            }, () =>
            {
                h.Dispose();
                Closed?.Invoke(this, EventArgs.Empty);
            });
        }
        else
        {
            h.Dispose();
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Creates a sheet with the given content, shows it and returns it so the caller can close it later.</summary>
    public static MavBottomSheet Show(
        string? title,
        Control body,
        Visual? anchor = null,
        bool showDragHandle = true)
    {
        var sheet = new MavBottomSheet
        {
            Title = title,
            Body = body,
            ShowDragHandle = showDragHandle,
        };
        sheet.Show(anchor);
        return sheet;
    }

    /// <summary>Shows a sheet over the current page with the given content and returns it.</summary>
    public static MavBottomSheet ShowCurrent(
        string? title,
        Control body,
        bool showDragHandle = true) =>
        Show(title, body, anchor: null, showDragHandle: showDragHandle);

    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_sheetCard is null || sender is not Control ctrl) return;
        _dragging = true;
        _dragStartY = e.GetPosition(_sheetCard.Parent as Visual).Y;
        e.Pointer.Capture(ctrl);
        e.Handled = true;
    }

    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _sheetCard is null) return;
        var curY = e.GetPosition(_sheetCard.Parent as Visual).Y;
        var dy = Math.Max(0, curY - _dragStartY);
        _sheetCard.SetTranslationY(dy);
        e.Handled = true;
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging || _sheetCard is null) return;
        _dragging = false;
        e.Pointer.Capture(null);
        if (_sheetCard.GetTranslationY() > 80)
        {
            Close();
        }
        else
        {
            var card = _sheetCard;
            MavMotion.Animate(card, "bs_snap", card.GetTranslationY(), 0, MavMotion.Short4, MavMotion.CubicOut, v =>
            {
                card.SetTranslationY(v);
            });
        }
        e.Handled = true;
    }
}
