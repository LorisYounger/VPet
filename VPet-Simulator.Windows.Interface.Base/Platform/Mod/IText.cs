//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/Mod/IText.cs; 只换了本地化与 Main 的命名空间
using LinePutScript.Converter;
using LinePutScript.Localization;
using System;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform.Display;

namespace VPet_Simulator.Windows.Interface;

public class IText
{
    /// <summary>
    /// 说话的内容
    /// </summary>
    [Line(IgnoreCase = true)] public string Text { get; set; } = string.Empty;

    private string? transText = null;
    /// <summary>
    /// 说话的内容 (翻译)
    /// </summary>
    public string TranslateText
    {
        get
        {
            if (transText == null)
            {
                transText = LocalizeCore.Translate(Text);
            }
            return transText;
        }
        set
        {
            transText = value;
        }
    }
    /// <summary>
    /// 文本内容标签
    /// </summary>
    [Line(IgnoreCase = true)]
    public string Tag
    {
        get => string.Join(",", tags);
        set => tags = value.Split(',');
    }

    private string[] tags = new string[] { "all" };
    /// <summary>
    /// 查找是否符合内容标签
    /// </summary>
    public bool FindTag(string[] tags) => tags.Any(tag => this.tags.Contains(tag));


    /// <summary>
    /// 将文本转换成实际值
    /// </summary>
    public string TranslateTextConvert(Main m) => ConverText(TranslateText, m);
    /// <summary>
    /// 将文本转换成实际值 (注意: 会和 Trainslate({0}) 冲突), 先 Trainslate, 再 Convert 最后再 Format
    /// </summary>
    public static string ConverText(string text, Main m)
        //占位符表在共享源码里, 两个平台认得出同一批
        => SaveTextTemplate.Convert(text, m.Core.Save!);
}
