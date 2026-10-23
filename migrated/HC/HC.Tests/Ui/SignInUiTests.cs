using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Hc.Pages.Account;
using Hc.Pages.Home;
using Hc.Pages.Shell;
using Mav.Controls;
using Mav.Primitives;
using Xunit;

namespace Hc.Tests.Ui;

/// <summary>The sign-in flow through the real screens, against the local stub cloud.</summary>
[Collection("HC UI")]
public class SignInUiTests
{
    [AvaloniaFact]
    public void Wrong_account_stays_on_sign_in_and_shows_the_error()
    {
        var window = Launch();
        try
        {
            WaitFor(() => Find<LoginPage>(window) is not null, "the sign-in page");
            Type(window, "EmailField", "stranger@example.test");
            Type(window, "PasswordField", "secret");

            Activate(window, "SignInButton");
            WaitFor(() => Find<MavBanner>(window, "ErrorBanner") is { IsVisible: true }, "the error banner");

            var banner = Find<MavBanner>(window, "ErrorBanner")!;
            ScreenDump.Write(window, "sign-in-error");
            Assert.False(string.IsNullOrWhiteSpace(banner.Message));
            Assert.NotNull(Find<LoginPage>(window));
            Assert.Null(Find<HomePage>(window));
            Assert.True(StubCloud.Instance.SignInWasSent("stranger@example.test"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Valid_account_opens_home()
    {
        var window = Launch();
        try
        {
            WaitFor(() => Find<LoginPage>(window) is not null, "the sign-in page");
            Type(window, "EmailField", StubCloud.KnownEmail);
            Type(window, "PasswordField", "secret");

            Activate(window, "SignInButton");
            WaitFor(() => Find<HomePage>(window) is not null, "the home page");

            ScreenDump.Write(window, "home");
            Assert.Null(Find<LoginPage>(window));
            Assert.True(StubCloud.Instance.SignInWasSent(StubCloud.KnownEmail));
        }
        finally
        {
            window.Close();
        }
    }

    private static Window Launch()
    {
        var window = new Window { Width = 390, Height = 844, Content = new MainView() };
        window.Show();
        return window;
    }

    private static T? Find<T>(Window window, string? name = null) where T : Control =>
        window.GetVisualDescendants().OfType<T>().FirstOrDefault(c => name is null || c.Name == name);

    private static void Type(Window window, string fieldName, string value)
    {
        var field = Find<MavTextField>(window, fieldName)
                    ?? throw new InvalidOperationException($"Field {fieldName} is not on screen.");
        field.Text = value;
    }

    /// <summary>Presses the button the way a keyboard user does: focus, then Enter.</summary>
    private static void Activate(Window window, string buttonName)
    {
        var button = Find<MavButton>(window, buttonName)
                     ?? throw new InvalidOperationException($"Button {buttonName} is not on screen.");
        var surface = button.GetVisualDescendants().OfType<MavInkSurface>().First();
        surface.Focus();
        surface.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
    }

    /// <summary>Runs the UI loop until the condition holds, so cloud answers and navigation can arrive.</summary>
    private static void WaitFor(Func<bool> condition, string what)
    {
        for (var i = 0; i < 500; i++)
        {
            if (condition())
                return;
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
        Assert.Fail($"Timed out waiting for {what}.");
    }
}
