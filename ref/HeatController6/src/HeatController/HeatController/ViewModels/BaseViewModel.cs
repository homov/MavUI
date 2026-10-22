
namespace HeatController;

public partial class BaseViewModel: ObservableObject
{
    public class BusyState: IDisposable
    {
        BaseViewModel model;
        public BusyState(BaseViewModel model)
        {
            this.model = model;
            this.model.Busy(true);
        }

        public void Dispose()
        {
            this.model.Busy(false);
        }
    }

    protected readonly MessageBoxService messageBoxService = ContainerService.Container.Resolve<MessageBoxService>();
    protected readonly IMainThread mainThread = ContainerService.Container.Resolve<IMainThread>();
    protected readonly NavigationService navigationService = ContainerService.Container.Resolve<NavigationService>();

    [ObservableProperty]
    bool isBusy;

    [ObservableProperty] private bool isReady = true;

    public BusyState Busy()
    {
        return new BusyState(this);
    }

    public void Busy(bool state)
    {
        mainThread.InvokeOnMainThreadAsync(()=>IsBusy = state);
    }
}
