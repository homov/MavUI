using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Wraps any target control and anchors an <see cref="MavBadge"/> to its top-right corner.
/// </summary>
public class MavBadgedBox : Grid
{
    public static readonly StyledProperty<Control?> TargetProperty =
        AvaloniaProperty.Register<MavBadgedBox, Control?>(nameof(Target));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavBadgedBox, string?>(nameof(BadgeText));

    public static readonly StyledProperty<int?> BadgeCountProperty =
        AvaloniaProperty.Register<MavBadgedBox, int?>(nameof(BadgeCount));

    public static readonly StyledProperty<bool> IsBadgeVisibleProperty =
        AvaloniaProperty.Register<MavBadgedBox, bool>(nameof(IsBadgeVisible), true);

    private readonly Border _targetHost;
    private readonly MavBadge _badge;

    static MavBadgedBox()
    {
        TargetProperty.Changed.AddClassHandler<MavBadgedBox>((b, e) =>
        {
            var ctrl = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(ctrl);
            b._targetHost.Child = ctrl;
        });
        BadgeTextProperty.Changed.AddClassHandler<MavBadgedBox>((b, e) => b._badge.Text = e.GetNewValue<string?>());
        BadgeCountProperty.Changed.AddClassHandler<MavBadgedBox>((b, e) => b._badge.Count = e.GetNewValue<int?>());
        IsBadgeVisibleProperty.Changed.AddClassHandler<MavBadgedBox>((b, e) => b._badge.IsVisible = e.GetNewValue<bool>());
    }

    /// <summary>Creates a badged box without a target or badge text.</summary>
    public MavBadgedBox() : this(null, null)
    {
    }

    /// <summary>Creates a box that overlays a badge on <paramref name="target"/>.</summary>
    public MavBadgedBox(Control? target = null, string? badgeText = null)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        ClipToBounds = false;

        _targetHost = new Border();
        _badge = new MavBadge(badgeText)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -2, -4, 0),
        };

        Children.Add(_targetHost);
        Children.Add(_badge);

        if (target is not null) Target = target;
        if (badgeText is not null) BadgeText = badgeText;
    }

    public MavBadge Badge => _badge;

    [Content]
    public Control? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public int? BadgeCount
    {
        get => GetValue(BadgeCountProperty);
        set => SetValue(BadgeCountProperty, value);
    }

    public bool IsBadgeVisible
    {
        get => GetValue(IsBadgeVisibleProperty);
        set => SetValue(IsBadgeVisibleProperty, value);
    }

    public bool ShowBadge
    {
        get => IsBadgeVisible;
        set => IsBadgeVisible = value;
    }

    public bool IsDot
    {
        get => _badge.IsDot;
        set => _badge.IsDot = value;
    }
}
