using Avalonia.Markup.Xaml;
using LinePutScript.Localization;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Localization;

/// <summary>
/// AXAML 里的翻译标记扩展: {ll:Str 文本}
/// </summary>
/// 对应 Windows 版 LinePutScript.Localization.WPF 提供的 {ll:Str}, 写法完全一致,
/// 这样 XAML 搬到 AXAML 时是机械替换. 源字符串本身就是翻译键, 与 .Translate() 同源.
public class StrExtension : MarkupExtension
{
    /// <summary>
    /// 翻译键 (也是默认显示的原文)
    /// </summary>
    public string Key { get; set; }

    public StrExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizeCore.Translate(Key);
    }
}
