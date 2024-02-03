using System.Globalization;

namespace HKW.WPF.Converters;

/// <summary>
/// 相等转换器
/// <para>示例:
/// <code><![CDATA[
/// <MultiBinding Converter="{StaticResource RatioMarginConverter}">
///   <Binding Path="Value1" />
///   <Binding Path="Value2" />
/// </MultiBinding>
/// ]]></code></para>
/// </summary>
public class EqualsConverter : CanInverterMultiValueConverter<EqualsConverter>
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="values"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        if (values.Length != 2)
            throw new NotImplementedException("Values length must be 2");
        return values[0].Equals(values[1]) ^ Inverter;
    }
}
