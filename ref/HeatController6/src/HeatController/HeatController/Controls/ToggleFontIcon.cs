using System.Windows.Input;
using Xamarin.CommunityToolkit.Effects;

namespace HeatController;

public class ToggleFontIcon : Label
{
    ICommand TapCommand;

    private void Tap()
    {
        IsOn = !IsOn;
        if (IsOn)
            OnCommand?.Execute(GetValue(CommandTargetProperty));
        else
            OffCommand?.Execute(GetValue(CommandTargetProperty));
    }

    public ToggleFontIcon()
    {
        TapCommand = new Command(Tap);
        this.HorizontalOptions = LayoutOptions.Center;
        this.VerticalOptions = LayoutOptions.Center;
        this.FontFamily = Fonts.MaterialIconsRegular;
        TouchEffect.SetCommand(this, TapCommand);
        TouchEffect.SetNativeAnimation(this, true);
        this.PropertyChanged += ToggleFontIcon_PropertyChanged;
        this.Text = (string) ( (bool)IsOnProperty.DefaultValue ? OnIconProperty.DefaultValue : OffIconProperty.DefaultValue );
    }

    private void ToggleFontIcon_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnIcon))
        {
            if (this.IsOn) this.Text= OnIcon;
        }
        else if (e.PropertyName == nameof(OnColor))
        {
            if (this.IsOn) this.TextColor= OnColor;
        }
        else if (e.PropertyName == nameof(OffColor))
        {
            if (!this.IsOn) this.TextColor = OffColor;
        }
        else if (e.PropertyName == nameof(OffIcon))
        {
            if (!this.IsOn) this.Text = OffIcon;
        }
        else if (e.PropertyName == nameof(IsOn))
        {
            this.Text =  IsOn ? OnIcon : OffIcon;
            this.TextColor = IsOn ? OnColor : OffColor;
        }

    }

    public static readonly BindableProperty OnIconProperty   = BindableProperty.Create(nameof(OnIcon), typeof(string), typeof(ToggleFontIcon), Icons.ToggleSwitch);
    public static readonly BindableProperty OffIconProperty  = BindableProperty.Create(nameof(OffIcon), typeof(string), typeof(ToggleFontIcon), Icons.ToggleSwitchOff);
    public static readonly BindableProperty IsOnProperty = BindableProperty.Create(nameof(IsOn), typeof(bool), typeof(ToggleFontIcon), false, BindingMode.TwoWay);
    public static readonly BindableProperty CommandTargetProperty = BindableProperty.Create(nameof(CommandTarget), typeof(object), typeof(ToggleFontIcon), null);
    public static readonly BindableProperty OnCommandProperty = BindableProperty.Create(nameof(OnCommand), typeof(ICommand), typeof(ToggleFontIcon), null);
    public static readonly BindableProperty OffCommandProperty = BindableProperty.Create(nameof(OffCommand), typeof(ICommand), typeof(ToggleFontIcon), null);
    public static readonly BindableProperty OffColorProperty = BindableProperty.Create(nameof(OffColor), typeof(Color), typeof(ToggleFontIcon), Color.Black);
    public static readonly BindableProperty OnColorProperty = BindableProperty.Create(nameof(OnColor), typeof(Color), typeof(ToggleFontIcon), Color.Black);

    public Color OnColor
    {
        get => (Color)GetValue(OnColorProperty);
        set => SetValue(OnColorProperty, value);
    }

    public Color OffColor
    {
        get => (Color)GetValue(OffColorProperty);
        set => SetValue(OffColorProperty, value);
    }

    public ICommand OnCommand
    {
        get => (ICommand)GetValue(OnCommandProperty);
        set => SetValue(OnCommandProperty, value);
    }

    public ICommand OffCommand
    {
        get => (ICommand)GetValue(OffCommandProperty);
        set => SetValue(OffCommandProperty, value);
    }

    public object CommandTarget
    {
        get => (object)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }


    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }


    public string OffIcon
    {
        get => (string)GetValue(OffIconProperty);
        set => SetValue(OffIconProperty, value);
    }

    public string OnIcon
    {
        get => (string)GetValue(OnIconProperty);
        set => SetValue(OnIconProperty, value);
    }

}
