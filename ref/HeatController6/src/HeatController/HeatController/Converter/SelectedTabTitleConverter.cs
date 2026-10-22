using Syncfusion.XForms.TabView;
using System.Globalization;

namespace HeatController;

public class SelectedTabTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SfTabView {SelectedIndex: >= 0} tabView)
        {
            return tabView.Items[tabView.SelectedIndex].Title;
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
