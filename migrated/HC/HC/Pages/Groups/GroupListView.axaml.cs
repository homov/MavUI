using Avalonia.Controls;
using Hc.Models.Cloud;
using Hc.Pages.Models;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>The user's groups ("locations") as a list. Loads on open and on <see cref="ReloadAsync"/>.</summary>
public partial class GroupListView : UserControl
{
    public GroupListView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await ReloadAsync();

    /// <summary>Loads the groups again. A failure is shown in the banner.</summary>
    public async Task ReloadAsync()
    {
        Progress.IsVisible = true;
        ErrorBanner.Dismiss();
        try
        {
            var groups = await AppServices.Default.Locations.GetGroupsAsync();
            Render(groups);
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

    private void Render(IReadOnlyList<UserGroupInfo> groups)
    {
        ListHost.Children.Clear();
        EmptyState.IsVisible = groups.Count == 0;
        if (groups.Count == 0)
            return;

        var list = new MavList { ShowDividers = true };
        foreach (var group in groups)
            list.AddItem(new MavListItem(group.Name, null, MavIcons.Groups));

        ListHost.Children.Add(new MavCard(MavCardVariant.Outlined, list));
    }
}
