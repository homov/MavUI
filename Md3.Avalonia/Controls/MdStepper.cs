using System.Collections;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdStepState
{
    Indexed,
    Active,
    Completed,
    Error,
}

public sealed class MdStep : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdStep, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MdStep, string?>(nameof(Subtitle));

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MdStep, Control?>(nameof(Content));

    public static readonly StyledProperty<MdStepState?> StateOverrideProperty =
        AvaloniaProperty.Register<MdStep, MdStepState?>(nameof(StateOverride));

    public static readonly StyledProperty<bool> IsOptionalProperty =
        AvaloniaProperty.Register<MdStep, bool>(nameof(IsOptional));

    internal event EventHandler? StepItemChanged;

    static MdStep()
    {
        TitleProperty.Changed.AddClassHandler<MdStep>((s, _) => s.StepItemChanged?.Invoke(s, EventArgs.Empty));
        SubtitleProperty.Changed.AddClassHandler<MdStep>((s, _) => s.StepItemChanged?.Invoke(s, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MdStep>((s, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(s, e.GetNewValue<Control?>());
            s.StepItemChanged?.Invoke(s, EventArgs.Empty);
        });
        StateOverrideProperty.Changed.AddClassHandler<MdStep>((s, _) => s.StepItemChanged?.Invoke(s, EventArgs.Empty));
    }

    public MdStep()
    {
    }

    public MdStep(
        string title,
        string? subtitle = null,
        Control? content = null,
        MdStepState? stateOverride = null)
    {
        Title = title;
        Subtitle = subtitle;
        Content = content;
        StateOverride = stateOverride;
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool IsOptional
    {
        get => GetValue(IsOptionalProperty);
        set => SetValue(IsOptionalProperty, value);
    }

    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public MdStepState? StateOverride
    {
        get => GetValue(StateOverrideProperty);
        set => SetValue(StateOverrideProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 Horizontal and Vertical Stepper with numbered/checked/error step badges,
/// connector lines, step body host, and optional Back/Next navigation controls.
/// </summary>
public class MdStepper : MdContentView, IEnumerable<MdStep>
{
    public static readonly StyledProperty<int> CurrentStepProperty =
        AvaloniaProperty.Register<MdStepper, int>(
            nameof(CurrentStep),
            0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MdStepper, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MdStepper, Orientation>(
            nameof(Orientation),
            Orientation.Horizontal);

    public static readonly StyledProperty<bool> ShowControlsProperty =
        AvaloniaProperty.Register<MdStepper, bool>(
            nameof(ShowControls),
            true);

    public static readonly StyledProperty<bool> AllowStepTapProperty =
        AvaloniaProperty.Register<MdStepper, bool>(
            nameof(AllowStepTap),
            true);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdStepper, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MdStepper, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<ICommand?> CompletedCommandProperty =
        AvaloniaProperty.Register<MdStepper, ICommand?>(nameof(CompletedCommand));

    public static readonly StyledProperty<object?> CompletedCommandParameterProperty =
        AvaloniaProperty.Register<MdStepper, object?>(nameof(CompletedCommandParameter));

    private readonly AvaloniaList<MdStep> _steps = new();
    private readonly List<object> _rawItems = [];
    private readonly MdItemEventWiring<MdStep, EventHandler> _wiredSteps = new(
        static (step, handler) => step.StepItemChanged += handler,
        static (step, handler) => step.StepItemChanged -= handler);
    private readonly MdCollectionObserver _observer;
    private readonly StackPanel _root;
    private readonly Panel _stepsHost;
    private readonly Border _bodyHost;
    private readonly StackPanel _controlsRow;
    private readonly MdButton _backBtn;
    private readonly MdButton _nextBtn;
    private bool _syncingSelection;

    public event EventHandler<int>? StepChanged;
    public event EventHandler? Completed;

    static MdStepper()
    {
        CurrentStepProperty.Changed.AddClassHandler<MdStepper>((s, e) =>
            s.OnCurrentStepChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MdStepper>((s, e) =>
            s.OnSelectedItemChanged(e.GetNewValue<object?>()));
        OrientationProperty.Changed.AddClassHandler<MdStepper>((s, _) =>
            s.Rebuild());
        ShowControlsProperty.Changed.AddClassHandler<MdStepper>((s, _) =>
            s.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MdStepper>((s, e) =>
            s.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MdStepper>((s, _) =>
            s.RebuildFromItemsSource(s.ItemsSource));
    }

    public MdStepper() : this(Orientation.Horizontal, true)
    {
    }

    public MdStepper(Orientation orientation = Orientation.Horizontal, bool showControls = true)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _steps.CollectionChanged += (_, e) =>
        {
            _wiredSteps.HandleCollectionChanged(e);
            foreach (var st in _steps)
            {
                MdVisualTreeHelpers.AttachLogicalChild(this, st);
                _wiredSteps.Add(st, (_, _) => Rebuild());
            }
            Rebuild();
        };

        _stepsHost = new Grid();
        _bodyHost = new Border
        {
            Margin = new Thickness(0, 12, 0, 8),
            IsVisible = false,
        };

        _backBtn = new MdButton(MdLocalization.Back, MdButtonVariant.Text);
        _backBtn.Clicked += (_, _) => Previous();

        _nextBtn = new MdButton("Continue", MdButtonVariant.Filled);
        _nextBtn.Clicked += (_, _) => Next();

        _controlsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };
        _controlsRow.Children.Add(_nextBtn);
        _controlsRow.Children.Add(_backBtn);

        _root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
        };
        _root.Children.Add(_stepsHost);
        _root.Children.Add(_bodyHost);
        _root.Children.Add(_controlsRow);

        Content = _root;
        Orientation = orientation;
        ShowControls = showControls;

        MdTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public int CurrentStep
    {
        get => GetValue(CurrentStepProperty);
        set
        {
            var clamped = _steps.Count == 0 ? 0 : Math.Clamp(value, 0, _steps.Count - 1);
            SetValue(CurrentStepProperty, clamped);
        }
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public bool ShowControls
    {
        get => GetValue(ShowControlsProperty);
        set => SetValue(ShowControlsProperty, value);
    }

    public bool AllowStepTap
    {
        get => GetValue(AllowStepTapProperty);
        set => SetValue(AllowStepTapProperty, value);
    }

    [Content]
    public AvaloniaList<MdStep> Steps => _steps;

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
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

    public ICommand? CompletedCommand
    {
        get => GetValue(CompletedCommandProperty);
        set => SetValue(CompletedCommandProperty, value);
    }

    public object? CompletedCommandParameter
    {
        get => GetValue(CompletedCommandParameterProperty);
        set => SetValue(CompletedCommandParameterProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _steps.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MdStep st)
            {
                _steps.Add(st);
            }
            else if (ItemTemplate?.Build(item) is { } built)
            {
                built.DataContext = item;
                if (built is MdStep builtStep)
                    _steps.Add(builtStep);
                else
                    _steps.Add(new MdStep(item.ToString() ?? string.Empty, content: built));
            }
            else
            {
                _steps.Add(new MdStep(item.ToString() ?? string.Empty));
            }
        }
    }

    public MdStepper AddStep(
        string title,
        string? subtitle = null,
        Control? content = null,
        MdStepState? stateOverride = null)
    {
        _steps.Add(new MdStep(title, subtitle, content, stateOverride));
        return this;
    }

    public void Add(MdStep step) => _steps.Add(step);

    public IEnumerator<MdStep> GetEnumerator() => _steps.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Next()
    {
        if (_steps.Count == 0) return;
        if (CurrentStep < _steps.Count - 1)
        {
            CurrentStep++;
        }
        else
        {
            Completed?.Invoke(this, EventArgs.Empty);
            if (CompletedCommand is { } cCmd && cCmd.CanExecute(CompletedCommandParameter))
                cCmd.Execute(CompletedCommandParameter);
        }
    }

    public void Previous()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    private void OnCurrentStepChanged(int step)
    {
        if (!_syncingSelection)
        {
            _syncingSelection = true;
            object? item = null;
            if (step >= 0 && step < _rawItems.Count)
                item = _rawItems[step];
            else if (step >= 0 && step < _steps.Count)
                item = _steps[step];
            SetValue(SelectedItemProperty, item);
            _syncingSelection = false;
        }
        Rebuild();
        StepChanged?.Invoke(this, step);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        _syncingSelection = true;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MdStep st)
            idx = _steps.IndexOf(st);
        if (idx >= 0)
            CurrentStep = idx;
        _syncingSelection = false;
    }

    private MdStepState ResolveState(int index)
    {
        if (_steps[index].StateOverride is { } ov)
            return ov;
        if (index < CurrentStep)
            return MdStepState.Completed;
        if (index == CurrentStep)
            return MdStepState.Active;
        return MdStepState.Indexed;
    }

    private void Rebuild()
    {
        _bodyHost.Child = null;
        foreach (var step in _steps)
            MdVisualTreeHelpers.DetachFromParent(step.Content);
        _stepsHost.Children.Clear();
        var t = MdTheme.Current;
        var c = t.Color;

        if (_steps.Count == 0)
        {
            _bodyHost.IsVisible = false;
            _controlsRow.IsVisible = false;
            return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            var headerGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
            };

            for (int i = 0; i < _steps.Count; i++)
            {
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                if (i < _steps.Count - 1)
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            for (int i = 0; i < _steps.Count; i++)
            {
                var idx = i;
                var node = BuildStepHeader(i, t);
                headerGrid.Add(node, i * 2, 0);

                if (i < _steps.Count - 1)
                {
                    var done = i < CurrentStep;
                    var line = new Border
                    {
                        Height = 2,
                        MinWidth = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0),
                        Background = MdTheme.BrushOf(done ? c.Primary : c.OutlineVariant),
                    };
                    headerGrid.Add(line, i * 2 + 1, 0);
                }
            }

            _stepsHost.Children.Add(headerGrid);

            var activeContent = CurrentStep >= 0 && CurrentStep < _steps.Count
                ? _steps[CurrentStep].Content
                : null;
            MdVisualTreeHelpers.DetachFromParent(activeContent);
            _bodyHost.Child = activeContent;
            _bodyHost.IsVisible = activeContent is not null;
            if (activeContent is not null)
                MdMotion.FadeIn(activeContent, MdMotion.Short3);

            _controlsRow.IsVisible = ShowControls;
        }
        else
        {
            _bodyHost.Child = null;
            _bodyHost.IsVisible = false;
            _controlsRow.IsVisible = false;

            var vertStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
            };

            for (int i = 0; i < _steps.Count; i++)
            {
                var isCurrent = i == CurrentStep;
                var header = BuildStepHeader(i, t);
                vertStack.Children.Add(header);

                if (isCurrent || i < _steps.Count - 1)
                {
                    var bodyGrid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("28,*"),
                        Margin = new Thickness(6, 0, 0, 4),
                    };

                    if (i < _steps.Count - 1)
                    {
                        var vLine = new Border
                        {
                            Width = 2,
                            MinHeight = isCurrent ? 36 : 18,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Background = MdTheme.BrushOf(i < CurrentStep ? c.Primary : c.OutlineVariant),
                        };
                        bodyGrid.Add(vLine, 0, 0);
                    }

                    if (isCurrent)
                    {
                        var stepCol = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 10,
                            Margin = new Thickness(12, 4, 0, 12),
                        };
                        if (_steps[i].Content is { } stepContent)
                        {
                            MdVisualTreeHelpers.DetachFromParent(stepContent);
                            stepCol.Children.Add(stepContent);
                        }

                        if (ShowControls)
                        {
                            var inlineBtns = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 8,
                            };
                            var next = new MdButton(
                                i == _steps.Count - 1 ? "Finish" : "Continue",
                                MdButtonVariant.Filled,
                                size: MdButtonSize.Small);
                            next.Clicked += (_, _) => Next();
                            inlineBtns.Children.Add(next);

                            if (i > 0)
                            {
                                var back = new MdButton(MdLocalization.Back, MdButtonVariant.Text, size: MdButtonSize.Small);
                                back.Clicked += (_, _) => Previous();
                                inlineBtns.Children.Add(back);
                            }
                            stepCol.Children.Add(inlineBtns);
                        }

                        bodyGrid.Add(stepCol, 1, 0);
                    }

                    vertStack.Children.Add(bodyGrid);
                }
            }

            _stepsHost.Children.Add(vertStack);
        }

        _backBtn.IsEnabled = CurrentStep > 0;
        _nextBtn.Text = CurrentStep >= _steps.Count - 1 ? "Finish" : "Continue";
    }

    private Control BuildStepHeader(int index, MdTheme t)
    {
        var c = t.Color;
        var step = _steps[index];
        var state = ResolveState(index);

        Color badgeBg = state switch
        {
            MdStepState.Active or MdStepState.Completed => c.Primary,
            MdStepState.Error => c.Error,
            _ => c.SurfaceContainerHighest,
        };

        Color badgeFg = state switch
        {
            MdStepState.Active or MdStepState.Completed => c.OnPrimary,
            MdStepState.Error => c.OnError,
            _ => c.OnSurfaceVariant,
        };

        Control badgeInner;
        if (state == MdStepState.Completed)
        {
            badgeInner = new MdIcon(MdIcons.Check, 16)
            {
                UseThemeDefaultColor = false,
                Color = badgeFg,
            };
        }
        else if (state == MdStepState.Error)
        {
            badgeInner = new MdIcon(MdIcons.Warning, 16)
            {
                UseThemeDefaultColor = false,
                Color = badgeFg,
            };
        }
        else
        {
            var numLbl = new TextBlock
            {
                Text = (index + 1).ToString(CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            numLbl.Apply(MdTypeRole.LabelMedium, t.Typography);
            numLbl.Foreground = MdTheme.BrushOf(badgeFg);
            badgeInner = numLbl;
        }

        var badge = new MdSurface
        {
            Width = 28,
            Height = 28,
            Radius = 14,
            FillColor = badgeBg,
            VerticalAlignment = VerticalAlignment.Center,
            Content = badgeInner,
        };

        var titleLbl = new TextBlock
        {
            Text = step.Title,
            TextWrapping = TextWrapping.NoWrap,
        };
        titleLbl.Apply(MdTypeRole.TitleSmall, t.Typography);
        titleLbl.Foreground = MdTheme.BrushOf(
            state == MdStepState.Error
                ? c.Error
                : state is MdStepState.Active or MdStepState.Completed
                    ? c.OnSurface
                    : c.OnSurfaceVariant);

        var textCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 1,
            VerticalAlignment = VerticalAlignment.Center,
        };
        textCol.Children.Add(titleLbl);

        if (!string.IsNullOrEmpty(step.Subtitle))
        {
            var subLbl = new TextBlock
            {
                Text = step.Subtitle,
                TextWrapping = TextWrapping.NoWrap,
            };
            subLbl.Apply(MdTypeRole.BodySmall, t.Typography);
            subLbl.Foreground = MdTheme.BrushOf(state == MdStepState.Error ? c.Error : c.OnSurfaceVariant);
            textCol.Children.Add(subLbl);
        }

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(badge);
        row.Children.Add(textCol);

        var hit = new MdInkSurface
        {
            CornerRadius = 10,
            Padding = new Thickness(6, 4),
            IsInteractive = AllowStepTap,
            Content = row,
        };
        var captured = index;
        hit.Clicked += (_, _) =>
        {
            if (AllowStepTap)
                CurrentStep = captured;
        };

        return hit;
    }
}
