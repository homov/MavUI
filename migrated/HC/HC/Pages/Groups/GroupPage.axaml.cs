using Avalonia.Interactivity;
using Hc.Models.Cloud;
using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>A group's page: its schedule, edited with <see cref="ScheduleEditorView"/>. The whole schedule is saved at once.</summary>
public partial class GroupPage : MavContentPage
{
    public GroupPage()
    {
        InitializeComponent();

        SaveButton.Clicked += OnSave;
        Loaded += OnLoaded;
    }

    /// <summary>The group whose schedule is shown. Set before the page is shown.</summary>
    public required UserGroupInfo Group { get; init; }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Title = Group.Name;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Progress.IsVisible = true;
        ErrorBanner.Dismiss();
        try
        {
            var scheduleId = Group.Schedule;
            var info = string.IsNullOrEmpty(scheduleId) ? null : await AppServices.Default.Schedules.GetAsync(scheduleId);
            Editor.Show(ScheduleDraft.From(info));
        }
        catch (OperationCanceledException)
        {
            // The page was closed during the request.
        }
        catch (Exception ex)
        {
            ErrorBanner.Message = ErrorText.From(ex);
            ErrorBanner.Show();
        }
        finally
        {
            Progress.IsVisible = false;
        }
    }

    private async void OnSave(object? sender, EventArgs e) =>
        await PageRunner.RunAsync(SaveButton, ErrorBanner, async () =>
        {
            await AppServices.Default.Schedules.SaveAsync(Editor.Draft.ToInfo(Group.Schedule));
        });
}
