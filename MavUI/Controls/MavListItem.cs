using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 one-, two-, or three-line list item with leading/trailing icons, custom controls,
/// and optional horizontal swipe actions (<see cref="LeadingSwipeAction"/> / <see cref="TrailingSwipeAction"/>).
/// </summary>
public class MavListItem : MavContentView, IMavDraggableControl
{
    public static readonly StyledProperty<string> HeadlineProperty =
        AvaloniaProperty.Register<MavListItem, string>(nameof(Headline), string.Empty);

    public static readonly StyledProperty<MavListItemLines> LinesProperty =
        AvaloniaProperty.Register<MavListItem, MavListItemLines>(nameof(Lines), MavListItemLines.OneLine);

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavListItem, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> OverlineProperty =
        AvaloniaProperty.Register<MavListItem, string?>(nameof(Overline));

    public static readonly StyledProperty<string?> TrailingTextProperty =
        AvaloniaProperty.Register<MavListItem, string?>(nameof(TrailingText));

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MavListItem, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MavListItem, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavListItem, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowDividerProperty =
        AvaloniaProperty.Register<MavListItem, bool>(nameof(ShowDivider));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavListItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavListItem, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<Control?> LeadingViewProperty =
        AvaloniaProperty.Register<MavListItem, Control?>(nameof(LeadingView));

    public static readonly StyledProperty<Control?> TrailingViewProperty =
        AvaloniaProperty.Register<MavListItem, Control?>(nameof(TrailingView));

    public static readonly StyledProperty<MavSwipeAction?> LeadingSwipeActionProperty =
        AvaloniaProperty.Register<MavListItem, MavSwipeAction?>(nameof(LeadingSwipeAction));

    public static readonly StyledProperty<MavSwipeAction?> TrailingSwipeActionProperty =
        AvaloniaProperty.Register<MavListItem, MavSwipeAction?>(nameof(TrailingSwipeAction));

    public static readonly StyledProperty<ICommand?> TrailingIconCommandProperty =
        AvaloniaProperty.Register<MavListItem, ICommand?>(nameof(TrailingIconCommand));

    public static readonly StyledProperty<object?> TrailingIconCommandParameterProperty =
        AvaloniaProperty.Register<MavListItem, object?>(nameof(TrailingIconCommandParameter));

    private readonly MavInkSurface _surface;
    private readonly Border _swipeBg;
    private readonly MavIcon _swipeIcon;
    private readonly TextBlock _swipeLabel;
    private readonly StackPanel _swipeContent;
    private readonly Border _leadingHost;
    private readonly MavIcon _leadingIconView;
    private readonly TextBlock _overlineLabel;
    private readonly TextBlock _headlineLabel;
    private readonly TextBlock _supportingLabel;
    private readonly StackPanel _trailingHost;
    private readonly TextBlock _trailingTextLabel;
    private readonly Border _trailingIconHit;
    private readonly MavIcon _trailingIconView;
    private readonly MavDivider _divider;
    private Control? _customLeading;
    private Control? _customTrailing;
    private Point _swipeStart;
    private double _swipeOffset;
    private bool _swipeTracking;
    private bool _swiping;

    public event EventHandler? Clicked;
    public event EventHandler? TrailingIconClicked;

