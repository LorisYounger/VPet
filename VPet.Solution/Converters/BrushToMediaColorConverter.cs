using System.Globalization;
using System.Windows.Media;

namespace HKW.WPF.Converters;

/// <summary>
/// 画笔颜色至媒体颜色转换器
/// </summary>
public class BrushToMediaColorConverter : ValueConverterBase
{
    /// <inheritdoc/>
    public override object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        if (value is not SolidColorBrush brush)
            throw new ArgumentException(
                $"Not type: {typeof(SolidColorBrush).FullName}",
                nameof(value)
            );
        return brush.Color;
    }

    /// <inheritdoc/>
    public override object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        if (value is not Color color)
            throw new ArgumentException($"Not type: {typeof(Color).FullName}", nameof(value));
        return new SolidColorBrush(color);
    }
}
