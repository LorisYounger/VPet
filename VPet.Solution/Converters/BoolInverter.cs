using System.Globalization;
using System.Windows.Data;

namespace VPet.House.Converters;

public class BoolInverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool b)
            return false;
        return !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
