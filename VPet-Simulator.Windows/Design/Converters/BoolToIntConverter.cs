using System;
using System.Globalization;
using System.Windows.Data;

namespace VPet_Simulator.Windows
{
    public class BoolToIntConverter
       : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as bool?) == true ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
