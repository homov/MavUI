using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;

namespace Md3.Gallery;

internal static class ActionsPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section(
                "Buttons",
                "MdButton supports five variants, five sizes, icons, toggle and loading states.",
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
            new MdButton("Filled", MdButtonVariant.Filled),
            new MdButton("Tonal", MdButtonVariant.Tonal),
            new MdButton("Elevated", MdButtonVariant.Elevated),
            new MdButton("Outlined", MdButtonVariant.Outlined),
            new MdButton("Text", MdButtonVariant.Text));

        var sizes = GalleryUi.Row(
            new MdButton("XS", MdButtonVariant.Filled, size: MdButtonSize.ExtraSmall),
            new MdButton("S", MdButtonVariant.Filled, size: MdButtonSize.Small),
            new MdButton("M", MdButtonVariant.Filled, size: MdButtonSize.Medium),
            new MdButton("L", MdButtonVariant.Filled, size: MdButtonSize.Large),
            new MdButton("XL", MdButtonVariant.Filled, size: MdButtonSize.ExtraLarge));

        var toggle = new MdButton("Toggle", MdButtonVariant.Tonal, MdIcons.Star) { IsToggle = true };
        var loading = new MdButton("Loading", MdButtonVariant.Filled, MdIcons.Download);
        var clicks = 0;
        var counter = new MdLabel { Text = "Clicks: 0", Role = Md3.Avalonia.Theming.MdTypeRole.BodySmall };

        var click = new MdButton("Click me", MdButtonVariant.Elevated, MdIcons.TouchApp);
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
            new MdButton("Disabled", MdButtonVariant.Outlined) { IsEnabled = false });

        loading.Clicked += async (_, _) =>
        {
            loading.IsLoading = true;
            await Task.Delay(1200);
            loading.IsLoading = false;
        };

        return GalleryUi.Showcase(
            "MdButton",
            "A text button with optional leading/trailing icons, toggle/selected and loading states.",
            GalleryUi.Column(variants, sizes, states),
            "new MdButton(text, variant, icon, size) · Clicked · IsToggle · IsSelected · IsLoading");
    }

    private static Control IconButtons()
    {
        var toggle = new MdIconButton(MdIcons.Favorite, isToggle: true)
        {
            SelectedIcon = MdIcons.Favorite,
        };

        return GalleryUi.Showcase(
            "MdIconButton",
            "Standard, filled, tonal and outlined icon buttons. The touch target stays 48 dp even when the visual is smaller.",
            GalleryUi.Row(
                new MdIconButton(MdIcons.Search),
                new MdIconButton(MdIcons.Add, MdIconButtonVariant.Filled),
                new MdIconButton(MdIcons.Edit, MdIconButtonVariant.Tonal),
                new MdIconButton(MdIcons.Delete, MdIconButtonVariant.Outlined),
                toggle),
            "new MdIconButton(icon, variant, isToggle) · SelectedIcon · TouchTargetSize");
    }

    private static Control Fabs()
    {
        var extended = new MdExtendedFab("Compose", MdIcons.Edit);

        return GalleryUi.Showcase(
            "MdFab and MdExtendedFab",
            "Floating action buttons in small, regular and large sizes, with primary, secondary, tertiary or surface color.",
            GalleryUi.Column(
                GalleryUi.Row(
                    new MdFab(MdIcons.Add, MdFabSize.Small, MdFabColor.Primary),
                    new MdFab(MdIcons.Add, MdFabSize.Regular, MdFabColor.Secondary),
                    new MdFab(MdIcons.Add, MdFabSize.Large, MdFabColor.Tertiary),
                    new MdFab(MdIcons.Edit, MdFabSize.Regular, MdFabColor.Surface)),
                extended),
            "new MdFab(icon, size, color) · MdExtendedFab(text, icon, color)");
    }

    private static Control Groups()
    {
        var standard = new MdButtonGroup { Kind = MdButtonGroupKind.Standard };
        standard.AddButton(new MdButton("Day", MdButtonVariant.Outlined));
        standard.AddButton(new MdButton("Week", MdButtonVariant.Outlined));
        standard.AddButton(new MdButton("Month", MdButtonVariant.Outlined));

        var connected = new MdButtonGroup { Kind = MdButtonGroupKind.Connected };
        connected.AddButton(new MdButton("Bold", MdButtonVariant.Tonal, MdIcons.FormatBold));
        connected.AddButton(new MdButton("Italic", MdButtonVariant.Tonal, MdIcons.FormatItalic));
        connected.AddButton(new MdButton("Underline", MdButtonVariant.Tonal, MdIcons.FormatUnderlined));

        var split = new MdSplitButton
        {
            Text = "Save",
            Menu = new MdMenu()
                .AddItem("Save as draft", MdIcons.Description, onSelected: () => { })
                .AddItem("Save and close", MdIcons.Close, onSelected: () => { }),
        };

        var fabMenu = new MdFabMenu();
        fabMenu.AddItem("New document", MdIcons.NoteAdd, () => { });
        fabMenu.AddItem("Upload", MdIcons.Upload, () => { });
        fabMenu.AddItem("Scan", MdIcons.DocumentScanner, () => { });

        return GalleryUi.Showcase(
            "MdButtonGroup, MdSplitButton and MdFabMenu",
            "Group related actions, pair a primary action with a menu, or expand a FAB into labeled actions.",
            GalleryUi.Column(
                GalleryUi.Row(standard, connected),
                GalleryUi.Row(split, fabMenu)),
            "MdButtonGroup.Kind · MdSplitButton.Menu · MdFabMenu.AddItem(label, icon, action)");
    }
}
