using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using LinePutScript.Localization;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Localization;

/// <summary>
/// AXAML 里的翻译标记扩展: {ll:Str 文本}
/// </summary>
/// 对应 Windows 版 LinePutScript.Localization.WPF 提供的 {ll:Str}, 写法完全一致,
/// 这样 XAML 搬到 AXAML 时是机械替换. 源字符串本身就是翻译键, 与 .Translate() 同源.
/// 带 ValueSource 时 ({ll:Str '{0:f1} 秒', ValueSource={Binding ...}}) 翻译结果当格式串, 把绑定的值填进去.
public class StrExtension : MarkupExtension
{
    /// <summary>
    /// 翻译键 (也是默认显示的原文)
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 要填进格式串里的值 (一个绑定)
    /// </summary>
    public object? ValueSource { get; set; }

    public StrExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// WPF 的 TextBlock 把单独的回车符当换行, Avalonia 不认, 翻译结果里的回车统一换成换行
    /// (Windows 版的 XAML 里写的是 \&#13;)
    /// </summary>
    public static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (ValueSource is BindingBase binding)
        {
            var format = Normalize(LocalizeCore.Translate(Key));
            var converter = new FuncValueConverter<object?, string>(v => string.Format(format, v));
            switch (binding)
            {
                case Binding b:
                    b.Converter = converter;
                    break;
                case Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindingExtension cb:
                    cb.Converter = converter;
                    break;
            }
            return binding;
        }
        return Normalize(LocalizeCore.Translate(Key));
    }
}
