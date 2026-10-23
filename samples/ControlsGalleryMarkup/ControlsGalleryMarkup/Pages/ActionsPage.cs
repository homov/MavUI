using Avalonia.Controls;
using Mav.Controls;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

public static class ActionsPage
{
    public static Control Build()
    {
        var clicks = 0;
        var counter = new MavLabel($"Clicks: {clicks}");

        var click = new MavButton("Click me", MavButtonVariant.Elevated, MavIcons.TouchApp)
            .OnClicked(() => counter.Text = $"Clicks: {++clicks}");

        return new StackPanel()
            .Spacing(18)
            .Children(
                Section(
                    "Buttons",
                    "MavButton supports five variants, five sizes, icons, toggle and loading states.",
                    Row(
                        new MavButton("Filled", MavButtonVariant.Filled),
                        new MavButton("Tonal", MavButtonVariant.Tonal),
                        new MavButton("Elevated", MavButtonVariant.Elevated),
                        new MavButton("Outlined", MavButtonVariant.Outlined),
                        new MavButton("Text", MavButtonVariant.Text)),
                    Row(
                        new MavButton("XS", MavButtonVariant.Filled).Size(MavButtonSize.ExtraSmall),
                        new MavButton("S", MavButtonVariant.Filled).Size(MavButtonSize.Small),
                        new MavButton("M", MavButtonVariant.Filled).Size(MavButtonSize.Medium),
                        new MavButton("L", MavButtonVariant.Filled).Size(MavButtonSize.Large),
                        new MavButton("XL", MavButtonVariant.Filled).Size(MavButtonSize.ExtraLarge)),
                    Row(
                        click,
                        counter,
                        new MavButton("Toggle", MavButtonVariant.Tonal, MavIcons.Star).IsToggle(true),
                        new MavButton("Loading", MavButtonVariant.Filled, MavIcons.Download).IsLoading(true),
                        new MavButton("Disabled", MavButtonVariant.Outlined).IsEnabled(false))),
                Section(
                    "Icon buttons and FABs",
                    "Icon-only actions keep a 48 dp touch target; FABs have three sizes and four color schemes.",
                    Row(
                        new MavIconButton(MavIcons.Search),
                        new MavIconButton(MavIcons.Add, MavIconButtonVariant.Filled),
                        new MavIconButton(MavIcons.Edit, MavIconButtonVariant.Tonal),
                        new MavIconButton(MavIcons.Delete, MavIconButtonVariant.Outlined),
                        new MavIconButton(MavIcons.Favorite, MavIconButtonVariant.Standard, isToggle: true)),
                    Row(
                        new MavFab(MavIcons.Add, MavFabSize.Small, MavFabColor.Primary),
                        new MavFab(MavIcons.Add, MavFabSize.Regular, MavFabColor.Secondary),
                        new MavFab(MavIcons.Add, MavFabSize.Large, MavFabColor.Tertiary),
                        new MavFab(MavIcons.Edit, MavFabSize.Regular, MavFabColor.Surface)),
                    new MavExtendedFab("Compose", MavIcons.Edit).HorizontalAlignment(Avalonia.Layout.HorizontalAlignment.Left)));
    }
}
