using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Buttons, icon buttons, FABs and action groups. Mirrors the AXAML Actions page.</summary>
public static class ActionsPage
{
    public static Control Build()
    {
        var clicks = 0;
        var counter = new MavLabel(L("Actions.Click.Counter", "Clicks: 0"), MavTypeRole.BodySmall);
        var click = new MavButton(L("Actions.Click.Button", "Click me"), MavButtonVariant.Elevated, MavIcons.TouchApp)
            .OnClicked(() =>
            {
                clicks++;
                counter.Text = MavLocalization.Format("Actions.Click.Counter.Value", "Clicks: {0}", clicks);
            });

        var loading = new MavButton(L("Actions.Loading", "Loading"), MavButtonVariant.Filled, MavIcons.Download);
        loading.Clicked += async (_, _) =>
        {
            loading.IsLoading = true;
            await Task.Delay(1200);
            loading.IsLoading = false;
        };

        return Page(
            Section(
                "Actions.Buttons.Section", "Buttons",
                "Actions.Buttons.Section.Body", "MavButton supports five variants, five sizes, icons, toggle and loading states.",
                Card(
                    "MavButton",
                    "Actions.Buttons.Card.Body", "A text button with optional leading/trailing icons, toggle/selected and loading states.",
                    "new MavButton(text, variant, icon, size) · Clicked · IsToggle · IsSelected · IsLoading",
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
                        new MavButton(L("Actions.Toggle", "Toggle"), MavButtonVariant.Tonal, MavIcons.Star).IsToggle(true),
                        loading,
                        Disabled(new MavButton(L("Actions.Disabled", "Disabled"), MavButtonVariant.Outlined))))),
            Section(
                "Actions.Icon.Section", "Icon buttons and FABs",
                "Actions.Icon.Section.Body", "Icon-only actions keep a 48 dp touch target; FABs have three sizes and four color schemes.",
                Card(
                    "MavIconButton",
                    "Actions.Icon.Card.Body", "Standard, filled, tonal and outlined icon buttons. The touch target stays 48 dp even when the visual is smaller.",
                    "new MavIconButton(icon, variant, isToggle) · SelectedIcon · TouchTargetSize",
                    Row(
                        new MavIconButton(MavIcons.Search),
                        new MavIconButton(MavIcons.Add, MavIconButtonVariant.Filled),
                        new MavIconButton(MavIcons.Edit, MavIconButtonVariant.Tonal),
                        new MavIconButton(MavIcons.Delete, MavIconButtonVariant.Outlined),
                        new MavIconButton(MavIcons.Favorite, MavIconButtonVariant.Standard, isToggle: true) { SelectedIcon = MavIcons.Favorite })),
                Card(
                    "MavFab and MavExtendedFab",
                    "Actions.Fab.Card.Body", "Floating action buttons in small, regular and large sizes, with primary, secondary, tertiary or surface color.",
                    "new MavFab(icon, size, color) · MavExtendedFab(text, icon, color)",
                    Row(
                        new MavFab(MavIcons.Add, MavFabSize.Small, MavFabColor.Primary),
                        new MavFab(MavIcons.Add, MavFabSize.Regular, MavFabColor.Secondary),
                        new MavFab(MavIcons.Add, MavFabSize.Large, MavFabColor.Tertiary),
                        new MavFab(MavIcons.Edit, MavFabSize.Regular, MavFabColor.Surface)),
                    new MavExtendedFab("Compose", MavIcons.Edit) { HorizontalAlignment = HorizontalAlignment.Left })),
            Section(
                "Actions.Groups.Section", "Action groups",
                "Actions.Groups.Section.Body", "Connected groups, split buttons and an expanding FAB menu.",
                Card(
                    "MavButtonGroup, MavSplitButton and MavFabMenu",
                    "Actions.Groups.Card.Body", "Group related actions, pair a primary action with a menu, or expand a FAB into labeled actions.",
                    "MavButtonGroup.Kind · MavSplitButton.Menu · MavFabMenu.AddItem(label, icon, action)",
                    Row(StandardGroup(), ConnectedGroup()),
                    Row(SplitButton(), FabMenu()))));
    }

    private static MavButtonGroup StandardGroup()
    {
        var group = new MavButtonGroup(MavButtonGroupKind.Standard);
        group.AddButton(new MavButton("Day", MavButtonVariant.Outlined));
        group.AddButton(new MavButton("Week", MavButtonVariant.Outlined));
        group.AddButton(new MavButton("Month", MavButtonVariant.Outlined));
        return group;
    }

    private static MavButtonGroup ConnectedGroup()
    {
        var group = new MavButtonGroup(MavButtonGroupKind.Connected);
        group.AddButton(new MavButton("Bold", MavButtonVariant.Tonal, MavIcons.FormatBold));
        group.AddButton(new MavButton("Italic", MavButtonVariant.Tonal, MavIcons.FormatItalic));
        group.AddButton(new MavButton("Underline", MavButtonVariant.Tonal, MavIcons.FormatUnderlined));
        return group;
    }

    private static MavSplitButton SplitButton()
    {
        var split = new MavSplitButton("Save");
        split.Menu = new MavMenu()
            .AddItem("Save as draft", MavIcons.Description, onSelected: () => { })
            .AddItem("Save and close", MavIcons.Close, onSelected: () => { });
        return split;
    }

    private static MavFabMenu FabMenu()
    {
        var menu = new MavFabMenu();
        menu.AddItem("New document", MavIcons.NoteAdd, () => { });
        menu.AddItem("Upload", MavIcons.Upload, () => { });
        menu.AddItem("Scan", MavIcons.DocumentScanner, () => { });
        return menu;
    }
}
