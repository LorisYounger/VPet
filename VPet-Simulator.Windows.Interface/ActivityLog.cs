using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 活动日志: eg: 工作, 事件等
/// </summary>
public class ActivityLog
{
    /// <summary>
    /// 活动日志: eg: 工作, 事件等
    /// </summary>
    public ActivityLog()
    {
    }
    /// <summary>
    /// 活动日志: eg: 工作, 事件等
    /// </summary>
    /// <param name="type">日志类型</param>
    /// <param name="isDebug">是否是调试日志 (仅在调试模式下显示给玩家)</param>
    /// <param name="description">日志详细信息</param>
    public ActivityLog(string type, bool isDebug = false, params string[] description)
    {
        Time = DateTime.Now;
        Type = type;
        Description = string.Join('|', description);
        IsDebug = isDebug;
    }
    /// <summary>
    /// 活动日志(非调试日志): eg: 工作, 事件等
    /// </summary>
    /// <param name="type">日志类型</param>
    /// <param name="description">日志详细信息</param>
    public ActivityLog(string type, params string[] description)
    {
        Time = DateTime.Now;
        Type = type;
        Description = string.Join('|', description);
        IsDebug = false;
    }
    /// <summary>
    /// 日志时间
    /// </summary>
    [Line]
    public DateTime Time { get; set; }
    /// <summary>
    /// 日志类型
    /// </summary>
    [Line]
    public string Type { get; set; }
    /// <summary>
    /// 日志详细信息, `|`分割
    /// </summary>
    [Line]
    public string Description { get; set; }

    /// <summary>
    /// 是否是调试日志 (仅在调试模式下显示给玩家)
    /// </summary>
    [Line] public bool IsDebug { get; set; }

    /// <summary>
    /// 转换成玩家可读的字符串
    /// </summary>
    public string ToString(Main m)
    {
        return $"[{Time.ToShortTimeString()}] {IText.ConverText(("al_" + Type).Translate(Description.Split('|')),m)}";
    }
}
