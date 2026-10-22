using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HeatController
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LocationListView : ContentView
    {
        public LocationListViewModel ViewModel { get;}
        public LocationListView()
        {
            InitializeComponent();
            this.BindingContext = ViewModel = ContainerService.Container.Resolve<LocationListViewModel>();
        }
    }
}