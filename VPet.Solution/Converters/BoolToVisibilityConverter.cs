using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HKW.WPF.Converters;

public class BoolToVisibilityConverter : BoolToValueConverterBase<BoolToVisibilityConverter>
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty TrueVisibilityValueProperty =
        DependencyProperty.Register(
            nameof(TrueVisibilityValue),
            typeof(Visibility),
            typeof(AllIsBoolToVisibilityConverter),
            new PropertyMetadata(Visibility.Visible)
        );

    /// <summary>
    /// 为真时的可见度
    /// </summary>
    [DefaultValue(Visibility.Visible)]
    public Visibility TrueVisibilityValue
    {
        get => (Visibility)GetValue(TrueVisibilityValueProperty);
        set => SetValue(TrueVisibilityValueProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty FalseVisibilityValueProperty =
        DependencyProperty.Register(
            nameof(FalseVisibilityValue),
            typeof(Visibility),
            typeof(AllIsBoolToVisibilityConverter),
            new PropertyMetadata(Visibility.Hidden)
        );

    /// <summary>
    /// 为假时的可见度
    /// </summary>
    [DefaultValue(Visibility.Hidden)]
    public Visibility FalseVisibilityValue
    {
        get => (Visibility)GetValue(FalseVisibilityValueProperty);
        set => SetValue(FalseVisibilityValueProperty, value);
    }

    public override object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        return Utils.GetBool(value, BoolValue, NullValue)
            ? TrueVisibilityValue
            : FalseVisibilityValue;
    }
}
