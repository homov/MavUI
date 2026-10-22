using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Headless.XUnit;
using Mav.Controls;
using Mav.Platform;
using Xunit;

namespace Mav.Tests;

public class AccessibilityTests
{
    [AvaloniaFact]
    public void Button_name_follows_text()
    {
        var button = new MavButton("Save");
        Assert.Equal("Save", AutomationProperties.GetName(button));
        Assert.Equal(AutomationControlType.Button, AutomationProperties.GetControlTypeOverride(button));

        button.Text = "Publish";
        Assert.Equal("Publish", AutomationProperties.GetName(button));
    }

    [AvaloniaFact]
    public void IconButton_uses_tooltip_as_name()
    {
        var button = new MavIconButton(MavIcons.Search);
        global::Avalonia.Controls.ToolTip.SetTip(button, "Search");
        Assert.Equal("Search", AutomationProperties.GetName(button));
    }

    [AvaloniaFact]
    public void TextField_name_and_help_follow_label_and_footer()
    {
        var field = new MavTextField("Email", supportingText: "Work address");
        Assert.Equal("Email", AutomationProperties.GetName(field));
        Assert.Equal("Work address", AutomationProperties.GetHelpText(field));
        Assert.Equal(AutomationControlType.Edit, AutomationProperties.GetControlTypeOverride(field));

        field.ErrorText = "Invalid email";
        Assert.Equal("Invalid email", AutomationProperties.GetHelpText(field));

        field.Label = "Recovery email";
        Assert.Equal("Recovery email", AutomationProperties.GetName(field));
    }

    [AvaloniaFact]
    public void Checkbox_and_radio_expose_labels_and_roles()
    {
        var check = new MavCheckBox("Remember me");
        Assert.Equal("Remember me", AutomationProperties.GetName(check));
        Assert.Equal(AutomationControlType.CheckBox, AutomationProperties.GetControlTypeOverride(check));

        var radio = new MavRadioButton { Label = "Admin" };
        Assert.Equal("Admin", AutomationProperties.GetName(radio));
        Assert.Equal(AutomationControlType.RadioButton, AutomationProperties.GetControlTypeOverride(radio));
    }

    [AvaloniaFact]
    public void ListItem_exposes_headline_and_supporting_text()
    {
        var item = new MavListItem
        {
            Headline = "Design review",
            SupportingText = "10:30 – 11:15",
        };

        Assert.Equal("Design review", AutomationProperties.GetName(item));
        Assert.Equal("10:30 – 11:15", AutomationProperties.GetHelpText(item));
        Assert.Equal(AutomationControlType.ListItem, AutomationProperties.GetControlTypeOverride(item));
    }

    [AvaloniaFact]
    public void Chip_and_menu_item_expose_their_text()
    {
        var chip = new MavChip("Filter");
        Assert.Equal("Filter", AutomationProperties.GetName(chip));

        var menuItem = new MavMenuItem("Rename");
        Assert.Equal("Rename", AutomationProperties.GetName(menuItem));
        Assert.Equal(AutomationControlType.MenuItem, AutomationProperties.GetControlTypeOverride(menuItem));
    }

    [AvaloniaFact]
    public void Required_validation_marks_the_field_for_forms()
    {
        var field = new MavTextField("Name");
        Assert.False(AutomationProperties.GetIsRequiredForForm(field));

        MavValidation.SetIsRequired(field, true);
        Assert.True(AutomationProperties.GetIsRequiredForForm(field));

        MavValidation.SetIsRequired(field, false);
        Assert.False(AutomationProperties.GetIsRequiredForForm(field));
    }

    [AvaloniaFact]
    public void Application_set_name_is_not_overwritten()
    {
        var button = new MavButton("Save");
        AutomationProperties.SetName(button, "Save document");

        button.Text = "Publish";

        Assert.Equal("Save document", AutomationProperties.GetName(button));
    }

    [AvaloniaFact]
    public void Automation_helpers_clear_generated_values()
    {
        var button = new MavButton("Save");
        Assert.Equal("Save", AutomationProperties.GetName(button));

        MavAutomation.SetName(button, null);

        Assert.True(string.IsNullOrEmpty(AutomationProperties.GetName(button)));
    }
}
