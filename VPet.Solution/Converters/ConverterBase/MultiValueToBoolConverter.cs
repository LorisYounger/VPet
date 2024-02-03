using System.ComponentModel;
using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 转换至布尔转换器基础
/// </summary>
public abstract class MultiValueToBoolConverter<T> : MultiValueConverterBase
    where T : MultiValueToBoolConverter<T>
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty TargetBoolValueProperty = DependencyProperty.Register(
        nameof(TargetBoolValue),
        typeof(bool),
        typeof(T),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// 指定值
    /// </summary>
    [DefaultValue(false)]
    public bool TargetBoolValue
    {
        get => (bool)GetValue(TargetBoolValueProperty);
        set => SetValue(TargetBoolValueProperty, value);
    }
}
