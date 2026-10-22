using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One segment of a <see cref="MavSegmentedButton"/>.</summary>
public sealed class MavSegment : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavSegment, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavSegment, string?>(nameof(Icon));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavSegment, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    internal event EventHandler? SegmentChanged;

    static MavSegment()
    {
        TextProperty.Changed.AddClassHandler<MavSegment>((s, _) => s.SegmentChanged?.Invoke(s, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavSegment>((s, _) => s.SegmentChanged?.Invoke(s, EventArgs.Empty));
        IsSelectedProperty.Changed.AddClassHandler<MavSegment>((s, _) => s.SegmentChanged?.Invoke(s, EventArgs.Empty));
    }

    /// <summary>Creates an empty segment.</summary>
    public MavSegment()
    {
    }

    /// <summary>Creates a segment with the given text, optional icon and initial selection state.</summary>
    public MavSegment(string text, string? icon = null, bool selected = false)
    {
        Text = text;
        Icon = icon;
        IsSelected = selected;
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Label
    {
        get => Text;
        set => Text = value;
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Measures the segment during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
