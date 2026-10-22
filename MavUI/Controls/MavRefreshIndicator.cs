using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

public enum MavRefreshIndicatorState
{
    Idle,
    Pulling,
    Armed,
    Refreshing,
}

/// <summary>
/// Material Design 3 pull-to-refresh host that wraps scrollable content and reveals
/// an elevated circular progress / loading indicator pill when pulled from the top edge.
/// </summary>
public class MavRefreshIndicator : MavContentView
{
    private const double PullStartDistance = 8.0;

    public static readonly StyledProperty<bool> IsRefreshingProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, bool>(nameof(IsRefreshing));

    public static readonly StyledProperty<double> TriggerDistanceProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, double>(nameof(TriggerDistance), 72.0);

    public static readonly StyledProperty<double> DisplacementProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, double>(nameof(Displacement), 52.0);

    public static readonly StyledProperty<double> MaxPullDistanceProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, double>(nameof(MaxPullDistance), 136.0);

    public static readonly StyledProperty<string> StatusTextProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, string>(nameof(StatusText), "Pull to refresh");

    public static readonly StyledProperty<string> ArmedTextProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, string>(nameof(ArmedText), "Release to refresh");

    public static readonly StyledProperty<string> RefreshingTextProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, string>(nameof(RefreshingText), "Refreshing…");

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavRefreshIndicator, Control?>(nameof(Body));

    private readonly Grid _root;
    private readonly Border _bodyHost;
    private readonly Border _indicatorHost;
    private readonly MavSurface _pill;
    private readonly MavCircularProgress _pullSpinner;
    private readonly MavLoadingIndicator _refreshingLoader;
    private readonly TextBlock _stateLabel;

    private double _pullOffset;
    private double _pullProgress;
    private MavRefreshIndicatorState _state = MavRefreshIndicatorState.Idle;
    private IPointer? _trackedPointer;
    private bool _pulling;
    private Point _startPoint;

    public event EventHandler? RefreshRequested;

    static MavRefreshIndicator()
    {
        IsRefreshingProperty.Changed.AddClassHandler<MavRefreshIndicator>((r, _) => r.SyncRefreshingState());
        StatusTextProperty.Changed.AddClassHandler<MavRefreshIndicator>((r, _) => r.UpdateVisualState());
        ArmedTextProperty.Changed.AddClassHandler<MavRefreshIndicator>((r, _) => r.UpdateVisualState());
        RefreshingTextProperty.Changed.AddClassHandler<MavRefreshIndicator>((r, _) => r.UpdateVisualState());
        BodyProperty.Changed.AddClassHandler<MavRefreshIndicator>((r, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(body);
            r._bodyHost.Child = body;
        });
    }

    public MavRefreshIndicator() : this(null)
    {
    }

    public MavRefreshIndicator(Control? content = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _bodyHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brushes.Transparent,
        };

        _pullSpinner = new MavCircularProgress(26, indeterminate: false, value: 0)
        {
            StrokeWidth = 3.0,
        };

        _refreshingLoader = new MavLoadingIndicator(28, contained: false)
        {
            IsVisible = false,
        };

        var iconHost = new Grid
        {
            Width = 28,
            Height = 28,
            VerticalAlignment = VerticalAlignment.Center,
        };
        iconHost.Children.Add(_pullSpinner);
        iconHost.Children.Add(_refreshingLoader);

