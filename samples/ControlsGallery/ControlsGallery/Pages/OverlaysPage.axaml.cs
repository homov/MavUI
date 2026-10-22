using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class OverlaysPage : UserControl
{
    public OverlaysPage()
    {
        InitializeComponent();

        MenusHost.Children.Add(BuildMenus());
        TooltipsHost.Children.Add(BuildTooltips());
        SearchHost.Children.Add(BuildSearch());
        FlyoutsHost.Children.Add(BuildFlyouts());
    }

    private static Control Row(params Control[] children)
    {
        var panel = new WrapPanel();
        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            panel.Children.Add(child);
        }
        return panel;
    }

    private static Control BuildMenus()
    {
        var menu = new MavMenu()
            .AddItem(MavLocalization.Get("Overlays.Menu.Rename", "Rename"), MavIcons.Edit, onSelected: () => { })
            .AddItem(MavLocalization.Get("Overlays.Menu.Duplicate", "Duplicate"), MavIcons.ContentCopy, onSelected: () => { })
            .AddDivider()
            .AddSubmenu(MavLocalization.Get("Overlays.Menu.MoveTo", "Move to"), sub => sub
                .AddItem(MavLocalization.Get("Overlays.Menu.Archive", "Archive"), MavIcons.Archive, onSelected: () => { })
                .AddItem(MavLocalization.Get("Overlays.Menu.Trash", "Trash"), MavIcons.Delete, onSelected: () => { }),
                MavIcons.DriveFileMove)
            .AddItem(MavLocalization.Get("Overlays.Menu.Delete", "Delete"), MavIcons.Delete, onSelected: () => { });

        var menuButton = new MavButton(MavLocalization.Get("Overlays.Menu.Open", "Open menu"), MavButtonVariant.Outlined, MavIcons.MoreVert);
        menuButton.Clicked += (_, _) => menu.Show(menuButton);

        var popover = new MavPopover
        {
            Title = MavLocalization.Get("Overlays.Popover.Title", "Details"),
            Placement = MavPopoverPlacement.Bottom,
            Content = new MavLabel
            {
                Text = MavLocalization.Get("Overlays.Popover.Body",
                    "Any control can live in a popover. It clamps to the viewport and stays near its anchor."),
                TextWrapping = TextWrapping.Wrap,
            },
        };

        var popoverButton = new MavButton(MavLocalization.Get("Overlays.Popover.Open", "Open popover"), MavButtonVariant.Outlined, MavIcons.OpenInNew);
        popoverButton.Clicked += (_, _) => popover.Show(popoverButton);

        return Row(menuButton, popoverButton);
    }

    private static Control BuildTooltips()
    {
        var simple = new MavButton(MavLocalization.Get("Overlays.Tip.Hover", "Hover for tooltip"), MavButtonVariant.Tonal, MavIcons.Info);
        MavTooltip.SetText(simple, MavLocalization.Get("Overlays.Tip.Text", "A simple MD3 tooltip"));

        var rich = new MavButton(MavLocalization.Get("Overlays.Tip.Rich", "Rich tooltip"), MavButtonVariant.Outlined, MavIcons.Keyboard);
        rich.Clicked += (_, _) => MavRichTooltip.Show(
            rich,
            MavLocalization.Get("Overlays.Tip.ShortcutTitle", "Keyboard shortcut"),
            MavLocalization.Get("Overlays.Tip.ShortcutText", "Ctrl+K opens the command palette."),
            actionText: MavLocalization.Get("Overlays.Tip.GotIt", "Got it"));

        return Row(simple, rich);
    }

    private static Control BuildSearch()
    {
        var button = new MavButton(MavLocalization.Get("Overlays.Search.Open", "Open search view"), MavButtonVariant.Filled, MavIcons.Search);
        var view = new MavSearchView
        {
            Placeholder = MavLocalization.Get("Overlays.Search.Placeholder", "Search every control"),
        };
        button.Clicked += (_, _) => view.Show(button);
        return button;
    }

    private static Control BuildFlyouts()
    {
        var menu = new MavMenu()
            .AddItem("First", MavIcons.LooksOne, onSelected: () => { })
            .AddItem("Second", MavIcons.LooksTwo, onSelected: () => { });

        var menuButton = new MavButton(MavLocalization.Get("Overlays.Flyout.Menu", "Flyout menu"), MavButtonVariant.Outlined, MavIcons.Menu);
        MavFlyout.SetMenu(menuButton, menu);

        var dialogButton = new MavButton(MavLocalization.Get("Overlays.Flyout.Dialog", "Flyout dialog"), MavButtonVariant.Outlined, MavIcons.Chat);
        MavFlyout.SetDialog(dialogButton, new MavDialog
        {
            Title = MavLocalization.Get("Overlays.Flyout.DialogTitle", "Declarative dialog"),
            Message = MavLocalization.Get("Overlays.Flyout.DialogMessage", "Opened by MavFlyout when the button is clicked."),
            ConfirmText = MavLocalization.Get("Overlays.Flyout.Close", "Close"),
        });

        var drawerButton = new MavButton(MavLocalization.Get("Overlays.Flyout.Drawer", "Flyout drawer"), MavButtonVariant.Outlined, MavIcons.MenuOpen);
        var drawer = new MavModalNavigationDrawer(MavLocalization.Get("Overlays.Flyout.Drawer", "Flyout drawer"));
        drawer.AddItem("One", MavIcons.LooksOne);
        drawer.AddItem("Two", MavIcons.LooksTwo);
        MavFlyout.SetModalDrawer(drawerButton, drawer);

        return Row(menuButton, dialogButton, drawerButton);
    }
}
