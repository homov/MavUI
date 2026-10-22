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

public sealed class MavSwipeAction : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavSwipeAction, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavSwipeAction, string>(nameof(Icon), MavIcons.Check);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavSwipeAction, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavSwipeAction, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<Color?> BackgroundColorProperty =
        AvaloniaProperty.Register<MavSwipeAction, Color?>(nameof(BackgroundColor));

    public static readonly StyledProperty<Color?> ForegroundColorProperty =
        AvaloniaProperty.Register<MavSwipeAction, Color?>(nameof(ForegroundColor));

    public static readonly StyledProperty<bool> IsDestructiveProperty =
        AvaloniaProperty.Register<MavSwipeAction, bool>(nameof(IsDestructive));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsDestructive
    {
        get => GetValue(IsDestructiveProperty);
        set => SetValue(IsDestructiveProperty, value);
    }

    public Action? OnTriggered { get; set; }
    public event EventHandler? Triggered;
    public event EventHandler? Clicked;

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

    public Color? BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public Color? ForegroundColor
    {
        get => GetValue(ForegroundColorProperty);
        set => SetValue(ForegroundColorProperty, value);
    }

    public MavSwipeAction()
    {
    }

    public MavSwipeAction(
        string label,
        string icon,
        Action? onTriggered = null,
        Color? backgroundColor = null,
        Color? foregroundColor = null)
    {
        Label = label;
        Icon = icon;
        OnTriggered = onTriggered;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    internal void Invoke()
    {
        OnTriggered?.Invoke();
        Triggered?.Invoke(this, EventArgs.Empty);
        Clicked?.Invoke(this, EventArgs.Empty);
        if (Command is { } cmd && cmd.CanExecute(CommandParameter))
            cmd.Execute(CommandParameter);
    }
}

/// <summary>
/// Section subheader label for <see cref="MavList"/>.
/// </summary>
public class MavListSubheader : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavListSubheader, string>(nameof(Text), string.Empty);

    private readonly TextBlock _label;

    static MavListSubheader()
    {
        TextProperty.Changed.AddClassHandler<MavListSubheader>((s, e) => s._label.Text = e.GetNewValue<string>());
    }

    public MavListSubheader() : this(string.Empty)
    {
    }

    public MavListSubheader(string text)
    {
        Padding = new Thickness(16, 12, 16, 4);
        _label = new TextBlock { Text = text };
        Content = _label;
        Text = text;

        MavTheme.Observe(this, (self, theme) =>
        {
            self._label.Apply(MavTypeRole.LabelLarge, theme.Typography);
            self._label.Foreground = MavTheme.BrushOf(theme.Color.Primary);
        });
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}

public enum MavListItemLines
{
    OneLine,
    TwoLine,
    ThreeLine,
}

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

    public MavListItem() : this(string.Empty)
    {
    }

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
            Content = row,
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

        Content = root;

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

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (LeadingSwipeAction is null && TrailingSwipeAction is null) return;
        if (MavGestureZones.ShouldDeferToSystem(this, e)) return;
        _swipeStart = e.GetPosition(this);
        _swipeTracking = true;
        _swiping = false;
    }

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

