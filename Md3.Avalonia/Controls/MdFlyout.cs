using Avalonia;
using Avalonia.Controls;
using Md3.Avalonia.Primitives;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Provides AXAML attached properties for declaratively binding <see cref="MdMenu"/>,
/// <see cref="MdPopover"/>, <see cref="MdRichTooltip"/>, <see cref="MdBottomSheet"/>,
/// <see cref="MdSideSheet"/>, <see cref="MdDialog"/>, and <see cref="MdModalNavigationDrawer"/>
/// to buttons, icon buttons, chips, list items, or any <see cref="Control"/> without code-behind.
/// </summary>
public static class MdFlyout
{
    public static readonly AttachedProperty<MdMenu?> MenuProperty =
        AvaloniaProperty.RegisterAttached<Control, MdMenu?>("Menu", typeof(MdFlyout));

    public static readonly AttachedProperty<MdPopover?> PopoverProperty =
        AvaloniaProperty.RegisterAttached<Control, MdPopover?>("Popover", typeof(MdFlyout));

    public static readonly AttachedProperty<MdRichTooltip?> RichTooltipProperty =
        AvaloniaProperty.RegisterAttached<Control, MdRichTooltip?>("RichTooltip", typeof(MdFlyout));

    public static readonly AttachedProperty<MdBottomSheet?> BottomSheetProperty =
        AvaloniaProperty.RegisterAttached<Control, MdBottomSheet?>("BottomSheet", typeof(MdFlyout));

    public static readonly AttachedProperty<MdSideSheet?> SideSheetProperty =
        AvaloniaProperty.RegisterAttached<Control, MdSideSheet?>("SideSheet", typeof(MdFlyout));

    public static readonly AttachedProperty<MdDialog?> DialogProperty =
        AvaloniaProperty.RegisterAttached<Control, MdDialog?>("Dialog", typeof(MdFlyout));

    public static readonly AttachedProperty<MdModalNavigationDrawer?> ModalDrawerProperty =
        AvaloniaProperty.RegisterAttached<Control, MdModalNavigationDrawer?>("ModalDrawer", typeof(MdFlyout));

    private static readonly AttachedProperty<bool> IsTriggerWiredProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsTriggerWired", typeof(MdFlyout));

    static MdFlyout()
    {
        MenuProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdMenu?>());
            EnsureTriggerWired(c);
        });
        PopoverProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdPopover?>());
            EnsureTriggerWired(c);
        });
        RichTooltipProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdRichTooltip?>());
            EnsureTriggerWired(c);
        });
        BottomSheetProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdBottomSheet?>());
            EnsureTriggerWired(c);
        });
        SideSheetProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdSideSheet?>());
            EnsureTriggerWired(c);
        });
        DialogProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdDialog?>());
            EnsureTriggerWired(c);
        });
        ModalDrawerProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(c, e.GetNewValue<MdModalNavigationDrawer?>());
            EnsureTriggerWired(c);
        });
    }

    public static MdMenu? GetMenu(Control element) => element.GetValue(MenuProperty);
    public static void SetMenu(Control element, MdMenu? value) => element.SetValue(MenuProperty, value);

    public static MdPopover? GetPopover(Control element) => element.GetValue(PopoverProperty);
    public static void SetPopover(Control element, MdPopover? value) => element.SetValue(PopoverProperty, value);

    public static MdRichTooltip? GetRichTooltip(Control element) => element.GetValue(RichTooltipProperty);
    public static void SetRichTooltip(Control element, MdRichTooltip? value) => element.SetValue(RichTooltipProperty, value);

    public static MdBottomSheet? GetBottomSheet(Control element) => element.GetValue(BottomSheetProperty);
    public static void SetBottomSheet(Control element, MdBottomSheet? value) => element.SetValue(BottomSheetProperty, value);

    public static MdSideSheet? GetSideSheet(Control element) => element.GetValue(SideSheetProperty);
    public static void SetSideSheet(Control element, MdSideSheet? value) => element.SetValue(SideSheetProperty, value);

    public static MdDialog? GetDialog(Control element) => element.GetValue(DialogProperty);
    public static void SetDialog(Control element, MdDialog? value) => element.SetValue(DialogProperty, value);

    public static MdModalNavigationDrawer? GetModalDrawer(Control element) => element.GetValue(ModalDrawerProperty);
    public static void SetModalDrawer(Control element, MdModalNavigationDrawer? value) => element.SetValue(ModalDrawerProperty, value);

    private static void EnsureTriggerWired(Control control)
    {
        if (control.GetValue(IsTriggerWiredProperty))
            return;

        // Store the marker and handlers on the control. A static HashSet would root
        // every control that ever had a flyout attached.
        control.SetValue(IsTriggerWiredProperty, true);

        switch (control)
        {
            case MdButton btn:
                btn.Clicked += (_, _) => InvokeAttachedFlyouts(btn);
                break;
            case MdIconButton iconBtn:
                iconBtn.Clicked += (_, _) => InvokeAttachedFlyouts(iconBtn);
                break;
            case MdFab fab:
                fab.Clicked += (_, _) => InvokeAttachedFlyouts(fab);
                break;
            case MdExtendedFab extFab:
                extFab.Clicked += (_, _) => InvokeAttachedFlyouts(extFab);
                break;
            case MdChip chip:
                chip.Clicked += (_, _) => InvokeAttachedFlyouts(chip);
                break;
            case MdListItem listItem:
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
