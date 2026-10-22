using System.Collections;
using System.Reflection;

namespace HeatController;
public class ExtendedPicker:Picker
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ExtendedPicker ()
    {
        base.SelectedIndexChanged += OnSelectedIndexChanged;
    }

    public static readonly BindableProperty SelectedItemProperty =    BindableProperty.Create("SelectedItem", typeof(object),     typeof(ExtendedPicker), null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(ExtendedPicker.OnSelectedItemChanged), null, null, null);
    public static readonly BindableProperty ItemsSourceProperty =     BindableProperty.Create("ItemsSource", typeof(IEnumerable), typeof(ExtendedPicker), null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(ExtendedPicker.OnItemsSourceChanged), null, null, null);
    public static readonly BindableProperty DisplayPropertyProperty = BindableProperty.Create("DisplayProperty", typeof(string),  typeof(ExtendedPicker), null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(ExtendedPicker.OnDisplayPropertyChanged), null, null, null);
    public static readonly BindableProperty KeyMemberPathProperty = BindableProperty.Create("KeyMemberPath", typeof(string), typeof(ExtendedPicker), null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(ExtendedPicker.OnKeyMemberPathChanged), null, null, null);
    public static readonly BindableProperty CancelButtonProperty = BindableProperty.Create("CancelButton", typeof(string), typeof(ExtendedPicker), "CANCEL", BindingMode.OneWay);

    public string CancelButton
    {
        get => (string)GetValue(CancelButtonProperty);
        set => SetValue(CancelButtonProperty, value);
    }

    public IList ItemsSource
    {
        get
        {
            return (IList)base.GetValue(ExtendedPicker.ItemsSourceProperty);
        }
        set
        {
            base.SetValue(ExtendedPicker.ItemsSourceProperty, value);
        }
    }

    public object SelectedItem
    {
        get
        {
            return base.GetValue(ExtendedPicker.SelectedItemProperty);
        }
        set
        {
            base.SetValue(ExtendedPicker.SelectedItemProperty, value);
        }
    }
    
    public string DisplayProperty
    {
        get
        {
            return (string)base.GetValue(ExtendedPicker.DisplayPropertyProperty);
        }
        set
        {
            base.SetValue(ExtendedPicker.DisplayPropertyProperty, value);
        }
    }

    public string KeyMemberPath
    {
        get
        {
            return (string)base.GetValue(ExtendedPicker.KeyMemberPathProperty);
        }
        set
        {
            base.SetValue(ExtendedPicker.KeyMemberPathProperty, value);
        }
    }

    private void OnSelectedIndexChanged(object sender,EventArgs e)
    {
        this.SelectedItem = ItemsSource [SelectedIndex];
    }


    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ExtendedPicker bindablePicker = (ExtendedPicker) bindable;
        //ExtendedPicker picker = bindable as ExtendedPicker;
        // if (picker != null)
        // {
        //     var selectedItem = picker.ItemsSource[picker.SelectedIndex];
        //     if (!string.IsNullOrWhiteSpace(picker.KeyMemberPath))
        //     {
        //         var keyProperty = selectedItem.GetType().GetRuntimeProperty(picker.KeyMemberPath);
        //         if (keyProperty == null)
        //         {
        //             throw new InvalidOperationException(String.Concat(picker.KeyMemberPath, " is not a property of ",
        //                 selectedItem.GetType().FullName));
        //         }
        //         picker.SelectedItem = keyProperty.GetValue(selectedItem);
        //     }
        //     else
        //     {
        //         picker.SelectedItem = selectedItem.ToString();
        //     }
        // }
        if (bindablePicker.ItemsSource != null && bindablePicker.SelectedItem != null)
        {
            int count = 0;
            foreach (object obj in bindablePicker.ItemsSource)
            {
                if (obj == bindablePicker.SelectedItem)
                {
                    bindablePicker.SelectedIndex = count;
                    break;
                }
                count++;
            }
        }
    }

    private static void OnDisplayPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {

        ExtendedPicker bindablePicker = (ExtendedPicker)bindable;
        bindablePicker.DisplayProperty = (string)newValue;
        loadItemsAndSetSelected (bindable);

    }

    private static void OnKeyMemberPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ExtendedPicker picker = bindable as ExtendedPicker;
        picker.KeyMemberPath = newValue?.ToString();
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ExtendedPicker bindablePicker = (ExtendedPicker)bindable;
        bindablePicker.ItemsSource = (IList)newValue;
        loadItemsAndSetSelected (bindable);
    }
    static void loadItemsAndSetSelected (BindableObject bindable)
    {
        ExtendedPicker bindablePicker = (ExtendedPicker)bindable;
        if (bindablePicker.ItemsSource as IEnumerable != null) {
            PropertyInfo propertyInfo = null;
            int count = 0;
            foreach (object obj in (IEnumerable)bindablePicker.ItemsSource) {
                string value = string.Empty;
                if (bindablePicker.DisplayProperty != null) {
                    if (propertyInfo == null) {
                        propertyInfo = obj.GetType ().GetRuntimeProperty (bindablePicker.DisplayProperty);
                        if (propertyInfo == null)
                            throw new Exception (String.Concat (bindablePicker.DisplayProperty, " is not a property of ", obj.GetType ().FullName));
                    }
                    value = propertyInfo.GetValue (obj).ToString();
                }
                else {
                    value = obj.ToString();
                }
                bindablePicker.Items.Add (value);
                if (bindablePicker.SelectedItem != null) {
                    if (bindablePicker.SelectedItem == obj) {
                        bindablePicker.SelectedIndex = count;
                    }
                }
                count++;
            }
        }
    }
}

