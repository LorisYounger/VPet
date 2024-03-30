using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace HKW.WPF.Converters;

public class ValueToBoolConverter : ValueConverterBase
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty TargetValueProperty = DependencyProperty.Register(
        nameof(TargetValue),
        typeof(object),
        typeof(ValueToBoolConverter),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 目标值
    /// </summary>
    [DefaultValue(true)]
    public object TargetValue
    {
        get => (object)GetValue(TargetValueProperty);
        set => SetValue(TargetValueProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty InvertProperty = DependencyProperty.Register(
        nameof(Invert),
        typeof(bool),
        typeof(ValueToBoolConverter),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// 颠倒
    /// </summary>
    [DefaultValue(false)]
    public bool Invert
    {
        get => (bool)GetValue(InvertProperty);
        set => SetValue(InvertProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty NullValueProperty = DependencyProperty.Register(
        nameof(NullValue),
        typeof(bool),
        typeof(ValueToBoolConverter),
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
        if (value is null)
            return NullValue;
        return value.Equals(TargetValue) ^ Invert;
    }
}
