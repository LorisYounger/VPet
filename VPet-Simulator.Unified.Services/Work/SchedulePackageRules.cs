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
    /// 算"还剩多久"时要除以的那个数
    /// </summary>
    /// Windows 版原样如此, 含义不明但影响退款额, 保持不变
    public const double RemainingDivisor = 2;

    /// <summary>
    /// 换套餐时算出的"还剩多少天"
    /// </summary>
    public static double RemainingDays(DateTime endTime, DateTime now)
        => (endTime - now).TotalDays / RemainingDivisor;

    /// <summary>
    /// 剩余不足这么多天就一分不退
    /// </summary>
    public const double MinRefundDays = 0.5;

    /// <summary>
    /// 提前换套餐能退多少钱
    /// </summary>
    /// <param name="price">按当前档次重新算出的套餐价</param>
    /// <param name="durationDays">套餐总时长(天)</param>
    /// <param name="remainingDays">RemainingDays 算出的剩余</param>
    /// <returns>退款额; 算不出合理值时退 0</returns>
    /// 注意这个算式是按"已用比例"退的(duration - remaining), 也就是越接近到期退得
    /// 越多. 看着是反的, 但它是 Windows 版一直以来的行为, 改了就是改游戏平衡,
    /// 不该由迁移工作单方面决定 —— 原样搬过来.
    public static double Refund(double price, int durationDays, double remainingDays)
    {
        if (remainingDays <= MinRefundDays)
            return 0;
        if (durationDays == 0)
            return 0;
        double refund = price * (durationDays - remainingDays) / durationDays;
        if (refund < 0 || refund > price)
            return 0;
        return refund;
    }

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
