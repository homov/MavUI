using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

// Defaults and state rules of the last controls without direct tests.
// Overlays and animations are not shown or awaited.
public class RemainingControlTests
{
    [AvaloniaFact]
    public void AvatarGroup_defaults_to_four_visible_avatars_of_38_pixels()
    {
        var group = new MavAvatarGroup();

        Assert.Equal(4, group.MaxDisplayed);
        Assert.Equal(38.0, group.AvatarSize);
    }

    [AvaloniaFact]
    public void AvatarGroup_Add_keeps_every_added_avatar()
    {
        var group = new MavAvatarGroup();

        group.Add("AB");
        group.Add("CD");

        var count = 0;
        foreach (var avatar in group) count++;
        Assert.Equal(2, count);
    }

    [AvaloniaFact]
    public void ExpansionPanelList_allows_several_open_panels_by_default()
    {
        var list = new MavExpansionPanelList();

        Assert.False(list.SingleExpand);
        Assert.True(list.AllowMultipleExpanded);
    }

    [AvaloniaFact]
    public void ExpansionPanelList_single_expand_forbids_several_open_panels()
    {
        var list = new MavExpansionPanelList(true);

        Assert.True(list.SingleExpand);
        Assert.False(list.AllowMultipleExpanded);
    }

    [AvaloniaFact]
    public void ChatBubble_keeps_text_and_direction()
    {
        var bubble = new MavChatBubble { Text = "On my way", IsOutgoing = true };

        Assert.Equal("On my way", bubble.Text);
        Assert.True(bubble.IsOutgoing);
    }

    [AvaloniaFact]
    public void FabMenuItem_keeps_label_and_icon_and_has_no_handler_by_default()
    {
        var item = new MavFabMenuItem("Share", "\uE80D");

        Assert.Equal("Share", item.Label);
        Assert.Equal("\uE80D", item.Icon);
        Assert.Null(item.OnClick);
    }

    [AvaloniaFact]
    public void ListSubheader_shows_its_text()
    {
        var header = new MavListSubheader("Recent");

        Assert.Equal("Recent", header.Text);
    }

    [AvaloniaFact]
    public void Sparkline_parses_ValuesText_with_commas_semicolons_and_spaces()
    {
        var sparkline = new MavSparkline { ValuesText = "1, 2.5; 3" };

        Assert.Equal(3, sparkline.Values!.Count);
        Assert.Equal(2.5, sparkline.Values[1]);
    }

    [AvaloniaFact]
    public void Sparkline_is_a_line_chart_by_default()
    {
        var sparkline = new MavSparkline();

        Assert.Equal(MavSparklineKind.Line, sparkline.Kind);
    }

    [AvaloniaFact]
    public void SwipeAction_Invoke_runs_both_the_callback_and_the_event_once()
    {
        var callbackRuns = 0;
        var eventRuns = 0;
        var action = new MavSwipeAction
        {
            Label = "Delete",
            IsDestructive = true,
            TriggeredCallback = () => callbackRuns++,
        };
        action.Triggered += (_, _) => eventRuns++;

        action.Invoke();

        Assert.Equal(1, callbackRuns);
        Assert.Equal(1, eventRuns);
    }

    [AvaloniaFact]
    public void Toolbar_defaults_to_floating_standard_horizontal()
    {
        var toolbar = new MavToolbar();

        Assert.Equal(MavToolbarVariant.Floating, toolbar.Variant);
        Assert.Equal(MavToolbarColorStyle.Standard, toolbar.ColorStyle);
        Assert.Equal(Orientation.Horizontal, toolbar.Orientation);
    }

    [AvaloniaFact]
    public void CarouselItem_keeps_title_and_subtitle()
    {
        var item = new MavCarouselItem { Title = "Paris", Subtitle = "France" };

        Assert.Equal("Paris", item.Title);
        Assert.Equal("France", item.Subtitle);
    }

    [AvaloniaFact]
    public void FileDropZone_allows_several_files_by_default_and_can_be_limited_to_one()
    {
        Assert.True(new MavFileDropZone().AllowMultiple);

        var single = new MavFileDropZone("Drop a file", null, false);

        Assert.Equal("Drop a file", single.Title);
        Assert.False(single.AllowMultiple);
    }

    [AvaloniaFact]
    public void DateRangePickerField_has_no_dates_by_default()
    {
        var field = new MavDateRangePickerField();

        Assert.Null(field.StartDate);
        Assert.Null(field.EndDate);
    }

    [AvaloniaFact]
    public void WavyProgressIndicator_can_be_created_with_defaults()
    {
        var indicator = new MavWavyProgressIndicator();

        Assert.NotNull(indicator);
    }
}
