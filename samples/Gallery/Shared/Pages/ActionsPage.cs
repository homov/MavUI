using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;

namespace Mav.Gallery;

internal static class ActionsPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section(
                "Buttons",
                "MavButton supports five variants, five sizes, icons, toggle and loading states.",
                ButtonsShowcase()),
            GalleryUi.Section(
                "Icon buttons and FABs",
                "Icon-only actions keep a 48 dp touch target; FABs have three sizes and four color schemes.",
                IconButtons(),
                Fabs()),
            GalleryUi.Section(
                "Action groups",
                "Connected groups, split buttons and an expanding FAB menu.",
                Groups()));
    }

    private static Control ButtonsShowcase()
    {
        var variants = GalleryUi.Row(
            new MavButton("Filled", MavButtonVariant.Filled),
            new MavButton("Tonal", MavButtonVariant.Tonal),
            new MavButton("Elevated", MavButtonVariant.Elevated),
            new MavButton("Outlined", MavButtonVariant.Outlined),
            new MavButton("Text", MavButtonVariant.Text));

        var sizes = GalleryUi.Row(
            new MavButton("XS", MavButtonVariant.Filled, size: MavButtonSize.ExtraSmall),
            new MavButton("S", MavButtonVariant.Filled, size: MavButtonSize.Small),
            new MavButton("M", MavButtonVariant.Filled, size: MavButtonSize.Medium),
            new MavButton("L", MavButtonVariant.Filled, size: MavButtonSize.Large),
            new MavButton("XL", MavButtonVariant.Filled, size: MavButtonSize.ExtraLarge));

        var toggle = new MavButton("Toggle", MavButtonVariant.Tonal, MavIcons.Star) { IsToggle = true };
        var loading = new MavButton("Loading", MavButtonVariant.Filled, MavIcons.Download);
        var clicks = 0;
        var counter = new MavLabel { Text = "Clicks: 0", Role = Mav.Theming.MavTypeRole.BodySmall };

        var click = new MavButton("Click me", MavButtonVariant.Elevated, MavIcons.TouchApp);
        click.Clicked += (_, _) =>
        {
            clicks++;
            counter.Text = $"Clicks: {clicks}";
        };

        var states = GalleryUi.Row(
            click,
            counter,
            toggle,
            loading,
            new MavButton("Disabled", MavButtonVariant.Outlined) { IsEnabled = false });

        loading.Clicked += async (_, _) =>
        {
            loading.IsLoading = true;
            await Task.Delay(1200);
            loading.IsLoading = false;
        };

        return GalleryUi.Showcase(
            "MavButton",
            "A text button with optional leading/trailing icons, toggle/selected and loading states.",
            GalleryUi.Column(variants, sizes, states),
            "new MavButton(text, variant, icon, size) · Clicked · IsToggle · IsSelected · IsLoading");
    }

    private static Control IconButtons()
    {
        var toggle = new MavIconButton(MavIcons.Favorite, isToggle: true)
        {
            SelectedIcon = MavIcons.Favorite,
        };

        return GalleryUi.Showcase(
            "MavIconButton",
            "Standard, filled, tonal and outlined icon buttons. The touch target stays 48 dp even when the visual is smaller.",
            GalleryUi.Row(
                new MavIconButton(MavIcons.Search),
                new MavIconButton(MavIcons.Add, MavIconButtonVariant.Filled),
                new MavIconButton(MavIcons.Edit, MavIconButtonVariant.Tonal),
                new MavIconButton(MavIcons.Delete, MavIconButtonVariant.Outlined),
                toggle),
            "new MavIconButton(icon, variant, isToggle) · SelectedIcon · TouchTargetSize");
    }

    private static Control Fabs()
    {
        var extended = new MavExtendedFab("Compose", MavIcons.Edit);

        return GalleryUi.Showcase(
            "MavFab and MavExtendedFab",
            "Floating action buttons in small, regular and large sizes, with primary, secondary, tertiary or surface color.",
            GalleryUi.Column(
                GalleryUi.Row(
                    new MavFab(MavIcons.Add, MavFabSize.Small, MavFabColor.Primary),
                    new MavFab(MavIcons.Add, MavFabSize.Regular, MavFabColor.Secondary),
                    new MavFab(MavIcons.Add, MavFabSize.Large, MavFabColor.Tertiary),
                    new MavFab(MavIcons.Edit, MavFabSize.Regular, MavFabColor.Surface)),
                extended),
            "new MavFab(icon, size, color) · MavExtendedFab(text, icon, color)");
    }

    private static Control Groups()
    {
        var standard = new MavButtonGroup { Kind = MavButtonGroupKind.Standard };
        standard.AddButton(new MavButton("Day", MavButtonVariant.Outlined));
        standard.AddButton(new MavButton("Week", MavButtonVariant.Outlined));
        standard.AddButton(new MavButton("Month", MavButtonVariant.Outlined));

        var connected = new MavButtonGroup { Kind = MavButtonGroupKind.Connected };
        connected.AddButton(new MavButton("Bold", MavButtonVariant.Tonal, MavIcons.FormatBold));
        connected.AddButton(new MavButton("Italic", MavButtonVariant.Tonal, MavIcons.FormatItalic));
        connected.AddButton(new MavButton("Underline", MavButtonVariant.Tonal, MavIcons.FormatUnderlined));

        var split = new MavSplitButton
        {
            Text = "Save",
            Menu = new MavMenu()
                .AddItem("Save as draft", MavIcons.Description, onSelected: () => { })
                .AddItem("Save and close", MavIcons.Close, onSelected: () => { }),
        };

        var fabMenu = new MavFabMenu();
        fabMenu.AddItem("New document", MavIcons.NoteAdd, () => { });
        fabMenu.AddItem("Upload", MavIcons.Upload, () => { });
        fabMenu.AddItem("Scan", MavIcons.DocumentScanner, () => { });

        return GalleryUi.Showcase(
            "MavButtonGroup, MavSplitButton and MavFabMenu",
            "Group related actions, pair a primary action with a menu, or expand a FAB into labeled actions.",
            GalleryUi.Column(
                GalleryUi.Row(standard, connected),
                GalleryUi.Row(split, fabMenu)),
            "MavButtonGroup.Kind · MavSplitButton.Menu · MavFabMenu.AddItem(label, icon, action)");
    }
}
