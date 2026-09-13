using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 开关
/// </summary>
/// 对应 Panuon 的 pu:Switch. 属性名照 Panuon 的取 (BoxWidth/BoxHeight/ToggleSize/CheckedBackground…),
/// XAML 里 pu:Switch 机械替换成 shell:Switch 即可, 其余属性一个都不用改; 样子画在 basestyle.axaml 里.
/// 本质就是个 ToggleButton, IsChecked/Content/Click/Checked/Unchecked 全是继承来的.
public class Switch : ToggleButton
{
    /// <summary>
    /// 开关底座宽度
    /// </summary>
    public static readonly StyledProperty<double> BoxWidthProperty =
        AvaloniaProperty.Register<Switch, double>(nameof(BoxWidth), 35);
    /// <summary>
    /// 开关底座高度
    /// </summary>
    public static readonly StyledProperty<double> BoxHeightProperty =
        AvaloniaProperty.Register<Switch, double>(nameof(BoxHeight), 18);
    /// <summary>
    /// 滑块直径
    /// </summary>
    public static readonly StyledProperty<double> ToggleSizeProperty =
        AvaloniaProperty.Register<Switch, double>(nameof(ToggleSize), 14);
    /// <summary>
    /// 打开时底座的背景
    /// </summary>
    public static readonly StyledProperty<IBrush?> CheckedBackgroundProperty =
        AvaloniaProperty.Register<Switch, IBrush?>(nameof(CheckedBackground));
    /// <summary>
    /// 打开时底座的边框
    /// </summary>
    public static readonly StyledProperty<IBrush?> CheckedBorderBrushProperty =
        AvaloniaProperty.Register<Switch, IBrush?>(nameof(CheckedBorderBrush));
    /// <summary>
    /// 关闭时滑块的颜色
    /// </summary>
    public static readonly StyledProperty<IBrush?> ToggleBrushProperty =
        AvaloniaProperty.Register<Switch, IBrush?>(nameof(ToggleBrush));
    /// <summary>
    /// 打开时滑块的颜色
    /// </summary>
    public static readonly StyledProperty<IBrush?> CheckedToggleBrushProperty =
        AvaloniaProperty.Register<Switch, IBrush?>(nameof(CheckedToggleBrush));
    /// <summary>
    /// 打开时文字的颜色
    /// </summary>
    public static readonly StyledProperty<IBrush?> CheckedForegroundProperty =
        AvaloniaProperty.Register<Switch, IBrush?>(nameof(CheckedForeground));
    /// <summary>
    /// 滑块的阴影颜色 (Windows 版都写的 {x:Null}, 这里收下不画)
    /// </summary>
    public static readonly StyledProperty<Color?> ToggleShadowColorProperty =
        AvaloniaProperty.Register<Switch, Color?>(nameof(ToggleShadowColor));

    public double BoxWidth
    {
        get => GetValue(BoxWidthProperty);
        set => SetValue(BoxWidthProperty, value);
    }
    public double BoxHeight
    {
        get => GetValue(BoxHeightProperty);
        set => SetValue(BoxHeightProperty, value);
    }
    public double ToggleSize
    {
        get => GetValue(ToggleSizeProperty);
        set => SetValue(ToggleSizeProperty, value);
    }
    public IBrush? CheckedBackground
    {
        get => GetValue(CheckedBackgroundProperty);
        set => SetValue(CheckedBackgroundProperty, value);
    }
    public IBrush? CheckedBorderBrush
    {
        get => GetValue(CheckedBorderBrushProperty);
        set => SetValue(CheckedBorderBrushProperty, value);
    }
    public IBrush? ToggleBrush
    {
        get => GetValue(ToggleBrushProperty);
        set => SetValue(ToggleBrushProperty, value);
    }
    public IBrush? CheckedToggleBrush
    {
        get => GetValue(CheckedToggleBrushProperty);
        set => SetValue(CheckedToggleBrushProperty, value);
    }
    public IBrush? CheckedForeground
    {
        get => GetValue(CheckedForegroundProperty);
        set => SetValue(CheckedForegroundProperty, value);
    }
    public Color? ToggleShadowColor
    {
        get => GetValue(ToggleShadowColorProperty);
        set => SetValue(ToggleShadowColorProperty, value);
    }

    /// <summary>
    /// 底座圆角 = 高度的一半, 模板里用
    /// </summary>
    public static readonly IValueConverter HalfCornerRadius =
        new FuncValueConverter<double, CornerRadius>(h => new CornerRadius(h / 2));

    protected override System.Type StyleKeyOverride => typeof(Switch);

    //选择器写不了 "属性不为 null", 用伪类表示 CheckedForeground 有没有给
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CheckedForegroundProperty)
            PseudoClasses.Set(":checkedforeground", CheckedForeground != null);
    }
}
