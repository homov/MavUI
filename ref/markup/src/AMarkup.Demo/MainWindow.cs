using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace AMarkup.Demo;

/// <summary>
/// The whole window is built exclusively with generated fluent extension methods.
/// There are no XAML files, no object initializers and no direct UI property assignments.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        _viewModel = new MainViewModel();

        this
            .DataContext(_viewModel)
            .Title("AMarkup — fully fluent Avalonia UI")
            .Width(1060)
            .Height(800)
            .MinWidth(760)
            .MinHeight(560)
            .WindowStartupLocation(WindowStartupLocation.CenterScreen)
            .Background("#0E141B")
            .Resources(new ResourceDictionary
            {
                ["AccentBrush"] = new SolidColorBrush(Color.Parse("#F59E0B")),
                ["AccentTextBrush"] = new SolidColorBrush(Color.Parse("#111827")),
            })
            .UseMarkupHotReload(RebuildContent)
            .OnClosed(() => MarkupHotReload.Unregister(this));

        Styles.Add(
            new Style<Button>(selector => selector.Class("accent"))
                .Background(MarkupResource.DynamicResource("AccentBrush"))
                .Foreground(MarkupResource.DynamicResource("AccentTextBrush"))
                .Padding(14, 8)
                .CornerRadius(7)
                .FontWeight(FontWeight.SemiBold));

        RebuildContent();
    }

    private void RebuildContent()
    {
        this.Content(
            new ScrollViewer()
                .Content(BuildDashboard())
                .Padding(24));
    }

    private Control BuildDashboard()
    {
        var header = new StackPanel()
            .Children(
                new TextBlock()
                    .Text("AMarkup fluent dashboard")
                    .FontSize(32)
                    .FontWeight(FontWeight.Bold)
                    .Foreground("#4CC2FF"),
                new TextBlock()
                    .Text("Properties, bindings, children and attached properties are all set by generated extension methods.")
                    .FontSize(14)
                    .Foreground("#8FA3BF"),
                new TextBlock()
                    .Text(_viewModel, x => x.UserName)
                    .FontSize(13)
                    .Foreground("#6EE7B7"))
            .Spacing(4)
            .Margin(0, 0, 0, 20);

        var content = new Grid()
            .Cols("3*,2*")
            .Children(
                new StackPanel()
                    .Children(
                        BuildCounterCard(),
                        BuildInputCard(),
                        BuildCollectionCard())
                    .GridColumn(0)
                    .Margin(0, 0, 14, 0),
                new StackPanel()
                    .Children(
                        BuildAttachedPropertiesCard(),
                        BuildDockCard(),
                        BuildVisibilityCard(),
                        BuildConvertersCard(),
                        BuildStyleCard())
                    .GridColumn(1))
            .Rows("Auto");

        return new StackPanel()
            .Children(header, content)
            .Spacing(0)
            .Margin(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top);
    }

    private Control BuildCounterCard()
    {
        var increment = new Button()
            .Content("Increment")
            .Padding(14, 8)
            .CornerRadius(7)
            .Background("#2563EB")
            .Foreground("White")
            .HorizontalAlignment(HorizontalAlignment.Left)
            .OnClick(() => _viewModel.IncrementCounter());

        var reset = new Button()
            .Content("Reset")
            .Padding(14, 8)
            .CornerRadius(7)
            .Background("#374151")
            .Foreground("White")
            .HorizontalAlignment(HorizontalAlignment.Left)
            .OnClick(() => _viewModel.Counter = 0);

        var counter = new TextBlock()
            .Text(_viewModel, x => x.Counter)
            .FontSize(42)
            .FontWeight(FontWeight.Bold)
            .Foreground("#F8FAFC")
            .HorizontalAlignment(HorizontalAlignment.Center);

        return CreateCard(
            "Counter — value + binding overloads",
            new TextBlock()
                .Text("The number below is bound through the generated expression overload.")
                .FontSize(13)
                .Foreground("#9CA3AF"),
            counter,
            new StackPanel()
                .Children(increment, reset)
                .Orientation(Orientation.Horizontal)
                .Spacing(10));
    }

    private Control BuildInputCard()
    {
        var nameBox = new TextBox()
            .Text(_viewModel, x => x.UserName, BindingMode.TwoWay)
            .Watermark("Type your name…")
            .Padding(10, 6)
            .CornerRadius(7)
            .Background("#0F172A")
            .Foreground("#E5E7EB")
            .BorderBrush("#334155")
            .BorderThickness(1);

        var greeting = new TextBlock()
            .Text(_viewModel, x => x.UserName)
            .FontSize(14)
            .Foreground("#6EE7B7");

        var slider = new Slider()
            .Minimum(0)
            .Maximum(100)
            .Value(_viewModel, x => x.SliderValue, BindingMode.TwoWay)
            .Width(280)
            .HorizontalAlignment(HorizontalAlignment.Left);

        var sliderValue = new TextBlock()
            .Text(_viewModel, x => x.SliderValue)
            .FontSize(13)
            .Foreground("#9CA3AF");

        var datePicker = new DatePicker()
            .SelectedDate(_viewModel, x => x.SelectedDate, BindingMode.TwoWay)
            .Width(230)
            .HorizontalAlignment(HorizontalAlignment.Left);

        return CreateCard(
            "Inputs — two-way compiled bindings",
            new TextBlock().Text("Name").FontSize(13).Foreground("#9CA3AF"),
            nameBox,
            greeting,
            new TextBlock().Text("Slider value").FontSize(13).Foreground("#9CA3AF"),
            slider,
            sliderValue,
            new TextBlock().Text("Selected date").FontSize(13).Foreground("#9CA3AF"),
            datePicker);
    }

    private Control BuildCollectionCard()
    {
        var list = new ListBox()
            .ItemsSource(_viewModel, x => x.Items)
            .SelectedItem(_viewModel, x => x.SelectedItem!, BindingMode.TwoWay)
            .Height(170)
            .Padding(6)
            .CornerRadius(8)
            .BorderBrush("#334155")
            .BorderThickness(1)
            .Background("#0F172A");

        var selected = new TextBlock()
            .Text(_viewModel, x => x.SelectedItem!)
            .FontSize(13)
            .Foreground("#6EE7B7");

        return CreateCard(
            "Collections — ItemsSource + SelectedItem",
            list,
            selected);
    }

    private Control BuildStyleCard()
    {
        var styledButton = new Button()
            .Content("Styled through Style<Button>")
            .Classes("accent")
            .HorizontalAlignment(HorizontalAlignment.Left);

        var accentBrush = this.FindResource<IBrush>("AccentBrush");

        return CreateCard(
            "Styles & resources — typed setters and DynamicResource",
            new TextBlock()
                .Text("The accent style uses DynamicResource for both brush properties, and the card shows a direct typed resource lookup.")
                .FontSize(13)
                .Foreground("#9CA3AF")
                .TextWrapping(TextWrapping.Wrap),
            new TextBlock()
                .Text($"FindResource<IBrush>: {accentBrush}")
                .FontSize(12)
                .Foreground("#6EE7B7"),
            styledButton);
    }

    private Control BuildAttachedPropertiesCard()
    {
        var matrix = new Grid()
            .Cols("*,*,*")
            .Rows("Auto,Auto")
            .Children(
                Cell("0,0").Grid(row: 0, column: 0),
                Cell("0,1").GridRow(0).GridColumn(1),
                Cell("0,2").GridRow(0).GridColumn(2),
                Cell("1,0").GridRow(1).GridColumn(0),
                Cell("1,1").GridRow(1).GridColumn(1),
                Cell("1,2").GridRow(1).GridColumn(2))
            .Margin(0);

        var canvas = new Canvas()
            .Height(120)
            .Background("#0F172A")
            .Children(
                new Border()
                    .Child(new TextBlock().Text("Canvas.Left = 18").FontSize(12).Foreground("#E5E7EB"))
                    .Background("#1D4ED8")
                    .CornerRadius(6)
                    .Padding(8)
                    .CanvasLeft(18)
                    .CanvasTop(14),
                new Border()
                    .Child(new TextBlock().Text("Canvas.Left = 150").FontSize(12).Foreground("#E5E7EB"))
                    .Background("#047857")
                    .CornerRadius(6)
                    .Padding(8)
                    .CanvasLeft(150)
                    .CanvasTop(58));

        return CreateCard(
            "Attached properties — Grid.Row/Column, Canvas.Left/Top",
            new TextBlock()
                .Text("Attached property methods are generated next to regular properties.")
                .FontSize(13)
                .Foreground("#9CA3AF"),
            matrix,
            canvas);
    }

    private Control BuildDockCard()
    {
        var dock = new DockPanel()
            .LastChildFill(true)
            .Height(170)
            .Background("#0F172A")
            .Children(
                new Border()
                    .Child(new TextBlock().Text("Dock.Top").FontSize(12).Foreground("#F8FAFC"))
                    .Background("#1E3A8A")
                    .Padding(8)
                    .DockPanelDock(Dock.Top),
                new Border()
                    .Child(new TextBlock().Text("Dock.Left").FontSize(12).Foreground("#F8FAFC"))
                    .Background("#065F46")
                    .Padding(8)
                    .DockPanelDock(Dock.Left),
                new Border()
                    .Child(new TextBlock().Text("Dock.Bottom").FontSize(12).Foreground("#F8FAFC"))
                    .Background("#7C2D12")
                    .Padding(8)
                    .DockPanelDock(Dock.Bottom),
                new Border()
                    .Child(new TextBlock().Text("Fill").FontSize(12).Foreground("#0F172A"))
                    .Background("#FACC15")
                    .Padding(8));

        return CreateCard(
            "DockPanel — attached Dock property + multiple children",
            dock);
    }

    private Control BuildConvertersCard()
    {
        // No converter is needed here: the generated expression binding converts
        // int -> string automatically.
        var counter = new TextBlock()
            .Text(_viewModel, x => x.Counter)
            .FontSize(32)
            .FontWeight(FontWeight.Bold)
            .Foreground(
                _viewModel,
                x => x.Counter,
                converter: new FuncValueConverter<int, IBrush>(
                    value => value > 0 ? Brushes.LimeGreen : value == 0 ? Brushes.Gray : Brushes.Red));

        var sliderText = new TextBlock()
            .Text(
                _viewModel,
                x => x.SliderValue,
                converter: new FuncValueConverter<double, string>(value => $"{value:F1}%"))
            .FontSize(14)
            .Foreground("#6EE7B7");

        var statusText = new TextBlock()
            .Text(
                _viewModel,
                x => x.IsEnabled,
                converter: new FuncValueConverter<bool, string>(value => value ? "Enabled" : "Disabled"))
            .FontSize(14)
            .Foreground("#BFDBFE");

        var upperName = new TextBlock()
            .Text(_viewModel, x => x.UserName, converter: new StringToUpperConverter())
            .FontSize(14)
            .Foreground("#FDE68A");

        return CreateCard(
            "Converters — automatic + FuncValueConverter",
            new TextBlock().Text("Counter — automatic int → string conversion").FontSize(13).Foreground("#9CA3AF"),
            counter,
            new TextBlock().Text("Counter color — FuncValueConverter<int, IBrush>").FontSize(13).Foreground("#9CA3AF"),
            new TextBlock().Text("Slider — FuncValueConverter<double, string>").FontSize(13).Foreground("#9CA3AF"),
            sliderText,
            new TextBlock().Text("IsEnabled — FuncValueConverter<bool, string>").FontSize(13).Foreground("#9CA3AF"),
            statusText,
            new TextBlock().Text("UserName — reusable StringToUpperConverter").FontSize(13).Foreground("#9CA3AF"),
            upperName);
    }

    private Control BuildVisibilityCard()
    {
        var toggle = new Button()
            .Content("Toggle bound panel")
            .Padding(14, 8)
            .CornerRadius(7)
            .Background("#7C3AED")
            .Foreground("White")
            .HorizontalAlignment(HorizontalAlignment.Left)
            .OnClick(() => _viewModel.IsEnabled = !_viewModel.IsEnabled);

        var boundPanel = new Border()
            .Child(
                new TextBlock()
                    .Text("IsVisible is bound to MainViewModel.IsEnabled")
                    .FontSize(13)
                    .Foreground("#BFDBFE"))
            .Background("#172554")
            .BorderBrush("#1D4ED8")
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(12)
            .IsVisible(_viewModel, x => x.IsEnabled, BindingMode.OneWay);

        return CreateCard(
            "Conditional UI — IsVisible binding",
            toggle,
            boundPanel);
    }

    private static Button Cell(string text)
    {
        return new Button()
            .Content(text)
            .Padding(10, 8)
            .CornerRadius(6)
            .Background("#1F2937")
            .Foreground("#E5E7EB")
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    private static Border CreateCard(string title, params Control[] children)
    {
        var panel = new StackPanel()
            .Children(
                new TextBlock()
                    .Text(title)
                    .FontSize(19)
                    .FontWeight(FontWeight.SemiBold)
                    .Foreground("#E5E7EB"))
            .Children(children)
            .Spacing(12);

        return new Border()
            .Background("#151D27")
            .BorderBrush("#263445")
            .BorderThickness(1)
            .CornerRadius(12)
            .Padding(16)
            .Child(panel)
            .Margin(0, 0, 0, 16);
    }
}
