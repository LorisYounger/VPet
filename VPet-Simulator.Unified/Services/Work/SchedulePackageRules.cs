using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 打工套餐与日程表的算式
/// </summary>
/// 从 Windows 版 ScheduleTask.Package 和 winWorkMenu 里抽出来的, 算式一个没改.
/// 这些数直接决定经济系统, 两端必须用同一份 —— 抄一遍就迟早会抄歪.
public static class SchedulePackageRules
{
    /// <summary>
    /// 签一份套餐要多少钱
    /// </summary>
    /// <param name="basePrice">套餐的基础价</param>
    /// <param name="level">签的档次</param>
    public static double SignPrice(double basePrice, int level) => basePrice * (200 * level - 100);

    /// <summary>
    /// 签下来之后拿到的可用等级
    /// </summary>
    public static int GrantedLevel(int level, double levelInNeed) => (int)(level / levelInNeed);

    /// <summary>
    /// 套餐的到期时间
    /// </summary>
    public static DateTime EndTime(DateTime now, int durationDays) => now.AddDays(durationDays);

    /// <summary>
    /// 套餐还生效吗
    /// </summary>
    public static bool IsActive(DateTime endTime, DateTime now) => now < endTime;

    /// <summary>
    /// 日程表里工作占比超过这个数就该标红了
    /// </summary>
    public const double WorkRatioWarning = 0.71;

    /// <summary>
    /// 工作占总时长的比例
    /// </summary>
    public static double WorkRatio(int workTime, int restTime)
        => workTime + restTime == 0 ? 0 : workTime / (double)(workTime + restTime);
}
