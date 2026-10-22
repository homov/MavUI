using Avalonia;
using Avalonia.Controls;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>
/// Provides AXAML attached properties for declaratively binding <see cref="MavMenu"/>,
/// <see cref="MavPopover"/>, <see cref="MavRichTooltip"/>, <see cref="MavBottomSheet"/>,
/// <see cref="MavSideSheet"/>, <see cref="MavDialog"/>, and <see cref="MavModalNavigationDrawer"/>
/// to buttons, icon buttons, chips, list items, or any <see cref="Control"/> without code-behind.
/// </summary>
public static class MavFlyout
{
    public static readonly AttachedProperty<MavMenu?> MenuProperty =
        AvaloniaProperty.RegisterAttached<Control, MavMenu?>("Menu", typeof(MavFlyout));

    public static readonly AttachedProperty<MavPopover?> PopoverProperty =
        AvaloniaProperty.RegisterAttached<Control, MavPopover?>("Popover", typeof(MavFlyout));

    public static readonly AttachedProperty<MavRichTooltip?> RichTooltipProperty =
        AvaloniaProperty.RegisterAttached<Control, MavRichTooltip?>("RichTooltip", typeof(MavFlyout));

    public static readonly AttachedProperty<MavBottomSheet?> BottomSheetProperty =
        AvaloniaProperty.RegisterAttached<Control, MavBottomSheet?>("BottomSheet", typeof(MavFlyout));

    public static readonly AttachedProperty<MavSideSheet?> SideSheetProperty =
        AvaloniaProperty.RegisterAttached<Control, MavSideSheet?>("SideSheet", typeof(MavFlyout));

    public static readonly AttachedProperty<MavDialog?> DialogProperty =
        AvaloniaProperty.RegisterAttached<Control, MavDialog?>("Dialog", typeof(MavFlyout));

    public static readonly AttachedProperty<MavModalNavigationDrawer?> ModalDrawerProperty =
        AvaloniaProperty.RegisterAttached<Control, MavModalNavigationDrawer?>("ModalDrawer", typeof(MavFlyout));

    private static readonly AttachedProperty<bool> IsTriggerWiredProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsTriggerWired", typeof(MavFlyout));

    static MavFlyout()
    {
        MenuProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavMenu?>());
            EnsureTriggerWired(c);
        });
        PopoverProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavPopover?>());
            EnsureTriggerWired(c);
        });
        RichTooltipProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavRichTooltip?>());
            EnsureTriggerWired(c);
        });
        BottomSheetProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavBottomSheet?>());
            EnsureTriggerWired(c);
        });
        SideSheetProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavSideSheet?>());
            EnsureTriggerWired(c);
        });
        DialogProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavDialog?>());
            EnsureTriggerWired(c);
        });
        ModalDrawerProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MavModalNavigationDrawer?>());
            EnsureTriggerWired(c);
        });
    }

    /// <summary>
    /// Gets the menu attached to the element.
    /// </summary>
    public static MavMenu? GetMenu(Control element) => element.GetValue(MenuProperty);
    /// <summary>
    /// Attaches a menu to the element.
    /// </summary>
    public static void SetMenu(Control element, MavMenu? value) => element.SetValue(MenuProperty, value);

    /// <summary>
    /// Gets the popover attached to the element.
    /// </summary>
    public static MavPopover? GetPopover(Control element) => element.GetValue(PopoverProperty);
    /// <summary>
    /// Attaches a popover to the element.
    /// </summary>
    public static void SetPopover(Control element, MavPopover? value) => element.SetValue(PopoverProperty, value);

    /// <summary>
    /// Gets the rich tooltip attached to the element.
    /// </summary>
    public static MavRichTooltip? GetRichTooltip(Control element) => element.GetValue(RichTooltipProperty);
    /// <summary>
    /// Attaches a rich tooltip to the element.
    /// </summary>
    public static void SetRichTooltip(Control element, MavRichTooltip? value) => element.SetValue(RichTooltipProperty, value);

    /// <summary>
    /// Gets the bottom sheet attached to the element.
    /// </summary>
    public static MavBottomSheet? GetBottomSheet(Control element) => element.GetValue(BottomSheetProperty);
    /// <summary>
    /// Attaches a bottom sheet to the element.
    /// </summary>
    public static void SetBottomSheet(Control element, MavBottomSheet? value) => element.SetValue(BottomSheetProperty, value);

    /// <summary>
    /// Gets the side sheet attached to the element.
    /// </summary>
    public static MavSideSheet? GetSideSheet(Control element) => element.GetValue(SideSheetProperty);
    /// <summary>
    /// Attaches a side sheet to the element.
    /// </summary>
    public static void SetSideSheet(Control element, MavSideSheet? value) => element.SetValue(SideSheetProperty, value);

    /// <summary>
    /// Gets the dialog attached to the element.
    /// </summary>
    public static MavDialog? GetDialog(Control element) => element.GetValue(DialogProperty);
    /// <summary>
    /// Attaches a dialog to the element.
    /// </summary>
    public static void SetDialog(Control element, MavDialog? value) => element.SetValue(DialogProperty, value);

    /// <summary>
    /// Gets the modal navigation drawer attached to the element.
    /// </summary>
    public static MavModalNavigationDrawer? GetModalDrawer(Control element) => element.GetValue(ModalDrawerProperty);
    /// <summary>
    /// Attaches a modal navigation drawer to the element.
    /// </summary>
    public static void SetModalDrawer(Control element, MavModalNavigationDrawer? value) => element.SetValue(ModalDrawerProperty, value);

    private static void EnsureTriggerWired(Control control)
    {
        if (control.GetValue(IsTriggerWiredProperty))
            return;

        // Store the marker and handlers on the control. A static HashSet would root
        // every control that ever had a flyout attached.
        control.SetValue(IsTriggerWiredProperty, true);

        switch (control)
        {
            case MavButton btn:
                btn.Clicked += (_, _) => InvokeAttachedFlyouts(btn);
                break;
            case MavIconButton iconBtn:
                iconBtn.Clicked += (_, _) => InvokeAttachedFlyouts(iconBtn);
                break;
            case MavFab fab:
                fab.Clicked += (_, _) => InvokeAttachedFlyouts(fab);
                break;
            case MavExtendedFab extFab:
                extFab.Clicked += (_, _) => InvokeAttachedFlyouts(extFab);
                break;
            case MavChip chip:
                chip.Clicked += (_, _) => InvokeAttachedFlyouts(chip);
                break;
            case MavListItem listItem:
                listItem.Clicked += (_, _) => InvokeAttachedFlyouts(listItem);
                break;
            default:
                control.PointerReleased += (_, _) => InvokeAttachedFlyouts(control);
                break;
        }
    }

    private static void InvokeAttachedFlyouts(Control anchor)
    {
        if (GetMenu(anchor) is { } menu)
            menu.Show(anchor);

        if (GetPopover(anchor) is { } popover)
            popover.Toggle(anchor);

        if (GetRichTooltip(anchor) is { } richTip)
            richTip.Show(anchor);

        if (GetBottomSheet(anchor) is { } bottomSheet)
            bottomSheet.Show(anchor);

        if (GetSideSheet(anchor) is { } sideSheet)
            sideSheet.Show(anchor);

        if (GetDialog(anchor) is { } dialog)
            dialog.Show(anchor);

        if (GetModalDrawer(anchor) is { } drawer)
            drawer.Show(anchor);
    }
}
