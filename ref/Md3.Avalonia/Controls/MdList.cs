using System.Collections;
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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed class MdSwipeAction : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdSwipeAction, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdSwipeAction, string>(nameof(Icon), MdIcons.Check);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdSwipeAction, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdSwipeAction, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<Color?> BackgroundColorProperty =
        AvaloniaProperty.Register<MdSwipeAction, Color?>(nameof(BackgroundColor));

    public static readonly StyledProperty<Color?> ForegroundColorProperty =
        AvaloniaProperty.Register<MdSwipeAction, Color?>(nameof(ForegroundColor));

    public static readonly StyledProperty<bool> IsDestructiveProperty =
        AvaloniaProperty.Register<MdSwipeAction, bool>(nameof(IsDestructive));

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

    public MdSwipeAction()
    {
    }

    public MdSwipeAction(
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
/// Material Design 3 1dp horizontal or vertical divider with optional leading/trailing insets.
/// </summary>
public class MdDivider : Border
{
    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<MdDivider, bool>(nameof(IsVertical));

    public static readonly StyledProperty<double> InsetStartProperty =
        AvaloniaProperty.Register<MdDivider, double>(nameof(InsetStart));

    public static readonly StyledProperty<double> InsetEndProperty =
        AvaloniaProperty.Register<MdDivider, double>(nameof(InsetEnd));

    static MdDivider()
    {
        IsVerticalProperty.Changed.AddClassHandler<MdDivider>((d, _) => d.SyncMetrics());
        InsetStartProperty.Changed.AddClassHandler<MdDivider>((d, _) => d.SyncMetrics());
        InsetEndProperty.Changed.AddClassHandler<MdDivider>((d, _) => d.SyncMetrics());
    }

    public MdDivider() : this(0, 0, false)
    {
    }

    public MdDivider(double insetStart = 0, double insetEnd = 0, bool vertical = false)
    {
        InsetStart = insetStart;
        InsetEnd = insetEnd;
        IsVertical = vertical;
        SyncMetrics();

        MdTheme.Observe(this, (self, theme) =>
        {
            self.Background = MdTheme.BrushOf(theme.Color.OutlineVariant);
        });
    }

    public bool IsVertical
    {
        get => GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    public double InsetStart
    {
        get => GetValue(InsetStartProperty);
        set => SetValue(InsetStartProperty, value);
    }

    public double InsetEnd
    {
        get => GetValue(InsetEndProperty);
        set => SetValue(InsetEndProperty, value);
    }

    private void SyncMetrics()
    {
        if (IsVertical)
        {
            Width = 1;
            Height = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Stretch;
            Margin = new Thickness(0, InsetStart, 0, InsetEnd);
        }
        else
        {
            Height = 1;
            Width = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Center;
            Margin = new Thickness(InsetStart, 0, InsetEnd, 0);
        }
    }
}

/// <summary>
/// Section subheader label for <see cref="MdList"/>.
/// </summary>
public class MdListSubheader : MdContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdListSubheader, string>(nameof(Text), string.Empty);

    private readonly TextBlock _label;

    static MdListSubheader()
    {
        TextProperty.Changed.AddClassHandler<MdListSubheader>((s, e) => s._label.Text = e.GetNewValue<string>());
    }

    public MdListSubheader() : this(string.Empty)
    {
    }

    public MdListSubheader(string text)
    {
        Padding = new Thickness(16, 12, 16, 4);
        _label = new TextBlock { Text = text };
        Content = _label;
        Text = text;

        MdTheme.Observe(this, (self, theme) =>
        {
            self._label.Apply(MdTypeRole.LabelLarge, theme.Typography);
            self._label.Foreground = MdTheme.BrushOf(theme.Color.Primary);
        });
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}

public enum MdListItemLines
{
    OneLine,
    TwoLine,
    ThreeLine,
}

/// <summary>
/// Material Design 3 one-, two-, or three-line list item with leading/trailing icons, custom controls,
/// and optional horizontal swipe actions (<see cref="LeadingSwipeAction"/> / <see cref="TrailingSwipeAction"/>).
/// </summary>
public class MdListItem : MdContentView, IMdDraggableControl
{
    public static readonly StyledProperty<string> HeadlineProperty =
        AvaloniaProperty.Register<MdListItem, string>(nameof(Headline), string.Empty);

    public static readonly StyledProperty<MdListItemLines> LinesProperty =
        AvaloniaProperty.Register<MdListItem, MdListItemLines>(nameof(Lines), MdListItemLines.OneLine);

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MdListItem, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> OverlineProperty =
        AvaloniaProperty.Register<MdListItem, string?>(nameof(Overline));

    public static readonly StyledProperty<string?> TrailingTextProperty =
        AvaloniaProperty.Register<MdListItem, string?>(nameof(TrailingText));

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MdListItem, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MdListItem, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MdListItem, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowDividerProperty =
        AvaloniaProperty.Register<MdListItem, bool>(nameof(ShowDivider));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdListItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdListItem, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<Control?> LeadingViewProperty =
        AvaloniaProperty.Register<MdListItem, Control?>(nameof(LeadingView));

    public static readonly StyledProperty<Control?> TrailingViewProperty =
        AvaloniaProperty.Register<MdListItem, Control?>(nameof(TrailingView));

    public static readonly StyledProperty<MdSwipeAction?> LeadingSwipeActionProperty =
        AvaloniaProperty.Register<MdListItem, MdSwipeAction?>(nameof(LeadingSwipeAction));

    public static readonly StyledProperty<MdSwipeAction?> TrailingSwipeActionProperty =
        AvaloniaProperty.Register<MdListItem, MdSwipeAction?>(nameof(TrailingSwipeAction));

    public static readonly StyledProperty<ICommand?> TrailingIconCommandProperty =
        AvaloniaProperty.Register<MdListItem, ICommand?>(nameof(TrailingIconCommand));

    public static readonly StyledProperty<object?> TrailingIconCommandParameterProperty =
        AvaloniaProperty.Register<MdListItem, object?>(nameof(TrailingIconCommandParameter));

    private readonly MdInkSurface _surface;
    private readonly Border _swipeBg;
    private readonly MdIcon _swipeIcon;
    private readonly TextBlock _swipeLabel;
    private readonly StackPanel _swipeContent;
    private readonly Border _leadingHost;
    private readonly MdIcon _leadingIconView;
    private readonly TextBlock _overlineLabel;
    private readonly TextBlock _headlineLabel;
    private readonly TextBlock _supportingLabel;
    private readonly StackPanel _trailingHost;
    private readonly TextBlock _trailingTextLabel;
    private readonly Border _trailingIconHit;
    private readonly MdIcon _trailingIconView;
    private readonly MdDivider _divider;
    private Control? _customLeading;
    private Control? _customTrailing;
    private Point _swipeStart;
    private double _swipeOffset;
    private bool _swipeTracking;
    private bool _swiping;

    public event EventHandler? Clicked;
    public event EventHandler? TrailingIconClicked;

    static MdListItem()
    {
        HeadlineProperty.Changed.AddClassHandler<MdListItem>((i, _) =>
        {
            i.Sync();
            i.SyncAutomation();
        });
        SupportingTextProperty.Changed.AddClassHandler<MdListItem>((i, _) =>
        {
            i.Sync();
            i.SyncAutomation();
        });
        OverlineProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.Sync());
        TrailingTextProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.Sync());
        LeadingIconProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.Sync());
        TrailingIconProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.Sync());
        IsSelectedProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.ApplyTheme(MdTheme.Current));
        ShowDividerProperty.Changed.AddClassHandler<MdListItem>((i, e) => i._divider.IsVisible = e.GetNewValue<bool>());
        CommandProperty.Changed.AddClassHandler<MdListItem>((i, e) => i._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MdListItem>((i, e) => i._surface.CommandParameter = e.GetNewValue<object?>());
        LeadingSwipeActionProperty.Changed.AddClassHandler<MdListItem>((i, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<MdSwipeAction?>());
            i.ApplyTheme(MdTheme.Current);
        });
        TrailingSwipeActionProperty.Changed.AddClassHandler<MdListItem>((i, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<MdSwipeAction?>());
            i.ApplyTheme(MdTheme.Current);
        });
        LeadingViewProperty.Changed.AddClassHandler<MdListItem>((i, e) =>
        {
            i._customLeading = e.GetNewValue<Control?>();
            MdVisualTreeHelpers.DetachFromParent(i._customLeading);
            i.Sync();
        });
        TrailingViewProperty.Changed.AddClassHandler<MdListItem>((i, e) =>
        {
            if (i._customTrailing is not null)
                i._trailingHost.Children.Remove(i._customTrailing);
            i._customTrailing = e.GetNewValue<Control?>();
            if (i._customTrailing is not null)
            {
                MdVisualTreeHelpers.DetachFromParent(i._customTrailing);
                i._trailingHost.Children.Add(i._customTrailing);
            }
            i.Sync();
        });
        LeadingSwipeActionProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.ApplyTheme(MdTheme.Current));
        TrailingSwipeActionProperty.Changed.AddClassHandler<MdListItem>((i, _) => i.ApplyTheme(MdTheme.Current));
    }

    public MdListItem() : this(string.Empty)
    {
    }

    public MdListItem(
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
        _leadingIconView = new MdIcon { Size = 24, UseThemeDefaultColor = false };
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
        _trailingIconView = new MdIcon { Size = 24, UseThemeDefaultColor = false };
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

        _surface = new MdInkSurface
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

        _swipeIcon = new MdIcon { Size = 22, UseThemeDefaultColor = false, VerticalAlignment = VerticalAlignment.Center };
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

        var itemLayer = new Grid { ClipToBounds = true };
        itemLayer.Children.Add(_swipeBg);
        itemLayer.Children.Add(_surface);

        _divider = new MdDivider { IsVisible = showDivider };

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
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MdAutomation.SetName(this, Headline);
        MdAutomation.SetHelpText(this, SupportingText);
        MdAutomation.SetControlType(this, AutomationControlType.ListItem);
    }

    public string Headline
    {
        get => GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public MdListItemLines Lines
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

    public MdSwipeAction? LeadingSwipeAction
    {
        get => GetValue(LeadingSwipeActionProperty);
        set => SetValue(LeadingSwipeActionProperty, value);
    }

    public MdSwipeAction? TrailingSwipeAction
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
                MdVisualTreeHelpers.DetachFromParent(_customLeading);
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
                MdVisualTreeHelpers.DetachFromParent(_leadingIconView);
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
        _surface.MinHeight = Math.Max(44, minH + MdTheme.Current.DensityOffset);
    }

    private void ApplyTheme(MdTheme t)
    {
        Sync();
        var c = t.Color;
        _overlineLabel.Apply(MdTypeRole.LabelSmall, t.Typography);
        _headlineLabel.Apply(MdTypeRole.BodyLarge, t.Typography);
        _supportingLabel.Apply(MdTypeRole.BodyMedium, t.Typography);
        _trailingTextLabel.Apply(MdTypeRole.LabelSmall, t.Typography);

        var bg = IsSelected
            ? c.SecondaryContainer
            : ((LeadingSwipeAction is not null || TrailingSwipeAction is not null) ? c.Surface : Colors.Transparent);
        var fg = IsSelected ? c.OnSecondaryContainer : c.OnSurface;
        var subFg = IsSelected ? c.OnSecondaryContainer : c.OnSurfaceVariant;

        _surface.FillColor = bg;
        _surface.RippleColor = fg;
        _headlineLabel.Foreground = MdTheme.BrushOf(fg);
        _overlineLabel.Foreground = MdTheme.BrushOf(subFg);
        _supportingLabel.Foreground = MdTheme.BrushOf(subFg);
        _trailingTextLabel.Foreground = MdTheme.BrushOf(subFg);
        _leadingIconView.Color = subFg;
        _trailingIconView.Color = subFg;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (LeadingSwipeAction is null && TrailingSwipeAction is null) return;
        if (MdGestureZones.ShouldDeferToSystem(this, e)) return;
        _swipeStart = e.GetPosition(this);
        _swipeTracking = true;
        _swiping = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_swipeTracking) return;

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

        if (!_swiping) return;
        e.Pointer.Capture(null);
        e.Handled = true;

        var triggeredAction = _swipeOffset >= 72
            ? LeadingSwipeAction
            : (_swipeOffset <= -72 ? TrailingSwipeAction : null);

        var start = _swipeOffset;
        MdMotion.Animate(this, "li_swipe", start, 0, MdMotion.Short4, MdMotion.CubicOut, v =>
        {
            _swipeOffset = v;
            UpdateSwipeVisual();
        }, () =>
        {
            _swiping = false;
            _swipeBg.IsVisible = false;
            triggeredAction?.Invoke();
        });
    }

    private void UpdateSwipeVisual()
    {
        _surface.SetTranslation(_swipeOffset, 0);
        var c = MdTheme.Current.Color;
        var action = _swipeOffset >= 0 ? LeadingSwipeAction : TrailingSwipeAction;
        if (action is null || Math.Abs(_swipeOffset) < 2)
        {
            _swipeBg.IsVisible = false;
            return;
        }

        _swipeBg.IsVisible = true;
        var bg = action.BackgroundColor ?? (_swipeOffset > 0 ? c.TertiaryContainer : c.ErrorContainer);
        var fg = action.ForegroundColor ?? (_swipeOffset > 0 ? c.OnTertiaryContainer : c.OnErrorContainer);
        _swipeBg.Background = MdTheme.BrushOf(bg);
        _swipeIcon.Glyph = action.Icon;
        _swipeIcon.Color = fg;
        _swipeLabel.Text = action.Label;
        _swipeLabel.Foreground = MdTheme.BrushOf(fg);
        _swipeContent.HorizontalAlignment = _swipeOffset > 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        _surface.FillColor = IsSelected ? c.SecondaryContainer : c.Surface;
    }
}

