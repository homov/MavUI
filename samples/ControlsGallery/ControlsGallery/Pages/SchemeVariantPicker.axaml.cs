using Avalonia.Controls;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

/// <summary>
/// Palette (scheme variant) buttons. The current variant is shown as a filled button; the list is rebuilt
/// on every theme change, so the highlight follows the theme.
/// </summary>
public partial class SchemeVariantPicker : UserControl
{
    public static readonly GalleryCommand Select =
        new(p => MavTheme.Configure(schemeVariant: (MavSchemeVariant)p!));

    private static readonly (MavSchemeVariant Variant, string English)[] Names =
    [
        (MavSchemeVariant.TonalSpot, "Tonal spot"),
        (MavSchemeVariant.Neutral, "Neutral"),
        (MavSchemeVariant.Vibrant, "Vibrant"),
        (MavSchemeVariant.Expressive, "Expressive"),
        (MavSchemeVariant.Monochrome, "Monochrome"),
        (MavSchemeVariant.Fidelity, "Fidelity"),
        (MavSchemeVariant.Content, "Content"),
        (MavSchemeVariant.Rainbow, "Rainbow"),
        (MavSchemeVariant.FruitSalad, "Fruit salad"),
    ];

    public SchemeVariantPicker()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            MavTheme.Changed -= OnThemeChanged;
            MavTheme.Changed += OnThemeChanged;
            Refresh();
        };
        DetachedFromVisualTree += (_, _) => MavTheme.Changed -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, MavTheme theme) => Refresh();

    private void Refresh()
    {
        var current = MavTheme.Current.SchemeVariant;
        Choices.ItemsSource = Names.Select(n => new SchemeVariantChoice(
            n.Variant,
            MavLocalization.Get($"Theming.Variant.{n.Variant}", n.English),
            n.Variant == current ? MavButtonVariant.Filled : MavButtonVariant.Outlined)).ToList();
    }
}
