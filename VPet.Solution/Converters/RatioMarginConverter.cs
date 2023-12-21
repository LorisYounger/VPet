using System.Windows;
using System.Windows.Data;

namespace VPet.House.Converters;

/// <summary>
/// 边距转换器
/// <para>示例:
/// <code><![CDATA[
/// <MultiBinding Converter="{StaticResource RatioMarginConverter}">
///   <Binding Path="Left" />
///   <Binding Path="Top" />
///   <Binding Path="Right" />
///   <Binding Path="Bottom" />
/// </MultiBinding>
/// ]]></code></para>
/// </summary>
public class RatioMarginConverter : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        System.Globalization.CultureInfo culture
    )
    {
        if (values.Any(i => i == DependencyProperty.UnsetValue))
            return new Thickness();
        if (values.Length == 0)
        {
            return new Thickness();
        }
        else if (values.Length == 1)
        {
            return new Thickness();
        }
        var ratio = System.Convert.ToDouble(values[0]);
        if (values.Length == 2)
        {
            return new Thickness()
            {
                Left = System.Convert.ToDouble(values[1]) * ratio,
                Top = default,
                Right = default,
                Bottom = default
            };
        }
        else if (values.Length == 3)
        {
            return new Thickness()
            {
                Left = System.Convert.ToDouble(values[1]) * ratio,
                Top = System.Convert.ToDouble(values[2]) * ratio,
                Right = default,
                Bottom = default
            };
        }
        else if (values.Length == 4)
        {
            return new Thickness()
            {
                Left = System.Convert.ToDouble(values[1]) * ratio,
                Top = System.Convert.ToDouble(values[2]) * ratio,
                Right = System.Convert.ToDouble(values[3]) * ratio,
                Bottom = default
            };
        }
        else if (values.Length == 5)
        {
            return new Thickness()
            {
                Left = System.Convert.ToDouble(values[1]) * ratio,
                Top = System.Convert.ToDouble(values[2]) * ratio,
                Right = System.Convert.ToDouble(values[3]) * ratio,
                Bottom = System.Convert.ToDouble(values[4]) * ratio
            };
        }
        else
            throw new NotImplementedException();
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