        _stateLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
            Margin = new Thickness(8, 0, 4, 0),
        };

        var pillRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        pillRow.Children.Add(iconHost);
        pillRow.Children.Add(_stateLabel);

        _pill = new MavSurface
        {
            MinWidth = 48,
            Height = 44,
            Radius = 22,
            Elevation = 2,
            Padding = new Thickness(10, 6),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 6),
            Opacity = 0,
            Content = pillRow,
        };

        _indicatorHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Height = 0,
            IsHitTestVisible = false,
            ZIndex = 2,
            Child = _pill,
        };

        _root = new Grid
        {
            ClipToBounds = true,
            Background = Brushes.Transparent,
        };
        _root.Children.Add(_bodyHost);
        _root.Children.Add(_indicatorHost);

        base.Content = _root;
        if (content is not null)
            Body = content;

        AddHandler(PointerPressedEvent, OnPullPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnPullMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPullReleased, RoutingStrategies.Tunnel, handledEventsToo: true);

        UpdateVisualState();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public new Control? Content
    {
        get => Body;
        set => Body = value;
    }

    public bool IsRefreshing
    {
        get => GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    public double TriggerDistance
    {
        get => GetValue(TriggerDistanceProperty);
        set => SetValue(TriggerDistanceProperty, Math.Max(36, value));
    }

    public double Displacement
    {
        get => GetValue(DisplacementProperty);
        set => SetValue(DisplacementProperty, Math.Max(36, value));
    }

    public double MaxPullDistance
    {
        get => GetValue(MaxPullDistanceProperty);
        set => SetValue(MaxPullDistanceProperty, Math.Max(64, value));
    }

    public string StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public string ArmedText
    {
        get => GetValue(ArmedTextProperty);
        set => SetValue(ArmedTextProperty, value);
    }

    public string RefreshingText
    {
        get => GetValue(RefreshingTextProperty);
        set => SetValue(RefreshingTextProperty, value);
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

    public Func<Task>? RefreshHandler { get; set; }

    public double PullOffset => _pullOffset;
    public double PullProgress => _pullProgress;
    public MavRefreshIndicatorState State => _state;

    public async Task TriggerRefreshAsync()
    {
        if (IsRefreshing) return;
        IsRefreshing = true;
        await ExecuteRefreshWorkAsync();
    }

    private void OnPullPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_trackedPointer is not null || !IsEnabled || IsRefreshing || !e.Pointer.IsPrimary)
            return;

        var point = e.GetCurrentPoint(null);
        if (!point.Properties.IsLeftButtonPressed && e.Pointer.Type == PointerType.Mouse)
            return;

        if (!IsAtTop())
            return;

        if (e.Source is Visual source && source.GetSelfAndVisualAncestors()
            .TakeWhile(v => !ReferenceEquals(v, this))
            .Any(v => v is IMavDraggableControl or RangeBase or TextBox or MavSlider or MavRangeSlider or MavRadialGauge or MavTimeDial or MavColorPicker or MavRatingBar or MavCarousel))
        {
            return;
        }

        MavMotion.Cancel(this, "refresh_pull");
        _startPoint = point.Position;
        _trackedPointer = e.Pointer;
        _pulling = false;
    }

    private void OnPullMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(_trackedPointer, e.Pointer) || !IsEnabled || IsRefreshing)
            return;

        var delta = e.GetPosition(null) - _startPoint;
        if (!_pulling)
        {
            if (e.Pointer.Captured is Visual captured && !ReferenceEquals(captured, this) && captured is not ScrollViewer)
            {
                ResetPull(releaseCapture: false, animate: false);
                return;
            }

            if (delta.Y < -PullStartDistance || (Math.Abs(delta.X) > PullStartDistance && Math.Abs(delta.X) >= Math.Abs(delta.Y)))
            {
                ResetPull(releaseCapture: false, animate: false);
                return;
            }

            if (delta.Y < PullStartDistance)
                return;

            if (!IsAtTop())
            {
                ResetPull(releaseCapture: false, animate: false);
                return;
            }

            _pulling = true;
            e.Pointer.Capture(this);
        }

        if (!ReferenceEquals(e.Pointer.Captured, this))
        {
            ResetPull(releaseCapture: false, animate: true);
            return;
        }

        e.PreventGestureRecognition();
        var resisted = Math.Min(MaxPullDistance, Math.Sqrt(Math.Max(0.0, delta.Y - PullStartDistance)) * 9.5);
        SetPullOffset(resisted);
        e.Handled = true;
    }

    private void OnPullReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!ReferenceEquals(_trackedPointer, e.Pointer))
            return;

        var claimed = _pulling && ReferenceEquals(e.Pointer.Captured, this);
        var armed = claimed && _pullProgress >= 1.0 && !IsRefreshing;
        if (claimed)
        {
            e.PreventGestureRecognition();
            e.Handled = true;
        }

        _trackedPointer = null;
        _pulling = false;
        if (ReferenceEquals(e.Pointer.Captured, this))
            e.Pointer.Capture(null);

        if (armed)
        {
            IsRefreshing = true;
            _ = ExecuteRefreshWorkAsync();
        }
        else if (!IsRefreshing)
        {
            AnimatePullTo(0);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (ReferenceEquals(e.Source, this) &&
            ReferenceEquals(_trackedPointer, e.Pointer) &&
            !ReferenceEquals(e.Pointer.Captured, this))
        {
            ResetPull(releaseCapture: false, animate: true);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ResetPull(releaseCapture: true, animate: false);
        base.OnDetachedFromVisualTree(e);
    }

    private void ResetPull(bool releaseCapture, bool animate)
    {
        var pointer = _trackedPointer;
        _trackedPointer = null;
        _pulling = false;
        if (releaseCapture && pointer is not null && ReferenceEquals(pointer.Captured, this))
            pointer.Capture(null);

        if (!IsRefreshing)
        {
            if (animate)
                AnimatePullTo(0);
            else
                SetPullOffset(0);
        }
    }

    private bool IsAtTop()
    {
        var scroller = _bodyHost.Child as ScrollViewer
            ?? _bodyHost.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        return scroller is null || scroller.Offset.Y <= 0.5;
    }

    private async Task ExecuteRefreshWorkAsync()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);

        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);

        if (RefreshHandler is not null)
        {
            try
            {
                await RefreshHandler();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }

    private void SyncRefreshingState()
    {
        if (IsRefreshing)
        {
            AnimatePullTo(Math.Min(MaxPullDistance, Math.Max(52.0, Displacement)));
        }
        else
        {
            AnimatePullTo(0);
        }
        UpdateVisualState();
    }

    private void AnimatePullTo(double target)
    {
        MavMotion.Animate(this, "refresh_pull", _pullOffset, target, MavMotion.Medium1, MavMotion.CubicOut, SetPullOffset);
    }

    private void SetPullOffset(double value)
    {
        _pullOffset = Math.Clamp(value, 0.0, MaxPullDistance);
        _pullProgress = Math.Clamp(_pullOffset / Math.Max(1.0, TriggerDistance), 0.0, 1.0);
        _indicatorHost.Height = _pullOffset;
        _pill.Opacity = Math.Clamp(_pullProgress * 1.25, 0.0, 1.0);
        _pill.SetScale(0.75 + 0.25 * _pullProgress);
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        _state = IsRefreshing
            ? MavRefreshIndicatorState.Refreshing
            : _pullOffset <= 0.5
                ? MavRefreshIndicatorState.Idle
                : _pullProgress >= 1.0
                    ? MavRefreshIndicatorState.Armed
                    : MavRefreshIndicatorState.Pulling;

        var isRefreshing = _state == MavRefreshIndicatorState.Refreshing;
        _pullSpinner.IsVisible = !isRefreshing;
        _refreshingLoader.IsVisible = isRefreshing;

        if (!isRefreshing)
        {
            _pullSpinner.Value = Math.Clamp(_pullProgress * 0.88, 0.05, 0.92);
            _pullSpinner.SetRotation(_pullProgress * 220.0);
        }

        var text = _state switch
        {
            MavRefreshIndicatorState.Refreshing => RefreshingText,
            MavRefreshIndicatorState.Armed => string.IsNullOrWhiteSpace(ArmedText) ? StatusText : ArmedText,
            MavRefreshIndicatorState.Pulling => StatusText,
            _ => string.Empty,
        };

        _stateLabel.Text = text;
        _stateLabel.IsVisible = !string.IsNullOrWhiteSpace(text);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _pill.FillColor = c.SurfaceContainerHigh;
        _stateLabel.Apply(MavTypeRole.LabelMedium, t.Typography);
        _stateLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
    }
}
