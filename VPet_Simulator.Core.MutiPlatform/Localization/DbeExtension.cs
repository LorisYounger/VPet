using Avalonia.Markup.Xaml;
using LinePutScript.Localization;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Localization;

/// <summary>
/// AXAML 里按语言取数值的标记扩展: {ll:Dbe 键名, DefValue=默认值}
/// </summary>
/// 对应 Windows 版的 {ll:Dbe}. 用途是让不同语言的界面能有不同的宽高
/// (例如英文更长, 窗口要更宽), 数值写在语言包里, 没有就用 DefValue.
public class DbeExtension : MarkupExtension
{
    /// <summary>
    /// 语言包里的键名
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 语言包里没有时用的默认值
    /// </summary>
    public double DefValue { get; set; }

    public DbeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizeCore.GetDouble(Key, DefValue);
    }
}
