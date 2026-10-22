namespace HeatController;

public class Separator : View
{

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create("Orientation", typeof(SeparatorOrientation), typeof(Separator), SeparatorOrientation.Horizontal, BindingMode.OneWay, null, null, null, null);

    public SeparatorOrientation Orientation
    {
        get => (SeparatorOrientation)base.GetValue(Separator.OrientationProperty);

        set => base.SetValue(Separator.OrientationProperty, value);
    }

    public static readonly BindableProperty ColorProperty = BindableProperty.Create("Color", typeof(Color), typeof(Separator), Color.Default, BindingMode.OneWay, null, null, null, null);

    public Color Color
    {
        get => (Color)base.GetValue(Separator.ColorProperty);
        set => base.SetValue(Separator.ColorProperty, value);
    }

    public static readonly BindableProperty SpacingBeforeProperty = BindableProperty.Create("SpacingBefore", typeof(double), typeof(Separator), (double)1, BindingMode.OneWay, null, null, null, null);

    public double SpacingBefore
    {
        get => (double)base.GetValue(Separator.SpacingBeforeProperty);
        set => base.SetValue(Separator.SpacingBeforeProperty, value);
    }

    public static readonly BindableProperty SpacingAfterProperty = BindableProperty.Create("SpacingAfter", typeof(double), typeof(Separator), (double)1, BindingMode.OneWay, null, null, null, null);

    public double SpacingAfter
    {
        get => (double)base.GetValue(Separator.SpacingAfterProperty);
        set => base.SetValue(Separator.SpacingAfterProperty, value);
    }

    public static readonly BindableProperty ThicknessProperty = BindableProperty.Create("Thickness", typeof(double), typeof(Separator), (double)1, BindingMode.OneWay, null, null, null, null);

    public double Thickness
    {
        get => (double)base.GetValue(Separator.ThicknessProperty);
        set => base.SetValue(Separator.ThicknessProperty, value);
    }


    public static readonly BindableProperty StrokeTypeProperty = BindableProperty.Create("StrokeType", typeof(StrokeType), typeof(Separator), StrokeType.Solid, BindingMode.OneWay, null, null, null, null);

    public StrokeType StrokeType
    {
        get => (StrokeType)base.GetValue(Separator.StrokeTypeProperty);
        set => base.SetValue(Separator.StrokeTypeProperty, value);
    }

    public Separator()
    {
        UpdateRequestedSize();
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == ThicknessProperty.PropertyName ||
           propertyName == ColorProperty.PropertyName ||
           propertyName == SpacingBeforeProperty.PropertyName ||
           propertyName == SpacingAfterProperty.PropertyName ||
           propertyName == StrokeTypeProperty.PropertyName ||
           propertyName == OrientationProperty.PropertyName)
        {
            UpdateRequestedSize();
        }
    }

    private void UpdateRequestedSize()
    {
        var minSize = Thickness;
        var optimalSize = SpacingBefore + Thickness + SpacingAfter;
        if (Orientation == SeparatorOrientation.Horizontal)
        {
            MinimumHeightRequest = minSize;
            HeightRequest = optimalSize;
            HorizontalOptions = LayoutOptions.FillAndExpand;
        }
        else
        {
            MinimumWidthRequest = minSize;
            WidthRequest = optimalSize;
            VerticalOptions = LayoutOptions.FillAndExpand;
        }
    }
}
public enum StrokeType
{
    Solid,
    Dotted,
    Dashed
}

public enum SeparatorOrientation
{
    Vertical,
    Horizontal
}

