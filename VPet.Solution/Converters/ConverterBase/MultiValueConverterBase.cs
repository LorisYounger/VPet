using System.Globalization;
using System.Windows.Data;

namespace HKW.WPF.Converters;

/// <summary>
/// 多个值转换器
/// </summary>
public abstract class MultiValueConverterBase : ConverterBase, IMultiValueConverter
{
    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="values">值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="culture">文化</param>
    /// <returns>转换后的值</returns>
    public abstract object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture
    );

    /// <summary>
    /// 转换回调
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="culture">文化</param>
    /// <returns>转换后的值</returns>
    public virtual object[] ConvertBack(
        object value,
        Type[] targetType,
        object parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException(
            "Converter '" + this.GetType().Name + "' does not support backward conversion."
        );
    }
}
