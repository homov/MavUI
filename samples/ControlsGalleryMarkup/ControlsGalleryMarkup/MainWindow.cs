using Avalonia.Controls;
using Mav.Controls;
using Mav.ControlsGalleryMarkup.Pages;

namespace Mav.ControlsGalleryMarkup;

/// <summary>Window with a navigation column of MavUI buttons and a page area, all built in C# markup.</summary>
public sealed class MainWindow : MavWindow
{
    private readonly Border _page;

    public MainWindow()
    {
        Title = "ControlsGallery (markup)";
        Width = 1000;
        Height = 700;

        _page = new Border().Padding(24);

        Content = new Grid()
            .Cols("220, *")
            .Children(
                new StackPanel()
                    .Spacing(8)
                    .Children(
                        new MavButton("Overview", MavButtonVariant.Text)
                            .OnClicked(() => Show(OverviewPage.Build())),
                        new MavButton("Inputs", MavButtonVariant.Text)
                            .OnClicked(() => Show(InputsPage.Build())),
                        new MavButton("Actions", MavButtonVariant.Text)
                            .OnClicked(() => Show(ActionsPage.Build()))),
                _page.GridColumn(1));

        Show(OverviewPage.Build());
    }

    private void Show(Control page) => _page.Child = page;
}
