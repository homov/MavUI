using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One entry of a <see cref="MavTimeline"/>.</summary>
public sealed class MavTimelineItem : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavTimelineItem, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MavTimelineItem, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TimestampProperty =
        AvaloniaProperty.Register<MavTimelineItem, string?>(nameof(Timestamp));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavTimelineItem, string?>(nameof(Icon));

    public static readonly StyledProperty<MavTimelineStatus> StatusProperty =
        AvaloniaProperty.Register<MavTimelineItem, MavTimelineStatus>(nameof(Status), MavTimelineStatus.Completed);

    public static readonly StyledProperty<Control?> CustomContentProperty =
        AvaloniaProperty.Register<MavTimelineItem, Control?>(nameof(CustomContent));

    internal event EventHandler? ItemChanged;

    static MavTimelineItem()
    {
        TitleProperty.Changed.AddClassHandler<MavTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        DescriptionProperty.Changed.AddClassHandler<MavTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        TimestampProperty.Changed.AddClassHandler<MavTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        StatusProperty.Changed.AddClassHandler<MavTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        CustomContentProperty.Changed.AddClassHandler<MavTimelineItem>((i, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<Control?>());
            i.ItemChanged?.Invoke(i, EventArgs.Empty);
        });
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? Subtitle
    {
        get => Description;
        set => Description = value;
    }

    public bool IsCompleted
    {
        get => Status == MavTimelineStatus.Completed;
        set { if (value) Status = MavTimelineStatus.Completed; }
    }

    public bool IsActive
    {
        get => Status == MavTimelineStatus.Active;
        set { if (value) Status = MavTimelineStatus.Active; }
    }

    public string? Timestamp
    {
        get => GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public MavTimelineStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    [Content]
    public Control? CustomContent
    {
        get => GetValue(CustomContentProperty);
        set => SetValue(CustomContentProperty, value);
    }

    /// <summary>Creates an empty timeline item.</summary>
    public MavTimelineItem()
    {
    }

    /// <summary>Creates a timeline item with a title, description, time and marker.</summary>
    public MavTimelineItem(
        string title,
        string? description = null,
        string? timestamp = null,
        string? icon = null,
        MavTimelineStatus status = MavTimelineStatus.Completed,
        Control? customContent = null)
    {
        Title = title;
        Description = description;
        Timestamp = timestamp;
        Icon = icon;
        Status = status;
        CustomContent = customContent;
    }

    /// <summary>
    /// Measures the item during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
