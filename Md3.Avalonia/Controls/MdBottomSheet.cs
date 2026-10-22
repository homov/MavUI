using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Modal Bottom Sheet with drag handle, optional title, swipe-down dismissal, and slide-up entrance.
/// </summary>
public class MdBottomSheet : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MdBottomSheet, string?>(nameof(Title));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MdBottomSheet, Control?>(nameof(Body));

    public static readonly StyledProperty<bool> ShowDragHandleProperty =
        AvaloniaProperty.Register<MdBottomSheet, bool>(nameof(ShowDragHandle), true);

    public static readonly StyledProperty<bool> DismissOnScrimTapProperty =
        AvaloniaProperty.Register<MdBottomSheet, bool>(nameof(DismissOnScrimTap), true);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdBottomSheet, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private IDisposable? _handle;
    private MdSurface? _sheetCard;
    private MdScrollViewer? _bodyScroll;
    private double _dragStartY;
    private bool _dragging;
    private bool _syncingIsOpen;

    static MdBottomSheet()
    {
        BodyProperty.Changed.AddClassHandler<MdBottomSheet>((s, e) =>
            MdVisualTreeHelpers.AttachLogicalChild(s, e.GetNewValue<Control?>()));
        IsOpenProperty.Changed.AddClassHandler<MdBottomSheet>((s, e) =>
        {
            if (s._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                s.Show(s.Parent as Visual ?? s);
            else
                s.Close();
        });
    }

    public MdBottomSheet()
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

    protected override Size MeasureOverride(Size availableSize) => default;

    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MdTheme.Current;
        var c = t.Color;
        var overlaySize = MdOverlay.GetOverlaySize(anchor);

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
            titleLbl.Apply(MdTypeRole.TitleLarge, t.Typography);
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
            MdVisualTreeHelpers.DetachFromParent(Body);
            var scroll = new MdScrollViewer
            {
                MaxHeight = Math.Max(220, overlaySize.Height * 0.72),
                Padding = new Thickness(24, 4, 24, 24),
                Content = Body,
            };
            _bodyScroll = scroll;
            stack.Children.Add(scroll);
        }

        _sheetCard = new MdSurface
        {
            MaxWidth = MaxWidth,
            Elevation = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Content = new MdSafeArea(stack, MdSafeAreaEdges.Bottom | MdSafeAreaEdges.Horizontal),
        };
        _sheetCard.SetCornerRadii(28, 28, 0, 0);
        MdAutomation.SetName(_sheetCard, Title);

        var pushAnchor = anchor ?? (this.Parent as Visual) ?? this;

        // The sheet stretches with the overlay; only the body cap and the max width have
        // to follow the viewport (rotation, keyboard, safe-area changes).
        void Reflow()
        {
            var card = _sheetCard;
            if (card is null) return;

            var size = MdOverlay.GetOverlaySize(pushAnchor);
            var insets = MdOverlay.SafeAreaOf(pushAnchor);
            var viewportWidth = Math.Max(0, size.Width - insets.Left - insets.Right);
            var viewportHeight = Math.Max(0, size.Height - insets.Top - insets.Bottom);

            card.MaxWidth = Math.Min(MaxWidth, viewportWidth);
            if (_bodyScroll is not null)
                _bodyScroll.MaxHeight = Math.Max(220, viewportHeight * 0.72);
        }

        Reflow();

        MdTheme.Observe(_sheetCard, (surf, theme) =>
        {
            var col = theme.Color;
            surf.FillColor = col.SurfaceContainerLow;
            if (pill is not null)
                pill.Background = MdTheme.BrushOf(col.OnSurfaceVariant.WithAlpha(0.4f));
            if (titleLbl is not null)
                titleLbl.Foreground = MdTheme.BrushOf(col.OnSurface);
        });

        _handle = MdOverlay.Show(
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
        MdMotion.Animate(card, "bs_in", 260, 0, MdMotion.Medium2, MdMotion.CubicOut, v =>
        {
            card.SetTranslationY(v);
        });
    }

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
            MdMotion.Cancel(card, "bs_in");
            MdMotion.Cancel(card, "bs_snap");
            MdMotion.Animate(card, "bs_out", card.GetTranslationY(), 260, MdMotion.Short4, MdMotion.CubicIn, v =>
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

    public static MdBottomSheet Show(
        string? title,
        Control body,
        Visual? anchor = null,
        bool showDragHandle = true)
    {
        var sheet = new MdBottomSheet
        {
            Title = title,
            Body = body,
            ShowDragHandle = showDragHandle,
        };
        sheet.Show(anchor);
        return sheet;
    }

    public static MdBottomSheet ShowCurrent(
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
            MdMotion.Animate(card, "bs_snap", card.GetTranslationY(), 0, MdMotion.Short4, MdMotion.CubicOut, v =>
            {
                card.SetTranslationY(v);
            });
        }
        e.Handled = true;
    }
}
