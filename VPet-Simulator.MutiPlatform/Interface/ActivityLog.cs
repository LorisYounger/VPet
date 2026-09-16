//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/ActivityLog.cs; 只换了本地化与 Main 的命名空间
using LinePutScript.Localization;
using VPet_Simulator.Core.MutiPlatform.Display;

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
