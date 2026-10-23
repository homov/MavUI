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

/// <summary>Action revealed when a <see cref="MavListItem"/> is swiped.</summary>
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

    public Action? TriggeredCallback { get; set; }
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

    /// <summary>Creates an empty swipe action.</summary>
    public MavSwipeAction()
    {
    }

    /// <summary>Creates a swipe action with a label, icon, color and click handler.</summary>
    public MavSwipeAction(
        string label,
        string icon,
        Action? onTriggered = null,
        Color? backgroundColor = null,
        Color? foregroundColor = null)
    {
        Label = label;
        Icon = icon;
        TriggeredCallback = onTriggered;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
    }

    /// <summary>
    /// Measures the action during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Runs the action callback, used by the list when the swipe is confirmed.</summary>
    internal void Invoke()
    {
        TriggeredCallback?.Invoke();
        Triggered?.Invoke(this, EventArgs.Empty);
        Clicked?.Invoke(this, EventArgs.Empty);
        if (Command is { } cmd && cmd.CanExecute(CommandParameter))
            cmd.Execute(CommandParameter);
    }
}
