using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Theme-aware text label bound to a Material Design 3 <see cref="MavTypeRole"/>.
/// </summary>
public class MavLabel : TextBlock
{
    public static readonly StyledProperty<MavTypeRole> RoleProperty =
        AvaloniaProperty.Register<MavLabel, MavTypeRole>(nameof(Role), MavTypeRole.BodyMedium);

    public static readonly StyledProperty<MavColorRole?> ColorRoleProperty =
        AvaloniaProperty.Register<MavLabel, MavColorRole?>(nameof(ColorRole));

    public static readonly StyledProperty<Color?> CustomTextColorProperty =
        AvaloniaProperty.Register<MavLabel, Color?>(nameof(CustomTextColor));

    public static readonly StyledProperty<bool> SecondaryProperty =
        AvaloniaProperty.Register<MavLabel, bool>(nameof(Secondary));

    static MavLabel()
    {
        RoleProperty.Changed.AddClassHandler<MavLabel>((lbl, _) => lbl.ApplyTheme(MavTheme.Current));
        ColorRoleProperty.Changed.AddClassHandler<MavLabel>((lbl, _) => lbl.ApplyTheme(MavTheme.Current));
        CustomTextColorProperty.Changed.AddClassHandler<MavLabel>((lbl, _) => lbl.ApplyTheme(MavTheme.Current));
        SecondaryProperty.Changed.AddClassHandler<MavLabel>((lbl, _) => lbl.ApplyTheme(MavTheme.Current));
    }

    /// <summary>Creates an empty label with the body-medium text role.</summary>
    public MavLabel() : this(null, MavTypeRole.BodyMedium, null)
    {
    }

    /// <summary>Creates a label with the given text, type role and optional color role.</summary>
    public MavLabel(string? text = null, MavTypeRole role = MavTypeRole.BodyMedium, MavColorRole? colorRole = null)
    {
        if (text is not null) Text = text;
        Role = role;
        if (colorRole is not null) ColorRole = colorRole;
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MavTypeRole Role
    {
        get => GetValue(RoleProperty);
        set => SetValue(RoleProperty, value);
    }

    public MavColorRole? ColorRole
    {
        get => GetValue(ColorRoleProperty);
        set => SetValue(ColorRoleProperty, value);
    }

    public Color TextColor
    {
        get => (Foreground as ISolidColorBrush)?.Color ?? Colors.Transparent;
        set => Foreground = MavTheme.BrushOf(value);
    }

    public Color? CustomTextColor
    {
        get => GetValue(CustomTextColorProperty);
        set => SetValue(CustomTextColorProperty, value);
    }

    public bool Secondary
    {
        get => GetValue(SecondaryProperty);
        set => SetValue(SecondaryProperty, value);
    }

    public TextAlignment HorizontalTextAlignment
    {
        get => TextAlignment;
        set => TextAlignment = value;
    }

    private void ApplyTheme(MavTheme theme)
    {
        this.Apply(Role, theme.Typography);
        var color = CustomTextColor
            ?? (ColorRole is { } role ? theme.Color.Get(role) : (Secondary ? theme.Color.OnSurfaceVariant : theme.Color.OnSurface));
        Foreground = MavTheme.BrushOf(color);
    }
}
