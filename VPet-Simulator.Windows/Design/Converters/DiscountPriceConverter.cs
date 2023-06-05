using System;
using System.Globalization;
using System.Windows.Data;

namespace VPet_Simulator.Windows
{
    public class DiscountPriceConverter
         : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var price = (double)values[0];
            var discount = (int)values[1];
            var discountPrice = (price * (discount / 100d));
            return $"¥ {discountPrice.ToString("0.0")}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
