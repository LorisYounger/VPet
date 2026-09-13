using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace VPet_Simulator.MutiPlatform
{
    //跨平台: 对应 VPet-Simulator.Windows/Design/Converters/BoolToIntConverter.cs, 只换了 IValueConverter 的命名空间
    public class BoolToIntConverter
       : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value as bool?) == true ? 1 : 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
