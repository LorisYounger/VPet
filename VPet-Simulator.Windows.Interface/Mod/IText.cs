using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Interface;

public class IText
{
    /// <summary>
    /// 说话的内容
    /// </summary>
    [Line(IgnoreCase = true)] public string Text { get; set; }

    private string transText = null;
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

}
