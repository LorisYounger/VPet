using System.ComponentModel;
using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 可反转值转换器
/// </summary>
public abstract class CanInverterValueConverter<T> : ValueConverterBase
    where T : CanInverterValueConverter<T>
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty InverterProperty = DependencyProperty.Register(
        nameof(Inverter),
        typeof(bool),
        typeof(AllIsBoolToVisibilityConverter),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// 反转
    /// </summary>
    [DefaultValue(false)]
    public bool Inverter
    {
        get => (bool)GetValue(InverterProperty);
        set => SetValue(InverterProperty, value);
    }
}
