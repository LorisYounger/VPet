using System.Windows.Data;

namespace VPet.House.Converters;

public class NotEqualsConverter : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        System.Globalization.CultureInfo culture
    )
    {
        if (values.Length != 2)
            throw new NotImplementedException("Values length must be 2");
        return !values[0].Equals(values[1]);
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        System.Globalization.CultureInfo culture
    )
    {
        throw new NotImplementedException();
    }
}
