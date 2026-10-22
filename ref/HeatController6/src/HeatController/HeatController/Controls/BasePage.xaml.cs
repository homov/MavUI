using System.ComponentModel;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Markup;
using Xamarin.CommunityToolkit.ObjectModel;

namespace HeatController
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [DefaultProperty(nameof(Body))]
    public partial class BasePage : ContentPage
    {
        public AsyncCommand DefaultBackCommand { get; }  
        public AsyncCommand DefaultMenuCommand { get; }  
        
        async Task ExecuteBack()
        {
            if (this.BackCommand != null)
                this.BackCommand.Execute(null);
            else
            {
                if (Application.Current != null && Application.Current.MainPage != null)
                    await Application.Current.MainPage.Navigation.PopAsync();
            }
        }

        async Task ExecuteMenu()
        {
            if (this.MenuCommand != null)
                this.MenuCommand.Execute(this.MenuCommandParameter);
        }        public BasePage()
        {
            DefaultBackCommand = new AsyncCommand(ExecuteBack);
            DefaultMenuCommand = new AsyncCommand(ExecuteMenu);
            InitializeComponent();
        }

        public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(BasePage), null, propertyChanged: OnBackCommandPropertyChanged);
        public static readonly BindableProperty MenuCommandProperty = BindableProperty.Create(nameof(MenuCommand), typeof(ICommand), typeof(BasePage), null, propertyChanged: OnMenuCommandPropertyChanged);
        public static readonly BindableProperty MenuCommandParameterProperty = BindableProperty.Create(nameof(MenuCommandParameter), typeof(object), typeof(BasePage), null);

        public static readonly BindableProperty BackIconProperty = BindableProperty.Create(nameof(BackIcon), typeof(string), typeof(BasePage), Icons.ArrowLeft);
        public static readonly BindableProperty MenuIconProperty = BindableProperty.Create(nameof(MenuIcon), typeof(string), typeof(BasePage), Icons.Menu);
        public static readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(nameof(HeaderBackgroundColor), typeof(Color), typeof(BasePage), Color.White);
        public static readonly BindableProperty HeaderTextColorProperty = BindableProperty.Create(nameof(HeaderTextColor), typeof(Color), typeof(BasePage), Color.Black);
        public static readonly BindableProperty HeaderFontFamilyProperty = BindableProperty.Create(nameof(HeaderFontFamily), typeof(string), typeof(BasePage), null);
        public static readonly BindableProperty HeaderFontSizeProperty = BindableProperty.Create(nameof(HeaderFontSize), typeof(double), typeof(BasePage), 14.0);
        public static readonly BindableProperty SeparatorColorProperty = BindableProperty.Create(nameof(SeparatorColor), typeof(Color), typeof(BasePage), Color.Gray);
        public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(BasePage), null);
        public static readonly BindableProperty BodyBackgroundColorProperty = BindableProperty.Create(nameof(BodyBackgroundColor), typeof(Color), typeof(BasePage), Color.White);
        public static readonly BindableProperty BodyPaddingProperty = BindableProperty.Create(nameof(BodyPadding), typeof(Thickness), typeof(BasePage), BasePage.PaddingProperty.DefaultValue);
        public static readonly BindableProperty HeaderPaddingProperty = BindableProperty.Create(nameof(HeaderPadding), typeof(Thickness), typeof(BasePage), BasePage.PaddingProperty.DefaultValue);
        public static readonly BindableProperty ShowHeaderProperty = BindableProperty.Create(nameof(ShowHeader), typeof(bool), typeof(BasePage), true);
        public static readonly BindableProperty BackIconVisibleProperty = BindableProperty.Create(nameof(BackIconVisible), typeof(bool), typeof(BasePage), true);
        public static readonly BindableProperty MenuIconVisibleProperty = BindableProperty.Create(nameof(MenuIconVisible), typeof(bool), typeof(BasePage), false);
        //public static readonly BindableProperty IsBusyPropery = BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(BasePage), false);

        private static void OnBackCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BasePage basePage)
            {
            }
        }

        private static void OnMenuCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BasePage basePage)
            {
            }
        }

        public object MenuCommandParameter
        {
            get => GetValue(MenuCommandParameterProperty);
            set => SetValue(MenuCommandParameterProperty, value);
        }

        public double HeaderFontSize
        {
            get => (double)GetValue(HeaderFontSizeProperty);
            set => SetValue(HeaderFontSizeProperty, value);
        }


        public string HeaderFontFamily
        {
            get => (string)GetValue(HeaderFontFamilyProperty);
            set => SetValue(HeaderFontFamilyProperty, value);
        }


        public bool BackIconVisible
        {
            get => (bool)GetValue(BackIconVisibleProperty);
            set => SetValue(BackIconVisibleProperty, value);
        }

        public bool MenuIconVisible
        {
            get => (bool)GetValue(MenuIconVisibleProperty);
            set => SetValue(MenuIconVisibleProperty, value);
        }

        public bool ShowHeader
        {
            get => (bool)GetValue(ShowHeaderProperty);
            set => SetValue(ShowHeaderProperty, value);
        }

        public ICommand BackCommand
        {
            get => (ICommand)GetValue(BackCommandProperty);
            set => SetValue(BackCommandProperty, value);
        }

        public ICommand MenuCommand
        {
            get => (ICommand)GetValue(MenuCommandProperty);
            set => SetValue(MenuCommandProperty, value);
        }

        public Thickness HeaderPadding
        {
            get => (Thickness)GetValue(HeaderPaddingProperty);
            set => SetValue(HeaderPaddingProperty, value);
        }

        public Thickness BodyPadding
        {
            get => (Thickness)GetValue(BodyPaddingProperty);
            set => SetValue(BodyPaddingProperty, value);
        }

        public View Body
        {
            get => (View)GetValue(BodyProperty);
            set => SetValue(BodyProperty, value);
        }

        public string BackIcon
        {
            get => (string)GetValue(BackIconProperty);
            set => SetValue(BackIconProperty, value);
        }

        public string MenuIcon
        {
            get => (string)GetValue(MenuIconProperty);
            set => SetValue(MenuIconProperty, value);
        }

        public Color BodyBackgroundColor
        {
            get => (Color)GetValue(BodyBackgroundColorProperty);
            set => SetValue(BodyBackgroundColorProperty, value);
        }


        public Color HeaderBackgroundColor
        {
            get => (Color)GetValue(HeaderBackgroundColorProperty);
            set => SetValue(HeaderBackgroundColorProperty, value);
        }

        public Color HeaderTextColor
        {
            get => (Color)GetValue(HeaderTextColorProperty);
            set => SetValue(HeaderTextColorProperty, value);
        }

        public Color SeparatorColor
        {
            get => (Color)GetValue(SeparatorColorProperty);
            set => SetValue(SeparatorColorProperty, value);
        }
    }
}