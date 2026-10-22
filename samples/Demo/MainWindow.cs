using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Platform;

namespace Md3.Demo;

public sealed class MainWindow : MdWindow
{
    public MainWindow()
    {
        Title = "Md3 Navigation Demo";
        Width = 1180;
        Height = 800;
        MinWidth = 720;
        MinHeight = 520;
        if (TryParseWindowSize(Environment.GetEnvironmentVariable("MD3_WINDOW"), out var windowSize))
        {
            Width = windowSize.Width;
            Height = windowSize.Height;
            MinWidth = Math.Min(MinWidth, windowSize.Width);
            MinHeight = Math.Min(MinHeight, windowSize.Height);
        }
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var navigation = new MdNavigationPage(new HomePage());
        if (TryParseSafeArea(Environment.GetEnvironmentVariable("MD3_SAFE_AREA"), out var insets))
            navigation.SafeAreaOverride = insets;
        if (double.TryParse(Environment.GetEnvironmentVariable("MD3_KEYBOARD"), out var keyboardHeight) && keyboardHeight > 0)
            navigation.KeyboardHost.SimulatedKeyboardHeight = keyboardHeight;
        Content = navigation;

        if (Environment.GetEnvironmentVariable("MD3_SMOKE") == "1")
            RunSmokeTest(navigation);
    }

    /// <summary>
    /// MD3_WINDOW=400x800 previews a phone-sized window on the desktop;
    /// a short height such as 640x360 previews landscape phone chrome.
    /// </summary>
    private static bool TryParseWindowSize(string? value, out Size size)
    {
        size = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !double.TryParse(parts[0], out var width) ||
            !double.TryParse(parts[1], out var height) ||
            width < 320 || height < 320)
            return false;

        size = new Size(width, height);
        return true;
    }

    /// <summary>
    /// MD3_SAFE_AREA=top,bottom or left,top,right,bottom simulates phone insets
    /// on the desktop so the mobile chrome can be checked without an emulator.
    /// </summary>
    private static bool TryParseSafeArea(string? value, out Thickness insets)
    {
        insets = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && double.TryParse(parts[0], out var uniform))
        {
            insets = new Thickness(0, uniform, 0, uniform);
            return true;
        }

        if (parts.Length == 2 &&
            double.TryParse(parts[0], out var top) &&
            double.TryParse(parts[1], out var bottom))
        {
            insets = new Thickness(0, top, 0, bottom);
            return true;
        }

        if (parts.Length == 4 &&
            double.TryParse(parts[0], out var left) &&
            double.TryParse(parts[1], out var top4) &&
            double.TryParse(parts[2], out var right) &&
            double.TryParse(parts[3], out var bottom4))
        {
            insets = new Thickness(left, top4, right, bottom4);
            return true;
        }

        return false;
    }

    private void RunSmokeTest(MdNavigationPage navigation)
    {
        DispatcherTimer.RunOnce(() =>
        {
            navigation.Push(new ControlsPage(), animated: false);
            Console.WriteLine($"push controls: depth={navigation.StackDepth}, canGoBack={navigation.CanGoBack}, command={navigation.GoBackCommand.CanExecute(null)}");
        }, TimeSpan.FromMilliseconds(300));

        DispatcherTimer.RunOnce(() =>
        {
            var dialog = new MdDialog
            {
                Title = "Smoke dialog",
                Message = "Back should close this overlay before popping the page.",
                ConfirmText = "OK",
            };
            dialog.Show(navigation.CurrentPage);
            Console.WriteLine($"dialog open: overlay={MdOverlay.HasActiveOverlay}, depth={navigation.StackDepth}");
        }, TimeSpan.FromMilliseconds(600));

        DispatcherTimer.RunOnce(() =>
        {
            navigation.TryGoBack();
            Console.WriteLine($"back closes overlay: overlay={MdOverlay.HasActiveOverlay}, depth={navigation.StackDepth}");
        }, TimeSpan.FromMilliseconds(900));

        DispatcherTimer.RunOnce(() =>
        {
            var dialog = new MdDialog
            {
                Title = "Page-owned dialog",
                Message = "This dialog should close when the page is pushed away.",
                ConfirmText = "OK",
            };
            dialog.Show(navigation.CurrentPage);
            Console.WriteLine($"dialog for page leave: overlay={MdOverlay.HasActiveOverlay}");
        }, TimeSpan.FromMilliseconds(1200));

        DispatcherTimer.RunOnce(() =>
        {
            navigation.Push(new InboxPage(), animated: false);
            Console.WriteLine($"push inbox: depth={navigation.StackDepth}, title={navigation.CurrentPage?.Title}, overlay={MdOverlay.HasActiveOverlay}");
        }, TimeSpan.FromMilliseconds(1500));

        DispatcherTimer.RunOnce(() =>
        {
            navigation.Pop(animated: false);
            Console.WriteLine($"pop: depth={navigation.StackDepth}, title={navigation.CurrentPage?.Title}");
        }, TimeSpan.FromMilliseconds(1800));

        DispatcherTimer.RunOnce(() =>
        {
            navigation.PopToRoot(animated: false);
            Console.WriteLine($"pop to root: depth={navigation.StackDepth}, title={navigation.CurrentPage?.Title}, command={navigation.GoBackCommand.CanExecute(null)}");
            Console.WriteLine("MD3_SMOKE_OK");
            Close();
        }, TimeSpan.FromMilliseconds(2100));
    }
}
