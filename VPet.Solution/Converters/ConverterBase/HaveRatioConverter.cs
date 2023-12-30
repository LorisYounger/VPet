using System.ComponentModel;
using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 含有比率的转换器
/// </summary>
/// <typeparam name="T">转换器类型</typeparam>
public abstract class HaveRatioConverter<T> : MultiValueConverterBase
    where T : HaveRatioConverter<T>
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty HaveRatioProperty = DependencyProperty.Register(
        nameof(HaveRatio),
        typeof(bool),
        typeof(T),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// 含有比例
    /// </summary>
    [DefaultValue(false)]
    public bool HaveRatio
    {
        get => (bool)GetValue(HaveRatioProperty);
        set => SetValue(HaveRatioProperty, value);
    }
}