    static MavListItem()
    {
        HeadlineProperty.Changed.AddClassHandler<MavListItem>((i, _) =>
        {
            i.Sync();
            i.SyncAutomation();
        });
        SupportingTextProperty.Changed.AddClassHandler<MavListItem>((i, _) =>
        {
            i.Sync();
            i.SyncAutomation();
        });
        OverlineProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.Sync());
        TrailingTextProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.Sync());
        LeadingIconProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.Sync());
        TrailingIconProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.Sync());
        IsSelectedProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.ApplyTheme(MavTheme.Current));
        ShowDividerProperty.Changed.AddClassHandler<MavListItem>((i, e) => i._divider.IsVisible = e.GetNewValue<bool>());
        CommandProperty.Changed.AddClassHandler<MavListItem>((i, e) => i._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavListItem>((i, e) => i._surface.CommandParameter = e.GetNewValue<object?>());
        LeadingSwipeActionProperty.Changed.AddClassHandler<MavListItem>((i, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<MavSwipeAction?>());
            i.ApplyTheme(MavTheme.Current);
        });
        TrailingSwipeActionProperty.Changed.AddClassHandler<MavListItem>((i, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<MavSwipeAction?>());
            i.ApplyTheme(MavTheme.Current);
        });
        LeadingViewProperty.Changed.AddClassHandler<MavListItem>((i, e) =>
        {
            i._customLeading = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(i._customLeading);
            i.Sync();
        });
        TrailingViewProperty.Changed.AddClassHandler<MavListItem>((i, e) =>
        {
            if (i._customTrailing is not null)
                i._trailingHost.Children.Remove(i._customTrailing);
            i._customTrailing = e.GetNewValue<Control?>();
            if (i._customTrailing is not null)
            {
                MavVisualTreeHelpers.DetachFromParent(i._customTrailing);
                i._trailingHost.Children.Add(i._customTrailing);
            }
            i.Sync();
        });
        LeadingSwipeActionProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.ApplyTheme(MavTheme.Current));
        TrailingSwipeActionProperty.Changed.AddClassHandler<MavListItem>((i, _) => i.ApplyTheme(MavTheme.Current));
    }

    /// <summary>Creates an empty list item.</summary>
    public MavListItem() : this(string.Empty)
    {
    }

    /// <summary>Creates a list item with headline, supporting text, leading and trailing content.</summary>
    public MavListItem(
        string headline = "",
        string? supportingText = null,
        string? leadingIcon = null,
        string? trailingIcon = null,
        string? trailingText = null,
        string? overline = null,
        Control? leadingView = null,
        Control? trailingView = null,
        bool showDivider = false)
    {
        _leadingIconView = new MavIcon { Size = 24, UseThemeDefaultColor = false };
        _leadingHost = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
            IsVisible = false,
            Child = _leadingIconView,
        };

        _overlineLabel = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.NoWrap };
        _headlineLabel = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxLines = 1 };
        _supportingLabel = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, MaxLines = 2 };

        var textStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        textStack.Children.Add(_overlineLabel);
        textStack.Children.Add(_headlineLabel);
        textStack.Children.Add(_supportingLabel);

        _trailingTextLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
        };
        _trailingIconView = new MavIcon { Size = 24, UseThemeDefaultColor = false };
        _trailingIconHit = new Border
        {
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
            Child = _trailingIconView,
        };
        _trailingIconHit.PointerReleased += (_, e) =>
        {
            if (TrailingIconClicked is not null || TrailingIconCommand is not null)
            {
                e.Handled = true;
                TrailingIconClicked?.Invoke(this, EventArgs.Empty);
                if (TrailingIconCommand is { } tCmd && tCmd.CanExecute(TrailingIconCommandParameter))
                    tCmd.Execute(TrailingIconCommandParameter);
            }
        };

        _trailingHost = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            IsVisible = false,
        };
        _trailingHost.Children.Add(_trailingTextLabel);
        _trailingHost.Children.Add(_trailingIconHit);

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        row.Add(_leadingHost, 0, 0);
        row.Add(textStack, 1, 0);
        row.Add(_trailingHost, 2, 0);

        _surface = new MavInkSurface
        {
            CornerRadius = 0,
            Padding = new Thickness(16, 12),
            Child = row,
        };
        _surface.Clicked += (_, _) =>
        {
            if (!_swiping)
                Clicked?.Invoke(this, EventArgs.Empty);
        };

        _swipeIcon = new MavIcon { Size = 22, UseThemeDefaultColor = false, VerticalAlignment = VerticalAlignment.Center };
        _swipeLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.Medium };
        _swipeContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0),
            Children = { _swipeIcon, _swipeLabel },
        };
        _swipeBg = new Border
        {
            IsVisible = false,
            Child = _swipeContent,
        };
        _swipeBg.PointerReleased += OnSwipeBackgroundReleased;

        var itemLayer = new Grid { ClipToBounds = true };
        itemLayer.Children.Add(_swipeBg);
        itemLayer.Children.Add(_surface);

        _divider = new MavDivider { IsVisible = showDivider };

        var root = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(itemLayer);
        root.Children.Add(_divider);

        Child = root;

        Headline = headline;
        SupportingText = supportingText;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        TrailingText = trailingText;
        Overline = overline;
        if (leadingView is not null) LeadingView = leadingView;
        if (trailingView is not null) TrailingView = trailingView;
        ShowDivider = showDivider;

        Sync();
        SyncAutomation();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MavAutomation.SetName(this, Headline);
        MavAutomation.SetHelpText(this, SupportingText);
        MavAutomation.SetControlType(this, AutomationControlType.ListItem);
    }

    public string Headline
    {
        get => GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public MavListItemLines Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    public string? SupportingText
    {
        get => GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public string? Overline
    {
        get => GetValue(OverlineProperty);
        set => SetValue(OverlineProperty, value);
    }

    public string? TrailingText
    {
        get => GetValue(TrailingTextProperty);
        set => SetValue(TrailingTextProperty, value);
    }

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public string? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool ShowDivider
    {
        get => GetValue(ShowDividerProperty);
        set => SetValue(ShowDividerProperty, value);
    }

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

    public bool IsInteractive
    {
        get => _surface.IsInteractive;
        set => _surface.IsInteractive = value;
    }

    public Control? LeadingView
    {
        get => GetValue(LeadingViewProperty);
        set => SetValue(LeadingViewProperty, value);
    }

    [Content]
    public Control? TrailingView
    {
        get => GetValue(TrailingViewProperty);
        set => SetValue(TrailingViewProperty, value);
    }

    public MavSwipeAction? LeadingSwipeAction
    {
        get => GetValue(LeadingSwipeActionProperty);
        set => SetValue(LeadingSwipeActionProperty, value);
    }

    public MavSwipeAction? TrailingSwipeAction
    {
        get => GetValue(TrailingSwipeActionProperty);
        set => SetValue(TrailingSwipeActionProperty, value);
    }

    public ICommand? TrailingIconCommand
    {
        get => GetValue(TrailingIconCommandProperty);
        set => SetValue(TrailingIconCommandProperty, value);
    }

    public object? TrailingIconCommandParameter
    {
        get => GetValue(TrailingIconCommandParameterProperty);
        set => SetValue(TrailingIconCommandParameterProperty, value);
    }

    private void Sync()
    {
        _headlineLabel.Text = Headline;

        var hasOver = !string.IsNullOrEmpty(Overline);
        _overlineLabel.Text = Overline ?? string.Empty;
        _overlineLabel.IsVisible = hasOver;

        var hasSub = !string.IsNullOrEmpty(SupportingText);
        _supportingLabel.Text = SupportingText ?? string.Empty;
        _supportingLabel.IsVisible = hasSub;

        if (_customLeading is not null)
        {
            if (!ReferenceEquals(_leadingHost.Child, _customLeading))
            {
                _leadingHost.Child = null;
                MavVisualTreeHelpers.DetachFromParent(_customLeading);
                _leadingHost.Child = _customLeading;
            }
            _leadingHost.IsVisible = true;
        }
        else if (!string.IsNullOrEmpty(LeadingIcon))
        {
            _leadingIconView.Glyph = LeadingIcon;
            if (!ReferenceEquals(_leadingHost.Child, _leadingIconView))
            {
                _leadingHost.Child = null;
                MavVisualTreeHelpers.DetachFromParent(_leadingIconView);
                _leadingHost.Child = _leadingIconView;
            }
            _leadingHost.IsVisible = true;
        }
        else
        {
            _leadingHost.IsVisible = false;
        }

        var hasTrailText = !string.IsNullOrEmpty(TrailingText);
        _trailingTextLabel.Text = TrailingText ?? string.Empty;
        _trailingTextLabel.IsVisible = hasTrailText;

        var hasTrailIcon = !string.IsNullOrEmpty(TrailingIcon);
        _trailingIconView.Glyph = TrailingIcon;
        _trailingIconHit.IsVisible = hasTrailIcon;

        _trailingHost.IsVisible = hasTrailText || hasTrailIcon || _customTrailing is not null;

        var lines = 1 + (hasSub ? 1 : 0) + (hasOver ? 1 : 0);
        var minH = lines switch
        {
            1 => 56.0,
            2 => 72.0,
            _ => 88.0,
        };
        _surface.MinHeight = Math.Max(44, minH + MavTheme.Current.DensityOffset);
    }

    private void ApplyTheme(MavTheme t)
    {
        Sync();
        var c = t.Color;
        _overlineLabel.Apply(MavTypeRole.LabelSmall, t.Typography);
        _headlineLabel.Apply(MavTypeRole.BodyLarge, t.Typography);
        _supportingLabel.Apply(MavTypeRole.BodyMedium, t.Typography);
        _trailingTextLabel.Apply(MavTypeRole.LabelSmall, t.Typography);

        var bg = IsSelected
            ? c.SecondaryContainer
            : ((LeadingSwipeAction is not null || TrailingSwipeAction is not null) ? c.Surface : Colors.Transparent);
        var fg = IsSelected ? c.OnSecondaryContainer : c.OnSurface;
        var subFg = IsSelected ? c.OnSecondaryContainer : c.OnSurfaceVariant;

        _surface.FillColor = bg;
        _surface.RippleColor = fg;
        _headlineLabel.Foreground = MavTheme.BrushOf(fg);
        _overlineLabel.Foreground = MavTheme.BrushOf(subFg);
        _supportingLabel.Foreground = MavTheme.BrushOf(subFg);
        _trailingTextLabel.Foreground = MavTheme.BrushOf(subFg);
        _leadingIconView.Color = subFg;
        _trailingIconView.Color = subFg;
    }

    /// <summary>Starts tracking a press on the item, for swipe and click detection.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (LeadingSwipeAction is null && TrailingSwipeAction is null) return;
        if (MavGestureZones.ShouldDeferToSystem(this, e)) return;
        _swipeStart = e.GetPosition(this);
        _swipeTracking = true;
        _swiping = false;
    }

    /// <summary>Follows the pointer while an item is being swiped.</summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_swipeTracking) return;

        // Tracking must not outlive the press: a tap on the revealed area never reaches OnPointerReleased,
        // so plain mouse movement afterwards would otherwise drag the row again.
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _swipeTracking = false;
            if (_swiping) SettleSwipe(0);
            return;
        }

        var pt = e.GetPosition(this);
        var dx = pt.X - _swipeStart.X;
        var dy = pt.Y - _swipeStart.Y;

        if (!_swiping)
        {
            if (Math.Abs(dy) > 10 && Math.Abs(dy) > Math.Abs(dx))
            {
                _swipeTracking = false;
                return;
            }
            if (Math.Abs(dx) > 10)
            {
                if ((dx > 0 && LeadingSwipeAction is null) || (dx < 0 && TrailingSwipeAction is null))
                {
                    _swipeTracking = false;
                    return;
                }
                _swiping = true;
                e.Pointer.Capture(this);
                e.PreventGestureRecognition();
            }
        }

        if (!_swiping) return;
        e.PreventGestureRecognition();

        if (dx > 0 && LeadingSwipeAction is null) dx = 0;
        if (dx < 0 && TrailingSwipeAction is null) dx = 0;

        _swipeOffset = Math.Clamp(dx, -140, 140);
        UpdateSwipeVisual();
        e.Handled = true;
    }

    /// <summary>Completes a swipe or click gesture, animating the item back or to the action.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_swipeTracking) return;
        _swipeTracking = false;

        if (!_swiping)
        {
            // A tap on the row itself closes a revealed action.
            if (_swipeOffset != 0) SettleSwipe(0);
            return;
        }
        e.Pointer.Capture(null);
        e.Handled = true;

        // Past the threshold the row stays open at the reveal width, so the action can be pressed.
        // Below it, the row springs back.
        var target = _swipeOffset >= SwipeThreshold && LeadingSwipeAction is not null ? SwipeRevealWidth
            : _swipeOffset <= -SwipeThreshold && TrailingSwipeAction is not null ? -SwipeRevealWidth
            : 0;
        SettleSwipe(target);
    }

    private void SettleSwipe(double target)
    {
        var start = _swipeOffset;
        MavMotion.Cancel(this, "li_swipe");
        MavMotion.Animate(this, "li_swipe", start, target, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _swipeOffset = v;
            UpdateSwipeVisual();
        }, () =>
        {
            _swiping = false;
            if (target == 0) _swipeBg.IsVisible = false;
        });
    }

    private void OnSwipeBackgroundReleased(object? sender, PointerReleasedEventArgs e)
    {
        _swipeTracking = false;
        if (_swipeOffset == 0) return;
        var action = _swipeOffset > 0 ? LeadingSwipeAction : TrailingSwipeAction;
        e.Handled = true;
        SettleSwipe(0);
        action?.Invoke();
    }

    private const double SwipeThreshold = 72;
    private const double SwipeRevealWidth = 96;

    private void UpdateSwipeVisual()
    {
        _surface.SetTranslation(_swipeOffset, 0);
        var c = MavTheme.Current.Color;
        var action = _swipeOffset >= 0 ? LeadingSwipeAction : TrailingSwipeAction;
        if (action is null || Math.Abs(_swipeOffset) < 2)
        {
            _swipeBg.IsVisible = false;
            return;
        }

        _swipeBg.IsVisible = true;
        var bg = action.BackgroundColor ?? (_swipeOffset > 0 ? c.TertiaryContainer : c.ErrorContainer);
        var fg = action.ForegroundColor ?? (_swipeOffset > 0 ? c.OnTertiaryContainer : c.OnErrorContainer);
        _swipeBg.Background = MavTheme.BrushOf(bg);
        _swipeIcon.Glyph = action.Icon;
        _swipeIcon.Color = fg;
        _swipeLabel.Text = action.Label;
        _swipeLabel.Foreground = MavTheme.BrushOf(fg);
        _swipeContent.HorizontalAlignment = _swipeOffset > 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        _surface.FillColor = IsSelected ? c.SecondaryContainer : c.Surface;
    }
}
