namespace HeatController;

public partial class BaseSelectionViewModel: BaseViewModel
{
    private readonly Func<object, Task> selectedCallback;

    public BaseSelectionViewModel( Func<object, Task> selectedCallback )
	{
		this.selectedCallback = selectedCallback;
	}


	public virtual object GetSelection(object parameter)
	{
		return parameter;
	}

	[ObservableProperty]
	string title;
	
	[RelayCommand]
	async Task Select(object parameter)
	{
		await selectedCallback?.Invoke(GetSelection(parameter));
		await navigationService.PopAsync();
	}

    [RelayCommand]
    async Task Back()
	{
        await navigationService.PopAsync();
    }

}
