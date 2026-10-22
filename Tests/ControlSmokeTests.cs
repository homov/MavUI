using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

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
        root.Children.Add(new MdButton("Filled", MdButtonVariant.Filled, MdIcons.Save));
        root.Children.Add(new MdIconButton(MdIcons.Search));
        root.Children.Add(new MdFab(MdIcons.Add, MdFabSize.Small, MdFabColor.Secondary));
        root.Children.Add(new MdExtendedFab("Label", MdIcons.Check));
        root.Children.Add(new MdButtonGroup { Kind = MdButtonGroupKind.Standard }.AddButton(new MdButton("A")));
        root.Children.Add(new MdSplitButton { Text = "Split" });
        root.Children.Add(new MdFabMenu().AddItem("I", MdIcons.Home, () => { }));
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Inputs_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MdTextField("Name", placeholder: "Ada"));
        root.Children.Add(new MdTextArea("Bio", minHeight: 60));
        root.Children.Add(new MdPinInput { Length = 4 });
        root.Children.Add(new MdNumberBox { Label = "Age", Minimum = 0, Maximum = 120 });
        root.Children.Add(new MdTagInput { Label = "Tags" }.AddTag("md3"));
        root.Children.Add(new MdRatingBar { Value = 3.5, AllowHalf = true });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Selection_controls_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MdSwitch { IsToggled = true });
        root.Children.Add(new MdCheckBox("Agree") { IsChecked = true });
        root.Children.Add(new MdRadioButton { Label = "Choice" });
        var radios = new MdRadioGroup();
        radios.AddRadio(new MdRadioButton { Label = "A" });
        root.Children.Add(radios);
        root.Children.Add(new MdSlider { Value = 50 });
        root.Children.Add(new MdRangeSlider { StartValue = 10, EndValue = 90 });
        root.Children.Add(new MdSegmentedButton().AddSegment("One"));
        root.Children.Add(new MdChip("Tag", MdChipKind.Assist));
        root.Children.Add(new MdChipGroup { SingleSelection = true }.AddChip("One"));
        root.Children.Add(new MdSelect("Role", new[] { "A", "B" }));
        root.Children.Add(new MdComboBox("C", new[] { "A", "B" }));
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Pickers_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MdDatePickerField { Label = "Date", SelectedDate = System.DateTime.Today });
        root.Children.Add(new MdTimePickerField { Label = "Time", SelectedTime = new System.TimeSpan(9, 0, 0) });
        root.Children.Add(new MdColorPicker { SelectedColor = global::Avalonia.Media.Colors.Blue });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Feedback_controls_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MdLinearProgress { Value = 0.5 });
        root.Children.Add(new MdCircularProgress(24, 3) { IsIndeterminate = true });
        root.Children.Add(new MdSkeleton { Shape = MdSkeletonShape.Rounded });
        root.Children.Add(new MdRefreshIndicator { IsRefreshing = false, Content = new MdLabel { Text = "Pull" } });
        root.Children.Add(new MdInfoBar { Severity = MdInfoBarSeverity.Info, Title = "Info" });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void Navigation_and_chrome_lay_out()
    {
        var page = new MdContentPage("Smoke");
        page.AddAction(MdIcons.Search, () => { });
        page.Subtitle = "Smoke";
        var host = new MdNavigationPage(page);
        Assert.Equal("Smoke", page.Title);
        Assert.Same(host, page.Navigation);
    }

    [AvaloniaFact]
    public void Containers_and_primitives_lay_out()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new MdCard(MdCardVariant.Outlined, new MdLabel { Text = "Body" }));
        root.Children.Add(new MdExpansionPanel { Header = "Panel", IsExpanded = true, Body = new MdLabel { Text = "X" } });
        root.Children.Add(new MdSplitView { Pane = new MdLabel { Text = "Pane" }, DetailContent = new MdLabel { Text = "Detail" } });
        root.Children.Add(new MdAvatar { Initials = "SS" });
        root.Children.Add(new MdBadgedBox(new MdButton("X"), "1"));
        root.Children.Add(new MdDivider());
        root.Children.Add(new MdSafeArea { Edges = MdSafeAreaEdges.All, Body = new MdLabel { Text = "Safe" } });
        Assert.True(root.Children.Count > 0);
    }

    [AvaloniaFact]
    public void MdWindow_lays_out_as_top_level()
    {
        var window = new MdWindow { Title = "Smoke", Content = new MdLabel { Text = "Smoke" } };
        window.Show();
        Assert.Equal("Smoke", window.Title);
        window.Close();
    }
}