/// <summary>
/// Vertical stack container for <see cref="MavListItem"/>, <see cref="MavListSubheader"/>, and <see cref="MavDivider"/>
/// with <see cref="ItemsSource"/> (<c>INotifyCollectionChanged</c>) and C# collection initializer support.
/// </summary>
public class MavList : StackPanel, IEnumerable<Control>
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavList, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MavList, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<bool> ShowDividersProperty =
        AvaloniaProperty.Register<MavList, bool>(nameof(ShowDividers));

    public static readonly StyledProperty<double> DividerInsetStartProperty =
        AvaloniaProperty.Register<MavList, double>(nameof(DividerInsetStart));

    private readonly MavCollectionObserver _observer;
    private Func<object, Control>? _itemFactory;

    // Virtualization state (ItemsSource mode only). Manual AddItem/AddSubheader lists
    // keep every child attached and behave exactly as before.
    private List<object>? _sourceItems;
    private readonly Dictionary<int, Control> _realized = new();
    private readonly HeightIndex _heights = new();
    private ScrollViewer? _scrollHost;
    private Border? _topSpacer;
    private Border? _bottomSpacer;
    private int _realizedStart = -1;
    private int _realizedEnd = -1;
    private bool _realizing;

    private const double EstimatedItemHeight = 64;
    private const double OverscanPx = 480;
    private const int EvictionMargin = 120;

    static MavList()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MavList>((l, e) =>
            l.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MavList>((l, _) =>
            l.RebuildFromItemsSource(l.ItemsSource));
        ShowDividersProperty.Changed.AddClassHandler<MavList>((l, e) =>
        {
            if (e.GetNewValue<bool>())
                l.SyncChildDividers();
            else
                l.ClearChildDividers();
        });
    }

    public bool ShowDividers
    {
        get => GetValue(ShowDividersProperty);
        set => SetValue(ShowDividersProperty, value);
    }

    public double DividerInsetStart
    {
        get => GetValue(DividerInsetStartProperty);
        set => SetValue(DividerInsetStartProperty, value);
    }

    private void SyncChildDividers()
    {
        // An explicit per-item ShowDivider is preserved while the list does not
        // manage dividers. Turning ShowDividers on (or off) takes over the items.
        if (!ShowDividers)
            return;

        var items = Children.OfType<MavListItem>().ToList();
        for (int i = 0; i < items.Count; i++)
            items[i].ShowDivider = i < items.Count - 1;
    }

    private void ClearChildDividers()
    {
        foreach (var item in Children.OfType<MavListItem>())
            item.ShowDivider = false;
    }

    public MavList()
    {
        Orientation = Orientation.Vertical;
        Spacing = 0;
        _observer = new MavCollectionObserver(
            this,
            () => RebuildFromItemsSource(ItemsSource),
            OnCollectionChanged);

        // Dividers must follow items added after ShowDividers was assigned, which is
        // the usual C# object-initializer order: new MavList { ShowDividers = true }.
        // Realization inserts many children in one pass; syncing dividers for every
        // insert would turn that pass into O(n²).
        Children.CollectionChanged += (_, _) =>
        {
            if (!_realizing)
                SyncChildDividers();
        };

        AttachedToVisualTree += (_, _) => AttachScrollHost();
        DetachedFromVisualTree += (_, _) => DetachScrollHost();
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public Func<object, Control>? ItemFactory
    {
        get => _itemFactory;
        set
        {
            _itemFactory = value;
            if (ItemsSource is not null)
                RebuildFromItemsSource(ItemsSource);
        }
    }

    public MavList SetItemTemplate(Func<object, Control> factory)
    {
        ItemFactory = factory;
        return this;
    }

    public MavList AddItem(MavListItem item)
    {
        Children.Add(item);
        return this;
    }

    public MavList AddSubheader(string text)
    {
        Children.Add(new MavListSubheader(text));
        return this;
    }

    public MavList AddDivider(double insetStart = 0, double insetEnd = 0)
    {
        Children.Add(new MavDivider(insetStart, insetEnd));
        return this;
    }

    public void Add(Control item) => Children.Add(item);

    public IEnumerator<Control> GetEnumerator() => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        _sourceItems = src is null
            ? null
            : src.Cast<object?>().Where(static item => item is not null).Cast<object>().ToList();

        _realized.Clear();
        ResetHeightIndex(_sourceItems?.Count ?? 0);
        _realizedStart = -1;
        _realizedEnd = -1;
        RemoveSpacers();
        Children.Clear();

        if (_sourceItems is null)
            return;

        if (_scrollHost is null)
        {
            // Not attached yet: defer large sources until the viewport is known.
            // Small lists are realized eagerly so code that inspects Children before
            // attaching keeps working.
            if (VisualRoot is null && _sourceItems.Count > 60)
                return;

            RealizeAll();
        }
        else
            RealizeWindow(force: true);
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_sourceItems is null || _realizing)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add
                when e.NewStartingIndex >= 0 && e.NewItems is { Count: > 0 } added
                     && !ContainsNull(added):
                InsertItems(e.NewStartingIndex, added);
                break;

            case NotifyCollectionChangedAction.Remove
                when e.OldStartingIndex >= 0 && e.OldItems is { Count: > 0 } old:
                RemoveItems(e.OldStartingIndex, old.Count);
                break;

            case NotifyCollectionChangedAction.Replace
                when e.NewStartingIndex >= 0 && e.NewItems is { Count: > 0 } replaced
                     && e.OldItems is { Count: > 0 } oldReplaced
                     && oldReplaced.Count == replaced.Count
                     && !ContainsNull(replaced):
                ReplaceItems(e.NewStartingIndex, replaced);
                break;

            case NotifyCollectionChangedAction.Move
                when e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0
                     && e.OldItems is { Count: > 0 } moved:
                MoveItems(e.OldStartingIndex, e.NewStartingIndex, moved.Count);
                break;

            default:
                // Reset (and sources reporting unknown indices) keep the full path.
                RebuildFromItemsSource(ItemsSource);
                break;
        }
    }

    private static bool ContainsNull(IList items)
    {
        foreach (var item in items)
        {
            if (item is null)
                return true;
        }

        return false;
    }

    private void InsertItems(int index, IList added)
    {
        var oldCount = _sourceItems!.Count;
        var start = Math.Clamp(index, 0, oldCount);
        var count = added.Count;

        var newItems = new List<object>(count);
        foreach (var item in added)
            newItems.Add(item!);

        _sourceItems.InsertRange(start, newItems);
        _heights.InsertRange(start, count, EstimatedItemHeight);

        if (start < oldCount)
        {
            RemapRealized(oldIndex =>
                oldIndex >= start ? oldIndex + count : oldIndex);
        }

        // The new items may be inside the current window; let the realization
        // pass build only those while keeping the already built controls.
        RefreshAfterCollectionChange(appendedAtEnd: start == oldCount && count == 1);
        UpdateSpacers(_realizedStart, _realizedEnd);
    }

    private void RemoveItems(int start, int count)
    {
        var oldCount = _sourceItems!.Count;
        if (start < 0 || count <= 0 || start + count > oldCount)
        {
            RebuildFromItemsSource(ItemsSource);
            return;
        }

        _sourceItems.RemoveRange(start, count);
        _heights.RemoveRange(start, count);

        if (_scrollHost is null && VisualRoot is not null && start + count == oldCount)
        {
            RemoveRealizedTail(start, oldCount);
            UpdateSpacers(_realizedStart, _realizedEnd);
            return;
        }

        RemapRealized(oldIndex =>
            oldIndex < start ? oldIndex
            : oldIndex < start + count ? -1
            : oldIndex - count);

        RefreshAfterCollectionChange();
        UpdateSpacers(_realizedStart, _realizedEnd);
    }

    private void ReplaceItems(int start, IList replaced)
    {
        if (start < 0 || start + replaced.Count > _sourceItems!.Count)
        {
            RebuildFromItemsSource(ItemsSource);
            return;
        }

        for (var i = 0; i < replaced.Count; i++)
        {
            var index = start + i;
            _sourceItems[index] = replaced[i]!;
            _heights.Set(index, EstimatedItemHeight);

            // The old control shows the old item; drop it so the realization
            // pass builds the replacement. Neighbours stay realized.
            if (_realized.Remove(index, out var oldControl))
                Children.Remove(oldControl);
        }

        RefreshAfterCollectionChange();
        UpdateSpacers(_realizedStart, _realizedEnd);
    }

    private void MoveItems(int oldIndex, int newIndex, int count)
    {
        var total = _sourceItems!.Count;
        if (oldIndex < 0 || newIndex < 0 || count <= 0
            || oldIndex + count > total || newIndex + count > total)
        {
            RebuildFromItemsSource(ItemsSource);
            return;
        }

        if (oldIndex == newIndex)
            return;

        var movedItems = _sourceItems.GetRange(oldIndex, count);
        _sourceItems.RemoveRange(oldIndex, count);
        _sourceItems.InsertRange(newIndex, movedItems);
        _heights.MoveRange(oldIndex, newIndex, count);

        RemapRealized(oldIndex2 => MapMovedIndex(oldIndex2, oldIndex, newIndex, count, total));

        RefreshAfterCollectionChange();
        UpdateSpacers(_realizedStart, _realizedEnd);
    }

    private static int MapMovedIndex(int index, int oldIndex, int newIndex, int count, int total)
    {
        if (index >= oldIndex && index < oldIndex + count)
            return newIndex + (index - oldIndex);

        if (oldIndex < newIndex)
        {
            if (index >= oldIndex + count && index <= newIndex)
                return index - count;
        }
        else if (index >= newIndex && index < oldIndex)
        {
            return index + count;
        }

        return index < total ? index : -1;
    }

    private void RemapRealized(Func<int, int> map)
    {
        var remapped = new Dictionary<int, Control>(_realized.Count);
        foreach (var (index, control) in _realized)
        {
            var newIndex = map(index);
            if (newIndex >= 0 && newIndex < _sourceItems!.Count)
                remapped[newIndex] = control;
        }

        _realized.Clear();
        foreach (var (index, control) in remapped)
            _realized[index] = control;
    }

    private void RefreshAfterCollectionChange(bool appendedAtEnd = false)
    {
        if (_sourceItems is null)
            return;

        if (appendedAtEnd && _scrollHost is null && VisualRoot is not null)
        {
            RealizeAppendedItem(_sourceItems.Count - 1);
            return;
        }

        if (_scrollHost is not null)
        {
            RealizeWindow(force: true);
            return;
        }

        if (VisualRoot is null && _sourceItems.Count > 60)
        {
            // Mirrors RebuildFromItemsSource: defer large unattached sources.
            _realized.Clear();
            _realizedStart = -1;
            _realizedEnd = -1;
            RemoveSpacers();
            Children.Clear();
            return;
        }

        RealizeAllIncremental();
    }

    private void RealizeAppendedItem(int index)
    {
        _realizing = true;
        try
        {
            var control = GetOrBuild(index);
            Children.Add(control);

            // Only the former last item changes divider state on append.
            if (ShowDividers && index > 0
                && _realized.TryGetValue(index - 1, out var previous)
                && previous is MavListItem previousItem)
            {
                previousItem.ShowDivider = true;
            }

            if (control is MavListItem item)
                item.ShowDivider = ShowDividers && index < _sourceItems!.Count - 1;

            _realizedStart = 0;
            _realizedEnd = index;
        }
        finally
        {
            _realizing = false;
        }
    }

    private void RemoveRealizedTail(int start, int oldCount)
    {
        _realizing = true;
        try
        {
            for (var i = oldCount - 1; i >= start; i--)
            {
                if (_realized.Remove(i, out var control))
                    Children.Remove(control);
            }

            if (start > 0
                && _realized.TryGetValue(start - 1, out var previous)
                && previous is MavListItem previousItem)
            {
                previousItem.ShowDivider = ShowDividers && start - 1 < _sourceItems!.Count - 1;
            }

            _realizedStart = _sourceItems!.Count == 0 ? -1 : 0;
            _realizedEnd = _sourceItems.Count - 1;
        }
        finally
        {
            _realizing = false;
        }
    }

    private void RealizeAllIncremental()
    {
        if (_sourceItems is null)
            return;

        _realizing = true;
        try
        {
            RemoveSpacers();

            var desired = new List<Control>(_sourceItems.Count);
            for (var i = 0; i < _sourceItems.Count; i++)
                desired.Add(GetOrBuild(i));

            SyncChildrenToDesired(desired);

            _realizedStart = _sourceItems.Count == 0 ? -1 : 0;
            _realizedEnd = _sourceItems.Count - 1;
        }
        finally
        {
            _realizing = false;
        }

        SyncVirtualDividers();
    }

    private void AttachScrollHost()
    {
        var host = this.FindAncestorOfType<ScrollViewer>();
        if (ReferenceEquals(host, _scrollHost))
            return;

        DetachScrollHost();
        _scrollHost = host;
        if (_scrollHost is null)
            return;

        _scrollHost.ScrollChanged += OnScrollChanged;
        _scrollHost.SizeChanged += OnScrollHostSizeChanged;

        if (_sourceItems is not null)
            RealizeWindow(force: true);
    }

    private void DetachScrollHost()
    {
        if (_scrollHost is null)
            return;

        _scrollHost.ScrollChanged -= OnScrollChanged;
        _scrollHost.SizeChanged -= OnScrollHostSizeChanged;
        _scrollHost = null;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => RealizeWindow();

    private void OnScrollHostSizeChanged(object? sender, SizeChangedEventArgs e) => RealizeWindow(force: true);

    /// <summary>
    /// Builds every item. Used when the list is not hosted in a <see cref="ScrollViewer"/>,
    /// where there is no viewport to virtualize against.
    /// </summary>
    private void RealizeAll()
    {
        if (_sourceItems is null)
            return;

        _realizing = true;
        try
        {
            RemoveSpacers();
            _realized.Clear();

            var desired = new List<Control>(_sourceItems.Count);
            for (var i = 0; i < _sourceItems.Count; i++)
            {
                var control = BuildItem(i);
                _realized[i] = control;
                desired.Add(control);
            }

            SyncChildrenToDesired(desired);

            _realizedStart = _sourceItems.Count == 0 ? -1 : 0;
            _realizedEnd = _sourceItems.Count - 1;
        }
        finally
        {
            _realizing = false;
        }

        SyncVirtualDividers();
    }

    private void RealizeWindow(bool force = false)
    {
        if (_sourceItems is null || _realizing)
            return;

        if (_sourceItems.Count == 0)
        {
            Children.Clear();
            RemoveSpacers();
            _realizedStart = _realizedEnd = -1;
            return;
        }

        if (_scrollHost is null)
        {
            RealizeAll();
            return;
        }

        var origin = this.TranslatePoint(default, _scrollHost);
        if (origin is null)
            return;

        var viewport = _scrollHost.Viewport.Height;
        if (double.IsNaN(viewport) || viewport <= 0)
            viewport = _scrollHost.Bounds.Height;
        if (double.IsNaN(viewport) || viewport <= 0)
            viewport = 800; // Before the first layout pass: realize a reasonable first page.

        var listTop = origin.Value.Y;
        var from = Math.Max(0, -listTop - OverscanPx);
        var to = -listTop + viewport + OverscanPx;

        var (start, end) = FindRange(from, to);
        if (!force && start == _realizedStart && end == _realizedEnd)
        {
            UpdateSpacers(start, end);
            return;
        }

        _realizing = true;
        try
        {
            RemoveSpacers();

            var desired = new List<Control>(Math.Max(0, end - start + 1));
            for (var i = start; i <= end; i++)
                desired.Add(GetOrBuild(i));

            // Keep only the desired window in Children, in ascending order.
            SyncChildrenToDesired(desired);

            _realizedStart = start;
            _realizedEnd = end;

            EvictDistant(start, end);
            AddSpacers(start, end);
        }
        finally
        {
            _realizing = false;
        }

        SyncVirtualDividers();
    }

    /// <summary>
    /// Applies the desired realized window without an IndexOf scan per child.
    /// The common prefix is reused; the remaining suffix is removed and re-added
    /// in order. The control instances themselves are preserved.
    /// </summary>
    private void SyncChildrenToDesired(List<Control> desired)
    {
        var common = 0;
        var commonLimit = Math.Min(Children.Count, desired.Count);
        while (common < commonLimit && ReferenceEquals(Children[common], desired[common]))
            common++;

        if (common == Children.Count && common == desired.Count)
            return;

        for (var i = Children.Count - 1; i >= common; i--)
            Children.RemoveAt(i);

        for (var i = common; i < desired.Count; i++)
            Children.Add(desired[i]);
    }

    private (int Start, int End) FindRange(double from, double to)
    {
        var count = _heights.Count;
        if (count == 0)
            return (-1, -1);

        var start = _heights.IndexAtOffset(Math.Max(0, from));
        var end = _heights.IndexAtOffset(Math.Max(from, to));
        if (end < start)
            end = start;

        return (start, Math.Min(count - 1, end));
    }

    private void ResetHeightIndex(int count) =>
        _heights.Reset(count, EstimatedItemHeight);

    private void SetItemHeight(int index, double height) => _heights.Set(index, height);

    private double SumHeights(int startInclusive, int endExclusive) =>
        _heights.Sum(startInclusive, endExclusive);

    private Control GetOrBuild(int index)
    {
        if (_realized.TryGetValue(index, out var existing))
            return existing;

        var control = BuildItem(index);
        _realized[index] = control;
        return control;
    }

    private Control BuildItem(int index)
    {
        var item = _sourceItems![index];
        return MavVisualTreeHelpers.BuildTemplateControl(item, ItemTemplate, _itemFactory)
               ?? new MavListItem(item.ToString() ?? string.Empty);
    }

    private void EvictDistant(int start, int end)
    {
        if (_realized.Count <= (end - start + 1) + 2 * EvictionMargin)
            return;

        foreach (var key in _realized.Keys.ToList())
        {
            if (key < start - EvictionMargin || key > end + EvictionMargin)
                _realized.Remove(key);
        }
    }

    private void UpdateSpacers(int start, int end)
    {
        if (_topSpacer is not null)
        {
            var top = SumHeights(0, Math.Max(0, start));
            _topSpacer.Height = top;
            _topSpacer.IsVisible = top > 0;
        }

        if (_bottomSpacer is not null && _sourceItems is not null)
        {
            var bottom = SumHeights(Math.Min(_sourceItems.Count, end + 1), _sourceItems.Count);
            _bottomSpacer.Height = bottom;
            _bottomSpacer.IsVisible = bottom > 0;
        }
    }

    private void AddSpacers(int start, int end)
    {
        _topSpacer ??= new Border { IsHitTestVisible = false };
        _bottomSpacer ??= new Border { IsHitTestVisible = false };

        var top = SumHeights(0, Math.Max(0, start));
        _topSpacer.Height = top;
        _topSpacer.IsVisible = top > 0;

        var bottom = _sourceItems is null
            ? 0
            : SumHeights(Math.Min(_sourceItems.Count, end + 1), _sourceItems.Count);
        _bottomSpacer.Height = bottom;
        _bottomSpacer.IsVisible = bottom > 0;

        Children.Insert(0, _topSpacer);
        Children.Add(_bottomSpacer);
    }

    private void RemoveSpacers()
    {
        if (_topSpacer is not null)
            Children.Remove(_topSpacer);
        if (_bottomSpacer is not null)
            Children.Remove(_bottomSpacer);
    }

    private void SyncVirtualDividers()
    {
        if (_sourceItems is null)
            return;

        foreach (var (index, control) in _realized)
        {
            if (control is not MavListItem item)
                continue;

            var showDivider = ShowDividers && index < _sourceItems.Count - 1;
            if (item.ShowDivider != showDivider)
                item.ShowDivider = showDivider;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        if (_sourceItems is not null && _realizedEnd >= _realizedStart)
        {
            for (var i = _realizedStart; i <= _realizedEnd; i++)
            {
                if (_realized.TryGetValue(i, out var control) && control.Bounds.Height > 0)
                    SetItemHeight(i, control.Bounds.Height);
            }
        }

        return result;
    }

    /// <summary>
    /// Item heights plus a Fenwick prefix tree. Appends and tail removals are
    /// amortized O(log n); middle insertions and moves shift the backing list once
    /// and rebuild the tree once. The previous array implementation copied and
    /// rebuilt the entire tree for every single-item append.
    /// </summary>
    private sealed class HeightIndex
    {
        private readonly List<double> _values = [];
        private readonly List<double> _tree = [0.0];

        public int Count => _values.Count;

        public void Reset(int count, double initial)
        {
            _values.Clear();
            _values.EnsureCapacity(count);
            _tree.Clear();
            _tree.EnsureCapacity(count + 1);
            _tree.Add(0);

            for (var i = 0; i < count; i++)
            {
                _values.Add(initial);
                _tree.Add(0);
            }

            RebuildTree();
        }

        public void InsertRange(int index, int count, double height)
        {
            if (count <= 0)
                return;

            index = Math.Clamp(index, 0, _values.Count);
            if (index == _values.Count)
            {
                Append(count, height);
                return;
            }

            var fill = new double[count];
            Array.Fill(fill, height);
            _values.InsertRange(index, fill);
            RebuildTree();
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0)
                return;

            index = Math.Clamp(index, 0, _values.Count);
            count = Math.Min(count, _values.Count - index);
            if (count <= 0)
                return;

            var oldCount = _values.Count;
            if (index + count == oldCount)
            {
                // Removing a suffix only affects Fenwick nodes that contain the
                // removed indices; no full n-element rebuild is needed.
                for (var i = index; i < oldCount; i++)
                {
                    var value = _values[i];
                    for (var parent = i + 1; parent <= oldCount; parent += parent & -parent)
                        _tree[parent] -= value;
                }

                _values.RemoveRange(index, count);
                _tree.RemoveRange(index + 1, count);
                return;
            }

            _values.RemoveRange(index, count);
            RebuildTree();
        }

        public void MoveRange(int oldIndex, int newIndex, int count)
        {
            if (count <= 0 || oldIndex == newIndex)
                return;

            if (oldIndex < 0 || newIndex < 0
                || oldIndex + count > _values.Count
                || newIndex + count > _values.Count)
                return;

            var block = _values.GetRange(oldIndex, count);
            _values.RemoveRange(oldIndex, count);
            _values.InsertRange(newIndex, block);
            RebuildTree();
        }

        public void Set(int index, double height)
        {
            if ((uint)index >= (uint)_values.Count || height <= 0)
                return;

            var delta = height - _values[index];
            if (Math.Abs(delta) < 0.01)
                return;

            _values[index] = height;
            for (var parent = index + 1; parent < _tree.Count; parent += parent & -parent)
                _tree[parent] += delta;
        }

        public double Sum(int startInclusive, int endExclusive)
        {
            var start = Math.Clamp(startInclusive, 0, _values.Count);
            var end = Math.Clamp(endExclusive, start, _values.Count);
            return Prefix(end) - Prefix(start);
        }

        /// <summary>
        /// Finds the first item whose cumulative height reaches <paramref name="offset"/>.
        /// Fenwick-tree lower bound keeps scrolling independent of the number of items.
        /// </summary>
        public int IndexAtOffset(double offset)
        {
            if (_values.Count == 0)
                return -1;
            if (offset <= 0)
                return 0;

            var total = Prefix(_values.Count);
            if (offset >= total)
                return _values.Count - 1;

            var index = 0;
            var step = 1;
            while (step <= _values.Count)
                step <<= 1;

            var accumulated = 0.0;
            for (step >>= 1; step > 0; step >>= 1)
            {
                var next = index + step;
                if (next <= _values.Count && accumulated + _tree[next] < offset)
                {
                    index = next;
                    accumulated += _tree[next];
                }
            }

            return Math.Min(index, _values.Count - 1);
        }

        private void Append(int count, double height)
        {
            var oldCount = _values.Count;
            for (var k = 0; k < count; k++)
            {
                var index = oldCount + k + 1;
                var rangeStart = index - (index & -index);

                // A new Fenwick node must contain the previous values in its
                // range; those nodes did not exist when the earlier values were
                // appended. Existing ancestors only need the new value.
                var rangeSum = Prefix(index - 1) - Prefix(rangeStart);
                _values.Add(height);
                _tree.Add(0);
                _tree[index] = rangeSum + height;

                var newCount = _values.Count;
                for (var parent = index + (index & -index);
                     parent <= newCount;
                     parent += parent & -parent)
                {
                    _tree[parent] += height;
                }
            }
        }

        private double Prefix(int count)
        {
            var sum = 0.0;
            for (var i = Math.Min(count, _values.Count); i > 0; i -= i & -i)
                sum += _tree[i];
            return sum;
        }

        private void RebuildTree()
        {
            var count = _values.Count;
            _tree.Clear();
            _tree.EnsureCapacity(count + 1);
            for (var i = 0; i <= count; i++)
                _tree.Add(0);

            for (var i = 1; i <= count; i++)
            {
                _tree[i] += _values[i - 1];
                var parent = i + (i & -i);
                if (parent <= count)
                    _tree[parent] += _tree[i];
            }
        }
    }
}
