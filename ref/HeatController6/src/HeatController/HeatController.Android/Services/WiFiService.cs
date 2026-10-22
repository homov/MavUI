using Android.Content;
using HeatController.Interface;
using Xamarin.Forms;

[assembly: Dependency(typeof(HeatController.Droid.Services.WiFiService))]

namespace HeatController.Droid.Services
{
    public class WiFiService : IWiFiService
    {
        public void OpenSettings()
        {
            var intent = new Intent(Android.Provider.Settings.ActionWifiSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
    }
}