using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Theme-aware text label bound to a Material Design 3 <see cref="MdTypeRole"/>.
/// </summary>
public class MdLabel : TextBlock
{
    public static readonly StyledProperty<MdTypeRole> RoleProperty =
        AvaloniaProperty.Register<MdLabel, MdTypeRole>(nameof(Role), MdTypeRole.BodyMedium);

    public static readonly StyledProperty<MdColorRole?> ColorRoleProperty =
        AvaloniaProperty.Register<MdLabel, MdColorRole?>(nameof(ColorRole));

    public static readonly StyledProperty<Color?> CustomTextColorProperty =
        AvaloniaProperty.Register<MdLabel, Color?>(nameof(CustomTextColor));

    public static readonly StyledProperty<bool> SecondaryProperty =
        AvaloniaProperty.Register<MdLabel, bool>(nameof(Secondary));

    static MdLabel()
    {
        RoleProperty.Changed.AddClassHandler<MdLabel>((lbl, _) => lbl.ApplyTheme(MdTheme.Current));
        ColorRoleProperty.Changed.AddClassHandler<MdLabel>((lbl, _) => lbl.ApplyTheme(MdTheme.Current));
        CustomTextColorProperty.Changed.AddClassHandler<MdLabel>((lbl, _) => lbl.ApplyTheme(MdTheme.Current));
        SecondaryProperty.Changed.AddClassHandler<MdLabel>((lbl, _) => lbl.ApplyTheme(MdTheme.Current));
    }

    public MdLabel() : this(null, MdTypeRole.BodyMedium, null)
    {
    }

    public MdLabel(string? text = null, MdTypeRole role = MdTypeRole.BodyMedium, MdColorRole? colorRole = null)
    {
        if (text is not null) Text = text;
        Role = role;
        if (colorRole is not null) ColorRole = colorRole;
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MdTypeRole Role
    {
        get => GetValue(RoleProperty);
        set => SetValue(RoleProperty, value);
    }

    public MdColorRole? ColorRole
    {
        get => GetValue(ColorRoleProperty);
        set => SetValue(ColorRoleProperty, value);
    }

    public Color TextColor
    {
        get => (Foreground as ISolidColorBrush)?.Color ?? Colors.Transparent;
        set => Foreground = MdTheme.BrushOf(value);
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

    private void ApplyTheme(MdTheme theme)
    {
        this.Apply(Role, theme.Typography);
        var color = CustomTextColor
            ?? (ColorRole is { } role ? theme.Color.Get(role) : (Secondary ? theme.Color.OnSurfaceVariant : theme.Color.OnSurface));
        Foreground = MdTheme.BrushOf(color);
    }
}
