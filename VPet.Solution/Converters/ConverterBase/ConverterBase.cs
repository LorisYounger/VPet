using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 值转换器基础
/// </summary>
public abstract class ConverterBase : DependencyObject
{
    /// <summary>
    /// 未设置值
    /// </summary>
    public static readonly object UnsetValue = DependencyProperty.UnsetValue;
}
