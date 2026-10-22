using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

// Property defaults and state rules of MD3 components that had no direct tests.
// Overlays (side and bottom sheets, tooltips) are not shown here, so no state leaks between tests.
public class MD3ComponentTests
{
    [AvaloniaFact]
    public void Timeline_keeps_items_in_order_and_Clear_removes_them()
    {
        var timeline = new MavTimeline();
        timeline.Add(new MavTimelineItem { Title = "Ordered" });
        timeline.Add(new MavTimelineItem { Title = "Shipped" });

        Assert.Equal(2, timeline.Items.Count);
        Assert.Equal("Shipped", timeline.Items[1].Title);

        timeline.Clear();

        Assert.Empty(timeline.Items);
    }

    [AvaloniaFact]
    public void TimelineItem_is_completed_by_default()
    {
        var item = new MavTimelineItem { Title = "Done" };

        Assert.Equal(MavTimelineStatus.Completed, item.Status);
        Assert.True(item.IsCompleted);
        Assert.False(item.IsActive);
    }

    [AvaloniaFact]
    public void TimelineItem_IsActive_true_makes_the_item_active()
    {
        var item = new MavTimelineItem { Title = "Current" };

        item.IsActive = true;

        Assert.Equal(MavTimelineStatus.Active, item.Status);
        Assert.False(item.IsCompleted);
    }

    [AvaloniaFact]
    public void TimelineItem_IsCompleted_false_does_not_change_an_active_item()
    {
        var item = new MavTimelineItem { Title = "Current", Status = MavTimelineStatus.Active };

        item.IsCompleted = false;

        Assert.Equal(MavTimelineStatus.Active, item.Status);
    }

    [AvaloniaFact]
    public void Step_is_required_by_default_and_can_be_made_optional()
    {
        var step = new MavStep { Title = "Address" };

        Assert.Equal("Address", step.Title);
        Assert.False(step.IsOptional);

        step.IsOptional = true;

        Assert.True(step.IsOptional);
    }

    [AvaloniaFact]
    public void LoadingIndicator_defaults_to_48_and_not_contained()
    {
        var indicator = new MavLoadingIndicator();

        Assert.Equal(48.0, indicator.Size);
        Assert.False(indicator.IsContained);
    }

    [AvaloniaFact]
    public void LoadingIndicator_constructor_arguments_set_size_and_containment()
    {
        var indicator = new MavLoadingIndicator(32, true);

        Assert.Equal(32.0, indicator.Size);
        Assert.True(indicator.IsContained);
    }

    [AvaloniaFact]
    public void BottomSheet_defaults_show_handle_allow_scrim_dismiss_and_stay_closed()
    {
        var sheet = new MavBottomSheet();

        Assert.True(sheet.ShowDragHandle);
        Assert.True(sheet.DismissOnScrimTap);
        Assert.False(sheet.IsOpen);
        Assert.Null(sheet.Title);
    }

    [AvaloniaFact]
    public void SideSheet_defaults_to_end_placement_and_320_width()
    {
        var sheet = new MavSideSheet();

        Assert.Equal(MavSideSheetPlacement.End, sheet.Placement);
        Assert.Equal(320.0, sheet.SheetWidth);
        Assert.False(sheet.ShowBackButton);
        Assert.False(sheet.IsOpen);
    }

    [AvaloniaFact]
    public void SideSheet_placement_can_move_to_the_start_edge()
    {
        var sheet = new MavSideSheet { Placement = MavSideSheetPlacement.Start };

        Assert.Equal(MavSideSheetPlacement.Start, sheet.Placement);
    }

    [AvaloniaFact]
    public void RichTooltip_SupportingText_mirrors_Text_and_has_no_action_by_default()
    {
        var tooltip = new MavRichTooltip { Text = "Saves the draft" };

        Assert.Equal("Saves the draft", tooltip.SupportingText);
        Assert.Null(tooltip.ActionText);
    }
}
