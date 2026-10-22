using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Desktop window host with a built-in template. Use it instead of relying on an
/// Avalonia theme for the window chrome. The window paints the theme surface behind
/// its content, so the desktop never shows through; a <see cref="Window.Background"/>
/// set by the caller is kept. The template still includes the visual layer manager
/// required by overlays, popups and the Mav overlay host.
/// </summary>
public class MavWindow : Window
{
    private IBrush? _themeBackground;

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

    /// <summary>Creates a window with the library chrome and theme.</summary>
    public MavWindow()
    {
        // Runs now and on every theme change. The theme brush is replaced only while the window
        // still uses it, so a Background chosen by the caller survives theme switches.
        MavTheme.Observe(this, (self, theme) =>
        {
            var brush = MavTheme.BrushOf(theme.Color.Surface);
            var userSet = self.IsSet(BackgroundProperty) && !ReferenceEquals(self.Background, self._themeBackground);
            if (!userSet)
                self.Background = brush;
            self._themeBackground = brush;
        });
        Template = DefaultTemplate;
    }
}
