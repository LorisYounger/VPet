using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace HKW.WPF.Converters;

public class BoolInverter : ValueConverterBase
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty NullValueProperty = DependencyProperty.Register(
        nameof(NullValue),
        typeof(bool),
        typeof(BoolInverter),
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

    public override object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        if (value is not bool b)
            return NullValue;
        return !b;
    }
}