/// <summary>
/// Vertical stack container for <see cref="MdListItem"/>, <see cref="MdListSubheader"/>, and <see cref="MdDivider"/>
/// with <see cref="ItemsSource"/> (<c>INotifyCollectionChanged</c>) and C# collection initializer support.
/// </summary>
public class MdList : StackPanel, IEnumerable<Control>
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdList, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MdList, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<bool> ShowDividersProperty =
        AvaloniaProperty.Register<MdList, bool>(nameof(ShowDividers));

    public static readonly StyledProperty<double> DividerInsetStartProperty =
        AvaloniaProperty.Register<MdList, double>(nameof(DividerInsetStart));

    private readonly MdCollectionObserver _observer;
    private Func<object, Control>? _itemFactory;

    // Virtualization state (ItemsSource mode only). Manual AddItem/AddSubheader lists
    // keep every child attached and behave exactly as before.
    private List<object>? _sourceItems;
    private readonly Dictionary<int, Control> _realized = new();
    private readonly Dictionary<int, double> _itemHeights = new();
    private ScrollViewer? _scrollHost;
    private Border? _topSpacer;
    private Border? _bottomSpacer;
    private int _realizedStart = -1;
    private int _realizedEnd = -1;
    private bool _realizing;

    private const double EstimatedItemHeight = 64;
    private const double OverscanPx = 480;
    private const int EvictionMargin = 120;

    static MdList()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MdList>((l, e) =>
            l.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MdList>((l, _) =>
            l.RebuildFromItemsSource(l.ItemsSource));
        ShowDividersProperty.Changed.AddClassHandler<MdList>((l, e) =>
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

        var items = Children.OfType<MdListItem>().ToList();
        for (int i = 0; i < items.Count; i++)
            items[i].ShowDivider = i < items.Count - 1;
    }

    private void ClearChildDividers()
    {
        foreach (var item in Children.OfType<MdListItem>())
            item.ShowDivider = false;
    }

    public MdList()
    {
        Orientation = Orientation.Vertical;
        Spacing = 0;
        _observer = new MdCollectionObserver(() => RebuildFromItemsSource(ItemsSource));

        // Dividers must follow items added after ShowDividers was assigned, which is
        // the usual C# object-initializer order: new MdList { ShowDividers = true }.
        Children.CollectionChanged += (_, _) => SyncChildDividers();

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

    public MdList SetItemTemplate(Func<object, Control> factory)
    {
        ItemFactory = factory;
        return this;
    }

    public MdList AddItem(MdListItem item)
    {
        Children.Add(item);
        return this;
    }

    public MdList AddSubheader(string text)
    {
        Children.Add(new MdListSubheader(text));
        return this;
    }

    public MdList AddDivider(double insetStart = 0, double insetEnd = 0)
    {
        Children.Add(new MdDivider(insetStart, insetEnd));
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
        _itemHeights.Clear();
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
            Children.Clear();
            _realized.Clear();
            for (var i = 0; i < _sourceItems.Count; i++)
            {
                var control = BuildItem(i);
                _realized[i] = control;
                Children.Add(control);
            }

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
            var desiredSet = new HashSet<Control>(desired);
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                if (!desiredSet.Contains(Children[i]))
                    Children.RemoveAt(i);
            }

            for (var i = 0; i < desired.Count; i++)
            {
                var child = desired[i];
                var current = Children.IndexOf(child);
                if (current < 0)
                    Children.Insert(Math.Min(i, Children.Count), child);
                else if (current != i)
                {
                    Children.RemoveAt(current);
                    Children.Insert(Math.Min(i, Children.Count), child);
                }
            }

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

    private (int Start, int End) FindRange(double from, double to)
    {
        if (_sourceItems is null || _sourceItems.Count == 0)
            return (-1, -1);

        var count = _sourceItems.Count;
        var start = 0;
        var accumulated = 0.0;
        for (var i = 0; i < count; i++)
        {
            var height = HeightOf(i);
            if (accumulated + height >= from)
            {
                start = i;
                break;
            }

            accumulated += height;
            if (i == count - 1)
                start = count - 1;
        }

        var end = start;
        accumulated = SumHeights(0, start);
        for (var i = start; i < count; i++)
        {
            accumulated += HeightOf(i);
            end = i;
            if (accumulated >= to)
                break;
        }

        return (start, end);
    }

    private double HeightOf(int index) =>
        _itemHeights.TryGetValue(index, out var measured) && measured > 0
            ? measured
            : EstimatedItemHeight;

    private double SumHeights(int startInclusive, int endExclusive)
    {
        var total = 0.0;
        for (var i = startInclusive; i < endExclusive; i++)
            total += HeightOf(i);
        return total;
    }

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
        return MdVisualTreeHelpers.BuildTemplateControl(item, ItemTemplate, _itemFactory)
               ?? new MdListItem(item.ToString() ?? string.Empty);
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
            if (control is MdListItem item)
                item.ShowDivider = ShowDividers && index < _sourceItems.Count - 1;
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
                    _itemHeights[i] = control.Bounds.Height;
            }
        }

        return result;
    }
}
