using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 是布尔值转换器
/// </summary>
public class IsBoolConverter : ValueConverterBase
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty BoolValueProperty = DependencyProperty.Register(
        nameof(BoolValue),
        typeof(bool),
        typeof(AllIsBoolToVisibilityConverter),
        new PropertyMetadata(true)
    );

    /// <summary>
    /// 目标布尔值
    /// </summary>
    [DefaultValue(true)]
    public bool BoolValue
    {
        get => (bool)GetValue(BoolValueProperty);
        set => SetValue(BoolValueProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty NullValueProperty = DependencyProperty.Register(
        nameof(NullValue),
        typeof(bool),
        typeof(AllIsBoolToVisibilityConverter),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// 为空值时布尔值
    /// </summary>
    [DefaultValue(false)]
    public bool NullValue
    {
        get => (bool)GetValue(NullValueProperty);
        set => SetValue(NullValueProperty, value);
    }

    /// <inheritdoc/>
    public override object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        return Utils.GetBool(value, BoolValue, NullValue);
    }
}
