using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 全部为布尔值转换器
/// </summary>
public class AllIsBoolConverter : MultiValueToBoolConverter<AllIsBoolConverter>
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty BoolOnNullProperty = DependencyProperty.Register(
        nameof(BoolOnNull),
        typeof(bool),
        typeof(AllIsBoolConverter),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// 目标为空时的指定值
    /// </summary>
    [DefaultValue(false)]
    public bool BoolOnNull
    {
        get => (bool)GetValue(BoolOnNullProperty);
        set => SetValue(BoolOnNullProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="values"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        var boolValue = TargetBoolValue;
        var nullValue = BoolOnNull;
        return values.All(o => Utils.GetBool(o, boolValue, nullValue));
    }
}
