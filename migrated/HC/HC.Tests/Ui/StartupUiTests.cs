using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Hc.Pages.Account;
using Hc.Pages.Home;
using Hc.Pages.Shell;
using Xunit;

namespace Hc.Tests.Ui;

/// <summary>Runs the real shell in a headless window: checks what the user sees after launch.</summary>
[Collection("HC UI")]
public class StartupUiTests
{
    [AvaloniaFact]
    public void Launch_shows_sign_in_or_home()
    {
        var window = new Window { Width = 390, Height = 844, Content = new MainView() };
        try
        {
            window.Show();
            Settle();

            var report = ScreenDump.Write(window, "startup");
            Assert.True(File.Exists(report));

            var pages = window.GetVisualDescendants().ToList();
            Assert.True(
                pages.Any(v => v is StartPage || v is LoginPage || v is HomePage),
                "After launch the shell shows neither the start page, the sign-in page nor the home page.");
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Lets layout, navigation and pending dispatcher work finish before the screen is read.</summary>
    private static void Settle()
    {
        for (var i = 0; i < 100; i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
    }
}
