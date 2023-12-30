using System.ComponentModel;
using System.Windows;

namespace HKW.WPF.Converters;

public abstract class BoolToValueConverterBase<T> : ValueConverterBase
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty BoolValueProperty = DependencyProperty.Register(
        nameof(BoolValue),
        typeof(bool),
        typeof(T),
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
        typeof(T),
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
}
