using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VPet.House.Converters;

public class MediaColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
            throw new ArgumentException("Not media color", nameof(value));
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush)
            throw new ArgumentException("Not SolidColorBrush", nameof(value));
        return brush.Color;
    }
}
