using LinePutScript.Localization.WPF;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 活动日志的 Windows 半
/// </summary>
/// 只剩这一个方法: 它要的 Main 是 WPF 的控件类型, 没法共享。
public partial class ActivityLog
{
    /// <summary>
    /// 转换成玩家可读的字符串
    /// </summary>
    public string ToString(Main m)
    {
        return $"[{Time.ToShortTimeString()}] {string.Format(IText.ConverText(("al_" + Type).Translate(), m), Description.Split('|'))}";
    }
}
