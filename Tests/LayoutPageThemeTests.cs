using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Mav.ControlsGallery.Pages;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class LayoutPageThemeTests
{
    [AvaloniaFact]
    public void Open_layout_surface_and_inset_examples_follow_theme_changes()
    {
        var previousSeed = MavTheme.SeedColor;
        var previousMode = MavTheme.Mode;
        var previousAccent = MavTheme.UseSystemAccentColor;
        Window? window = null;

        try
        {
            MavTheme.Configure(seed: MavColors.Indigo, mode: MavThemeMode.Light);
            var page = new LayoutPage();
            window = new Window { Width = 1200, Height = 900, Content = page };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var surface = Find<MavSurface>(page, "DemoSurface");
            var safeArea = Find<MavSafeArea>(page, "DemoSafeArea");
            var keyboardHost = Find<MavKeyboardAvoidingHost>(page, "DemoKeyboardHost");
            AssertThemeColors(surface, safeArea, keyboardHost);
            var lightSurface = surface.FillColor;
            var lightInset = BrushColor(safeArea.Background);

            // Change the theme without constructing or navigating back to the page.
            MavTheme.UseDark();
            Dispatcher.UIThread.RunJobs();
            Assert.Same(surface, Find<MavSurface>(page, "DemoSurface"));
            Assert.NotEqual(lightSurface, surface.FillColor);
            Assert.NotEqual(lightInset, BrushColor(safeArea.Background));
            AssertThemeColors(surface, safeArea, keyboardHost);

            // Both the mode and seed update resources on the same already-open controls.
            MavTheme.Configure(seed: MavColors.Blue);
            Dispatcher.UIThread.RunJobs();
            AssertThemeColors(surface, safeArea, keyboardHost);

            MavTheme.UseLight();
            Dispatcher.UIThread.RunJobs();
            AssertThemeColors(surface, safeArea, keyboardHost);
        }
        finally
        {
            window?.Close();
            MavTheme.Configure(seed: previousSeed, mode: previousMode, useSystemAccentColor: previousAccent);
        }
    }

    private static T Find<T>(LayoutPage page, string name) where T : Control =>
        Assert.Single(page.GetVisualDescendants().OfType<T>().Where(control => control.Name == name));

    private static void AssertThemeColors(MavSurface surface, MavSafeArea safeArea, MavKeyboardAvoidingHost keyboardHost)
    {
        var high = MavTheme.ColorOf(MavColorRole.SurfaceContainerHigh);
        var low = MavTheme.ColorOf(MavColorRole.SurfaceContainerLow);
        Assert.Equal(high, surface.FillColor);
        Assert.Equal(high, BrushColor(surface.Background));
        Assert.Equal(low, BrushColor(safeArea.Background));
        Assert.Equal(low, BrushColor(keyboardHost.Background));
    }

    private static Color BrushColor(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
