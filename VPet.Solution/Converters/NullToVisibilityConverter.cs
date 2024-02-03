using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace HKW.WPF.Converters;

public class NullToVisibilityConverter : ValueConverterBase
{
    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty NullVisibilityValueProperty =
        DependencyProperty.Register(
            nameof(NullVisibilityValue),
            typeof(Visibility),
            typeof(NullToVisibilityConverter),
            new PropertyMetadata(Visibility.Hidden)
        );

    /// <summary>
    /// NULL时的可见度
    /// </summary>
    [DefaultValue(Visibility.Hidden)]
    public Visibility NullVisibilityValue
    {
        get => (Visibility)GetValue(NullVisibilityValueProperty);
        set => SetValue(NullVisibilityValueProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty NotNullVisibilityValueProperty =
        DependencyProperty.Register(
            nameof(NotNullVisibilityValue),
            typeof(Visibility),
            typeof(NullToVisibilityConverter),
            new PropertyMetadata(Visibility.Visible)
        );

    /// <summary>
    /// 不为NULL时的可见度
    /// </summary>
    [DefaultValue(Visibility.Visible)]
    public Visibility NotNullVisibilityValue
    {
        get => (Visibility)GetValue(NotNullVisibilityValueProperty);
        set => SetValue(NotNullVisibilityValueProperty, value);
    }

    public override object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        return value is null ? NullVisibilityValue : NotNullVisibilityValue;
    }
}
