using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Desktop window host with a built-in template. Use it instead of relying on an
/// Avalonia theme for the window chrome; the Material page itself provides the
/// visible surface. The template still includes the visual layer manager required
/// by overlays, popups and the Md3 overlay host.
/// </summary>
public class MdWindow : Window
{
    private static readonly IControlTemplate DefaultTemplate =
        new FuncControlTemplate<Window>((window, scope) =>
        {
            var root = new Panel();

            var background = new Border
            {
                IsHitTestVisible = false,
                [!BackgroundProperty] = new TemplateBinding(BackgroundProperty),
            };
            root.Children.Add(background);

            var presenter = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = new TemplateBinding(ContentProperty),
                [!ContentPresenter.ContentTemplateProperty] =
                    new TemplateBinding(ContentTemplateProperty),
                [!Layoutable.MarginProperty] = new TemplateBinding(PaddingProperty),
                [!Layoutable.HorizontalAlignmentProperty] =
                    new TemplateBinding(HorizontalContentAlignmentProperty),
                [!Layoutable.VerticalAlignmentProperty] =
                    new TemplateBinding(VerticalContentAlignmentProperty),
            };
            scope.Register("PART_ContentPresenter", presenter);

            var layers = new VisualLayerManager
            {
                Name = "PART_VisualLayerManager",
                Child = presenter,
            };
            scope.Register("PART_VisualLayerManager", layers);
            root.Children.Add(layers);

            return root;
        });

    public MdWindow()
    {
        Background = Brushes.Transparent;
        Template = DefaultTemplate;
    }
}
