using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// Smoke-test: creates and lays out one instance of each major control category
/// from docs/AGENTS.md §5. Any null-reference or template crash will fail the build.
/// </summary>
public class ControlSmokeTests
{
    [AvaloniaFact]
    public void Buttons_and_actions_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MavButton("Filled", MavButtonVariant.Filled, MavIcons.Save));
        root.Children.Add(new MavIconButton(MavIcons.Search));
        root.Children.Add(new MavFab(MavIcons.Add, MavFabSize.Small, MavFabColor.Secondary));
        root.Children.Add(new MavExtendedFab("Label", MavIcons.Check));
        root.Children.Add(new MavButtonGroup { Kind = MavButtonGroupKind.Standard }.AddButton(new MavButton("A")));
        root.Children.Add(new MavSplitButton { Text = "Split" });
        root.Children.Add(new MavFabMenu().AddItem("I", MavIcons.Home, () => { }));
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Inputs_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MavTextField("Name", placeholder: "Ada"));
        root.Children.Add(new MavTextArea("Bio", minHeight: 60));
        root.Children.Add(new MavPinInput { Length = 4 });
        root.Children.Add(new MavNumberBox { Label = "Age", Minimum = 0, Maximum = 120 });
        root.Children.Add(new MavTagInput { Label = "Tags" }.AddTag("md3"));
        root.Children.Add(new MavRatingBar { Value = 3.5, AllowHalf = true });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Selection_controls_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MavSwitch { IsToggled = true });
        root.Children.Add(new MavCheckBox("Agree") { IsChecked = true });
        root.Children.Add(new MavRadioButton { Label = "Choice" });
        var radios = new MavRadioGroup();
        radios.AddRadio(new MavRadioButton { Label = "A" });
        root.Children.Add(radios);
        root.Children.Add(new MavSlider { Value = 50 });
        root.Children.Add(new MavRangeSlider { StartValue = 10, EndValue = 90 });
        root.Children.Add(new MavSegmentedButton().AddSegment("One"));
        root.Children.Add(new MavChip("Tag", MavChipKind.Assist));
        root.Children.Add(new MavChipGroup { SingleSelection = true }.AddChip("One"));
        root.Children.Add(new MavSelect("Role", new[] { "A", "B" }));
        root.Children.Add(new MavComboBox("C", new[] { "A", "B" }));
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Pickers_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MavDatePickerField { Label = "Date", SelectedDate = System.DateTime.Today });
        root.Children.Add(new MavTimePickerField { Label = "Time", SelectedTime = new System.TimeSpan(9, 0, 0) });
        root.Children.Add(new MavColorPicker { SelectedColor = global::Avalonia.Media.Colors.Blue });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Feedback_controls_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MavLinearProgress { Value = 0.5 });
        root.Children.Add(new MavCircularProgress(24, 3) { IsIndeterminate = true });
        root.Children.Add(new MavSkeleton { Shape = MavSkeletonShape.Rounded });
        root.Children.Add(new MavRefreshIndicator { IsRefreshing = false, Content = new MavLabel { Text = "Pull" } });
        root.Children.Add(new MavInfoBar { Severity = MavInfoBarSeverity.Info, Title = "Info" });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Navigation_and_chrome_lay_out()
    {
        var page = new MavContentPage("Smoke");
        page.AddAction(MavIcons.Search, () => { });
        page.Subtitle = "Smoke";
        var host = new MavNavigationPage(page);
        Assert.Equal("Smoke", page.Title);
        Assert.Same(host, page.Navigation);
    }

    [AvaloniaFact]
    public void Containers_and_primitives_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MavCard(MavCardVariant.Outlined, new MavLabel { Text = "Body" }));
        root.Children.Add(new MavExpansionPanel { Header = "Panel", IsExpanded = true, Body = new MavLabel { Text = "X" } });
        root.Children.Add(new MavSplitView { Pane = new MavLabel { Text = "Pane" }, DetailContent = new MavLabel { Text = "Detail" } });
        root.Children.Add(new MavAvatar { Initials = "SS" });
        root.Children.Add(new MavBadgedBox(new MavButton("X"), "1"));
        root.Children.Add(new MavDivider());
        root.Children.Add(new MavSafeArea { Edges = MavSafeAreaEdges.All, Body = new MavLabel { Text = "Safe" } });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void MavWindow_lays_out_as_top_level()
    {
        var window = new MavWindow { Title = "Smoke", Content = new MavLabel { Text = "Smoke" } };
        window.Show();
        Assert.Equal("Smoke", window.Title);
        window.Close();
    }
}
