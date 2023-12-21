using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VPet.House.Converters;

public class FalseToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool.TryParse(value.ToString(), out var result) && result)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Collapsed;
    }
}
