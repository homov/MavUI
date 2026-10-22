using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

public enum MavSideSheetPlacement
{
    End,
    Start,
}

/// <summary>
/// Material Design 3 Modal Side Sheet with slide-in animation, horizontal swipe-to-dismiss,
/// header, body, and optional footer actions.
/// </summary>
public class MavSideSheet : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MavSideSheet, string?>(nameof(Title));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavSideSheet, Control?>(nameof(Body));

    public static readonly StyledProperty<Control?> FooterProperty =
        AvaloniaProperty.Register<MavSideSheet, Control?>(nameof(Footer));

    public static readonly StyledProperty<MavSideSheetPlacement> PlacementProperty =
        AvaloniaProperty.Register<MavSideSheet, MavSideSheetPlacement>(nameof(Placement), MavSideSheetPlacement.End);

    public static readonly StyledProperty<double> SheetWidthProperty =
        AvaloniaProperty.Register<MavSideSheet, double>(nameof(SheetWidth), 320.0);

    public static readonly StyledProperty<bool> ShowBackButtonProperty =
        AvaloniaProperty.Register<MavSideSheet, bool>(nameof(ShowBackButton));

    public static readonly StyledProperty<bool> DismissOnScrimTapProperty =
        AvaloniaProperty.Register<MavSideSheet, bool>(nameof(DismissOnScrimTap), true);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavSideSheet, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> BackCommandProperty =
        AvaloniaProperty.Register<MavSideSheet, ICommand?>(nameof(BackCommand));

    public static readonly StyledProperty<object?> BackCommandParameterProperty =
        AvaloniaProperty.Register<MavSideSheet, object?>(nameof(BackCommandParameter));

    private IDisposable? _handle;
    private MavSurface? _panel;
    private double _dragStartX;
    private bool _dragCandidate;
    private bool _dragging;
    private bool _syncingIsOpen;

    static MavSideSheet()
    {
        BodyProperty.Changed.AddClassHandler<MavSideSheet>((s, e) =>
            MavVisualTreeHelpers.AttachLogicalChild(s, e.GetNewValue<Control?>()));
        FooterProperty.Changed.AddClassHandler<MavSideSheet>((s, e) =>
            MavVisualTreeHelpers.AttachLogicalChild(s, e.GetNewValue<Control?>()));
        IsOpenProperty.Changed.AddClassHandler<MavSideSheet>((s, e) =>
        {
            if (s._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                s.Show(s.Parent as Visual ?? s);
            else
                s.Close();
        });
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

    public Control? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public Control? FooterContent
    {
        get => Footer;
        set => Footer = value;
    }

    public MavSideSheetPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public double SheetWidth
    {
        get => GetValue(SheetWidthProperty);
        set => SetValue(SheetWidthProperty, value);
    }

    public bool ShowBackButton
    {
        get => GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
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

    public ICommand? BackCommand
    {
        get => GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public object? BackCommandParameter
    {
        get => GetValue(BackCommandParameterProperty);
        set => SetValue(BackCommandParameterProperty, value);
    }

    public event EventHandler? BackClicked;
    public event EventHandler? Closed;

    protected override Size MeasureOverride(Size availableSize) => default;

    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MavTheme.Current;
        var c = t.Color;

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            MinHeight = 64,
            Margin = new Thickness(12, 8, 12, 4),
            Background = Brushes.Transparent,
        };
        header.PointerPressed += OnDragPressed;
        header.PointerMoved += OnDragMoved;
        header.PointerReleased += OnDragReleased;

        if (ShowBackButton)
        {
            var backBtn = new MavIconButton(MavIcons.ArrowBack);
            backBtn.Clicked += (_, _) =>
            {
                BackClicked?.Invoke(this, EventArgs.Empty);
                if (BackCommand is { } bCmd && bCmd.CanExecute(BackCommandParameter))
                    bCmd.Execute(BackCommandParameter);
            };
            header.Add(backBtn, 0, 0);
        }

        TextBlock? titleLbl = null;
        if (!string.IsNullOrEmpty(Title))
        {
            titleLbl = new TextBlock
            {
                Text = Title,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(ShowBackButton ? 8 : 12, 0),
            };
            titleLbl.Apply(MavTypeRole.TitleLarge, t.Typography);
            header.Add(titleLbl, 1, 0);
        }

        var closeBtn = new MavIconButton(MavIcons.Close);
        closeBtn.Clicked += (_, _) => Close();
        header.Add(closeBtn, 2, 0);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
        };
        grid.Add(header, 0, 0);

        if (Body is not null)
        {
            MavVisualTreeHelpers.DetachFromParent(Body);
            var scroll = new MavScrollViewer
            {
                Padding = new Thickness(24, 8, 24, 16),
                Content = Body,
            };
            grid.Add(scroll, 0, 1);
        }

        if (Footer is not null)
        {
            MavVisualTreeHelpers.DetachFromParent(Footer);
            var footerStack = new StackPanel { Orientation = Orientation.Vertical };
            footerStack.Children.Add(new MavDivider());
            var footerHost = new Border
            {
                Padding = new Thickness(24, 16),
                Child = Footer,
            };
            footerStack.Children.Add(footerHost);
            grid.Add(footerStack, 0, 2);
        }

        var isEnd = Placement == MavSideSheetPlacement.End;
        _panel = new MavSurface
        {
            Width = SheetWidth,
            Elevation = 3,
            HorizontalAlignment = isEnd ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = new MavSafeArea(grid, MavSafeAreaEdges.Vertical),
        };
        if (isEnd)
            _panel.SetCornerRadii(28, 0, 28, 0);
        else
            _panel.SetCornerRadii(0, 28, 0, 28);

        var pushAnchor = anchor ?? (this.Parent as Visual) ?? this;

        // The panel stretches vertically; clamp its width so it still fits a phone in
        // portrait or a split-screen window after a resize/rotation.
        void Reflow()
        {
            if (_panel is null) return;
            var size = MavOverlay.GetOverlaySize(pushAnchor);
            var insets = MavOverlay.SafeAreaOf(pushAnchor);
            var viewportWidth = Math.Max(0, size.Width - insets.Left - insets.Right);
            _panel.Width = Math.Max(0, Math.Min(SheetWidth, viewportWidth));
        }

        Reflow();

        MavTheme.Observe(_panel, (surf, theme) =>
        {
            var col = theme.Color;
            surf.FillColor = col.SurfaceContainerLow;
            if (titleLbl is not null)
                titleLbl.Foreground = MavTheme.BrushOf(col.OnSurfaceVariant);
        });

        _handle = MavOverlay.Show(
            pushAnchor,
            _panel,
            scrimColor: c.Scrim.WithAlpha(0.32f),
            onScrimTap: () =>
            {
                if (DismissOnScrimTap) Close();
            },
            onViewportChanged: Reflow);

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;

        var panel = _panel;
        var startX = isEnd ? SheetWidth : -SheetWidth;
        panel.SetTranslationX(startX);
        MavMotion.Animate(panel, "ss_in", startX, 0, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            panel.SetTranslationX(v);
        });
    }

    public void Close()
    {
        if (_handle is null) return;
        var h = _handle;
        var p = _panel;
        _handle = null;
        _panel = null;
        _dragCandidate = false;
        _dragging = false;

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;

        if (p is not null)
        {
            MavMotion.Cancel(p, "ss_in");
            MavMotion.Cancel(p, "ss_snap");
            var targetX = Placement == MavSideSheetPlacement.End ? SheetWidth : -SheetWidth;
            MavMotion.Animate(p, "ss_out", p.GetTranslationX(), targetX, MavMotion.Short4, MavMotion.CubicIn, v =>
            {
                p.SetTranslationX(v);
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

    public static MavSideSheet Show(
        string? title,
        Control body,
        Control? footer = null,
        MavSideSheetPlacement placement = MavSideSheetPlacement.End,
        Visual? anchor = null)
    {
        var sheet = new MavSideSheet
        {
            Title = title,
            Body = body,
            Footer = footer,
            Placement = placement,
        };
        sheet.Show(anchor);
        return sheet;
    }

    public static MavSideSheet ShowCurrent(
        string? title,
        Control body,
        Control? footer = null,
        MavSideSheetPlacement placement = MavSideSheetPlacement.End) =>
        Show(title, body, footer, placement, anchor: null);

    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragCandidate = false;
        _dragging = false;
        if (_panel is null || sender is not Control) return;
        if (e.Source is Visual src &&
            src.GetSelfAndVisualAncestors().Any(v => v is MavInkSurface or MavIconButton or Button))
        {
            return;
        }

        _dragCandidate = true;
        _dragStartX = e.GetPosition(_panel.Parent as Visual).X;
    }

    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragCandidate || _panel is null || sender is not Control ctrl) return;
        var curX = e.GetPosition(_panel.Parent as Visual).X;
        var dx = curX - _dragStartX;

        if (!_dragging)
        {
            if (Math.Abs(dx) <= 6) return;
            _dragging = true;
            e.Pointer.Capture(ctrl);
        }

        var clamped = Placement == MavSideSheetPlacement.End
            ? Math.Max(0, dx)
            : Math.Min(0, dx);
        _panel.SetTranslationX(clamped);
        e.Handled = true;
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragCandidate = false;
        if (!_dragging || _panel is null) return;
        _dragging = false;
        e.Pointer.Capture(null);
        if (Math.Abs(_panel.GetTranslationX()) > 80)
        {
            Close();
        }
        else
        {
            var panel = _panel;
            MavMotion.Animate(panel, "ss_snap", panel.GetTranslationX(), 0, MavMotion.Short4, MavMotion.CubicOut, v =>
            {
                panel.SetTranslationX(v);
            });
        }
        e.Handled = true;
    }
}
