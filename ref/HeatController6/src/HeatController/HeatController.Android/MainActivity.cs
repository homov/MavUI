using System;
using System.Net.Http;
using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using Autofac;
using FFImageLoading;
using FFImageLoading.Forms.Platform;
using HeatController.Droid.Services;
using HeatController.Interface;
using HeatController.Services.WifiManager;

namespace HeatController.Droid
{
    [Activity(
        Label = "HeatController", 
        Icon = "@mipmap/icon", 
        Theme = "@style/MainTheme", 
        MainLauncher = false,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {

        public class OnApplyWindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public Func<Android.Views.View, WindowInsetsCompat, WindowInsetsCompat> OnApplyWindowInsetsCallback;
            public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
            {
                return OnApplyWindowInsetsCallback?.Invoke(v, insets);
            }
        }



        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            // Убираем флаги полноэкранного режима (если были)
            Window.ClearFlags(WindowManagerFlags.Fullscreen);
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
            //// опционально: задаём цвет статус-бара (под цвет тулбара)

            // Применяем WindowInsets: добавляем верхний padding к корневому view,
            //чтобы контент Forms не рисовался под статус - баром.
            var root = FindViewById(Android.Resource.Id.Content);
            ViewCompat.SetOnApplyWindowInsetsListener(root, new OnApplyWindowInsetsListener()
            {
                OnApplyWindowInsetsCallback = (v, insets) =>
                {
                    // Получаем системные отступы (status + nav)
                    var sysInsets = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
                    // Устанавливаем верхний padding равный высоте status bar
                    //v.SetPadding(v.PaddingLeft, sysInsets.Top, v.PaddingRight, v.PaddingBottom);
                    v.SetPadding(v.PaddingLeft, sysInsets.Top, v.PaddingRight, sysInsets.Bottom);
                    return insets;
                }
            });
            ViewCompat.RequestApplyInsets(root);
            ApplySystemStatusBarAppearance();   
            

            var config = new FFImageLoading.Config.Configuration()
            {
                VerboseLogging = false,
                VerbosePerformanceLogging = false,
                VerboseMemoryCacheLogging = false,
                VerboseLoadingCancelledLogging = false,
                HttpClient = GetClient()
            };
            ImageService.Instance.Initialize(config);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            CachedImageRenderer.Init(true);
            CachedImageRenderer.InitImageViewHandler();

            HeatController.ContainerService.Builder.RegisterType<WifiNetworkService>().As<IWifiNetworkService>().SingleInstance();
            
            LoadApplication(new App());
        }
        
        void ApplySystemStatusBarAppearance()
        {
            // Определяем текущий режим (ночь/день)
            bool isNight = (Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

            // Читаем цвет поверхности из темы (colorSurface) — этот ресурс будет автоматически
            // разрешён из values/ или values-night/ в зависимости от режима
            var tv = new TypedValue();
            int resolvedColor;
            if (Theme.ResolveAttribute(Resource.Attribute.colorSurface, tv, true))
            {
                if (tv.ResourceId != 0)
                    resolvedColor = ContextCompat.GetColor(this, tv.ResourceId);
                else
                    resolvedColor = tv.Data;
            }
            else
            {
                // fallback, если атрибут не найден
                resolvedColor = ContextCompat.GetColor(this, Resource.Color.colorPrimaryDark);
            }

            // Применяем цвет статус-бара (только на API >= 21)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window.SetStatusBarColor(new Color(resolvedColor));
            }

            // Настраиваем, какие иконки нужны: если не ночь => тёмные иконки (AppearanceLightStatusBars = true)
            var decorView = Window.DecorView;
            var controller = WindowCompat.GetInsetsController(Window, decorView);
            if (controller != null)
            {
                controller.AppearanceLightStatusBars = !isNight;
            }
            else
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    if (!isNight)
                        decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LightStatusBar;
                    else
                        decorView.SystemUiVisibility &= ~(StatusBarVisibility)SystemUiFlags.LightStatusBar;
                }
            }
        }
        
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private HttpClient GetClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            handler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
            return client;
        }
    }
}