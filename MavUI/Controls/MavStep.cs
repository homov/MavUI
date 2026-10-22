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
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One step of a <see cref="MavStepper"/>.</summary>
public sealed class MavStep : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavStep, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavStep, string?>(nameof(Subtitle));

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MavStep, Control?>(nameof(Content));

    public static readonly StyledProperty<MavStepState?> StateOverrideProperty =
        AvaloniaProperty.Register<MavStep, MavStepState?>(nameof(StateOverride));

    public static readonly StyledProperty<bool> IsOptionalProperty =
        AvaloniaProperty.Register<MavStep, bool>(nameof(IsOptional));

    internal event EventHandler? StepItemChanged;

    static MavStep()
    {
        TitleProperty.Changed.AddClassHandler<MavStep>((s, _) => s.StepItemChanged?.Invoke(s, EventArgs.Empty));
        SubtitleProperty.Changed.AddClassHandler<MavStep>((s, _) => s.StepItemChanged?.Invoke(s, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MavStep>((s, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(s, e.GetNewValue<Control?>());
            s.StepItemChanged?.Invoke(s, EventArgs.Empty);
        });
        StateOverrideProperty.Changed.AddClassHandler<MavStep>((s, _) => s.StepItemChanged?.Invoke(s, EventArgs.Empty));
    }

    /// <summary>Creates an empty step.</summary>
    public MavStep()
    {
    }

    /// <summary>Creates a step with a title, optional description and content.</summary>
    public MavStep(
        string title,
        string? subtitle = null,
        Control? content = null,
        MavStepState? stateOverride = null)
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

    public MavStepState? StateOverride
    {
        get => GetValue(StateOverrideProperty);
        set => SetValue(StateOverrideProperty, value);
    }

    /// <summary>
    /// Measures the step during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
