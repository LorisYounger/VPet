using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace VPet_Simulator.MutiPlatform
{
    //跨平台: 对应 VPet-Simulator.Windows/Design/Converters/DiscountPriceConverter.cs; Avalonia 的多值转换器参数是 IList
    public class DiscountPriceConverter
         : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var price = (double)values[0]!;
            var discount = (int)values[1]!;
            var discountPrice = (price * (discount / 100d));
            return $"¥ {discountPrice.ToString("0.0")}";
        }
    }
}
