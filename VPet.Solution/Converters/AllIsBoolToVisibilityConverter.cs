using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace HKW.WPF.Converters;

/// <summary>
/// 全部为布尔值转换器
/// </summary>
public class AllIsBoolToVisibilityConverter
    : MultiValueToBoolConverter<AllIsBoolToVisibilityConverter>
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty BoolOnNullProperty = DependencyProperty.Register(
        nameof(BoolOnNull),
        typeof(bool),
        typeof(AllIsBoolToVisibilityConverter),
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
    public static readonly DependencyProperty VisibilityOnTrueProperty =
        DependencyProperty.Register(
            nameof(VisibilityOnTrue),
            typeof(Visibility),
            typeof(AllIsBoolToVisibilityConverter),
            new PropertyMetadata(Visibility.Visible)
        );

    /// <summary>
    /// 目标为空时的指定值
    /// </summary>
    [DefaultValue(Visibility.Visible)]
    public Visibility VisibilityOnTrue
    {
        get => (Visibility)GetValue(TargetBoolValueProperty);
        set => SetValue(TargetBoolValueProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty VisibilityOnFalseProperty =
        DependencyProperty.Register(
            nameof(VisibilityOnFalse),
            typeof(Visibility),
            typeof(AllIsBoolToVisibilityConverter),
            new PropertyMetadata(Visibility.Hidden)
        );

    /// <summary>
    /// 目标为空时的指定值
    /// </summary>
    [DefaultValue(Visibility.Hidden)]
    public Visibility VisibilityOnFalse
    {
        get => (Visibility)GetValue(TargetBoolValueProperty);
        set => SetValue(TargetBoolValueProperty, value);
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
        return values.All(o => HKWUtils.HKWUtils.GetBool(o, boolValue, nullValue))
            ? VisibilityOnTrue
            : VisibilityOnFalse;
    }
}
