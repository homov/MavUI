using Avalonia.Controls;
using Avalonia.Interactivity;
using Hc.Models.Cloud;
using Hc.Pages.Models;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>
/// The user's groups as a list: name and schedule type. The group only holds a schedule id, so the type is
/// loaded per group. Pull down to refresh (<see cref="MavRefreshIndicator"/>).
/// </summary>
public partial class GroupListView : UserControl
{
    public GroupListView()
    {
        InitializeComponent();
        Refresh.RefreshHandler = ReloadAsync;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e) => await ReloadAsync();

    /// <summary>Loads the groups and their schedule types again. A failure is shown in the banner.</summary>
    public async Task ReloadAsync()
    {
        Progress.IsVisible = true;
        ErrorBanner.Dismiss();
        try
        {
            var groups = await AppServices.Default.Locations.GetGroupsAsync();
            var rows = new List<(string Name, string Schedule)>(groups.Count);
            foreach (var group in groups)
                rows.Add((group.Name, await LoadScheduleNameAsync(group)));

            Render(rows);
        }
        catch (OperationCanceledException)
        {
            // The view was closed during the request.
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

    private static async Task<string> LoadScheduleNameAsync(UserGroupInfo group)
    {
        if (string.IsNullOrEmpty(group.Schedule))
            return ScheduleTypeNames.Localize(null);

        var schedule = await AppServices.Default.Schedules.GetAsync(group.Schedule);
        return ScheduleTypeNames.Localize(schedule?.ScheduleType);
    }

    private void Render(IReadOnlyList<(string Name, string Schedule)> rows)
    {
        ListHost.Children.Clear();
        EmptyState.IsVisible = rows.Count == 0;
        if (rows.Count == 0)
            return;

        var list = new MavList { ShowDividers = true };
        foreach (var (name, schedule) in rows)
            list.AddItem(new MavListItem(name, schedule, MavIcons.Groups));

        ListHost.Children.Add(new MavCard(MavCardVariant.Outlined, list));
    }
}
