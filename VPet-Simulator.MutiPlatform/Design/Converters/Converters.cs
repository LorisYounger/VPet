using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// 对应 Panuon 的 pu:Converters 里用到的那几个: XAML 里 {x:Static pu:Converters.X} 换成 {x:Static local:Converters.X}
    /// </summary>
    /// TrueToCollapse / FalseToCollapse 不在这里: Avalonia 的可见性是 bool, 绑定写 IsVisible="{Binding !X}" 即可
    public static class Converters
    {
        public static readonly IValueConverter DoublePlusConverter = new FuncValueConverter<double, object?, double>((v, p) => v + Number(p));
        public static readonly IValueConverter DoubleMinusConverter = new FuncValueConverter<double, object?, double>((v, p) => v - Number(p));
        public static readonly IValueConverter DoubleDivideByConverter = new FuncValueConverter<double, object?, double>((v, p) => v / Number(p));
        public static readonly IValueConverter TrueToFalseConverter = new FuncValueConverter<bool, bool>(v => !v);

        private static double Number(object? parameter)
            => parameter == null ? 0 : System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
    }
}
