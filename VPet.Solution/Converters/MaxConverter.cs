namespace HKW.WPF.Converters;

public class MaxConverter : MultiValueConverterBase
{
    public override object Convert(
        object[] values,
        Type targetType,
        object parameter,
        System.Globalization.CultureInfo culture
    )
    {
        return values.Max(i => (double)i);
    }
}
