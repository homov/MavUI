namespace HeatController;

//[XamlCompilation(XamlCompilationOptions.Compile)]
//public partial class AddDevicePage : BasePage
//{
//    AddDevicePageModel ViewModel => (AddDevicePageModel)BindingContext;
//    public AddDevicePage()
//    {
//        this.BindingContext = new AddDevicePageModel();
//        InitializeComponent();
//    }

//    protected override void OnAppearing()
//    {
//        ViewModel.Start();
//    }
//    protected override void OnDisappearing()
//    {
//        try
//        {
//            ViewModel.CancelAll();
//        }
//        catch (Exception)
//        {
//            ;
//        }
//    }
//}