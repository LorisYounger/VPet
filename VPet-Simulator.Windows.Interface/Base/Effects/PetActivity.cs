using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 效果适用的宠物状态；基础活动按或组合，Schedule 要求日程开启
/// </summary>
[Flags]
public enum PetActivity
{
    /// <summary>不适用于任何活动</summary>
    None = 0,
    /// <summary>空闲时的恢复、消耗及上限调整。</summary>
    Idle = 1 << 0,
    /// <summary>睡眠时的恢复及上限调整。</summary>
    Sleep = 1 << 1,
    /// <summary>学习</summary>
    Study = 1 << 2,
    /// <summary>工作</summary>
    Work = 1 << 3,
    /// <summary>娱乐</summary>
    Play = 1 << 4,
    /// <summary>旅行</summary>
    Travel = 1 << 5,
    /// <summary>其他活动</summary>
    Other = 1 << 6,
    /// <summary>仅在使用物品时调整属性收益倍率</summary>
    TakeItem = 1 << 7,
    /// <summary>适用于所有基础活动，不要求日程开启</summary>
    All = Idle | Sleep | Study | Work | Play | Travel | Other | TakeItem,
    /// <summary>要求日程开启；单独使用时适用于所有基础活动</summary>
    Schedule = 1 << 8,
    /// <summary>仅适用于对应的已启用日程工作状态</summary>
    ScheduleWork = Schedule | Work,
    ScheduleStudy = Schedule | Study,
    SchedulePlay = Schedule | Play
}
