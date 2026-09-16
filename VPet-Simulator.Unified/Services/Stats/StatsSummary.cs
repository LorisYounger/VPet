using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 年度报告里的算式与分档
/// </summary>
/// 从 Windows 版 winCharacterPanel.GenRank 里抽出来的. 那里是"阈值 + 台词"一层层
/// 写死的 if-else, 台词属于界面, 阈值属于规则 —— 这里只拿阈值, 台词留在各自宿主.
///
/// 分档一律从 1 开始, 与 Windows 版那些 xxx_i 变量的取值逐一对应(报告图上的星级
/// 就是按这个数画的).
public static class StatsSummary
{
    /// <summary>
    /// 按阈值梯子算出档位
    /// </summary>
    /// <param name="value">要评的值</param>
    /// <param name="thresholds">从小到大的阈值</param>
    /// <returns>1 到 阈值个数+1</returns>
    /// 语义与 Windows 版那一串 if (v &lt; a) ... else if (v &lt; b) ... 完全一致:
    /// 小于第一个阈值是 1 档, 依次类推, 全都不小于就是最后一档.
    public static int Tier(double value, params double[] thresholds)
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value < thresholds[i])
                return i + 1;
        }
        return thresholds.Length + 1;
    }

    /// <summary>
    /// 把排行榜名次折算成 0~1 的百分位
    /// </summary>
    /// <param name="globalRank">名次, 没上榜传 null</param>
    /// <param name="entryCount">榜上总人数</param>
    /// <returns>越大越靠前; 没上榜给 0</returns>
    public static double RankPercentile(int? globalRank, double entryCount)
    {
        if (globalRank == null || entryCount <= 0)
            return 0;
        return 1 - (globalRank.Value - 1) / entryCount;
    }

    // ---- 派生指标 ----

    /// <summary>秒换算成小时</summary>
    public static double Hours(long seconds) => seconds / 3600.0;

    /// <summary>平均每天陪伴多少小时</summary>
    public static double HoursPerDay(double hours, double days) => days <= 0 ? 0 : hours / days;

    /// <summary>工作时长占总在线时长的比例</summary>
    public static double WorkRatio(long workSeconds, long totalSeconds)
        => totalSeconds <= 0 ? 0 : (double)workSeconds / totalSeconds;

    /// <summary>自动购买占总购买次数的比例</summary>
    public static double AutoBuyRatio(int autoBuyTimes, int buyTimes)
        => buyTimes <= 0 ? 0 : (double)autoBuyTimes / buyTimes;

    // ---- 各项分档 ----

    /// <summary>陪伴时长的排行百分位低于这个数就是"多陪陪我"</summary>
    public const double CompanionRankSplit = 0.5;

    /// <summary>每天陪伴多少小时 → 1(同学) 到 5(女鹅)</summary>
    public static int CompanionTier(double hoursPerDay) => Tier(hoursPerDay, 2, 4, 7, 10);

    /// <summary>桌宠等级 → 1(小学) 到 5(砖家)</summary>
    public static int StudyTier(int level) => Tier(level, 20, 40, 60, 80);

    /// <summary>单次学习收益的排行百分位 → 5(垫底) 到 1(顶尖)</summary>
    /// 注意方向: 这一项的档位是倒过来的, 百分位越高档位数越小
    public static int StudyExpTier(double rankPercentile) => 6 - Tier(rankPercentile, 0.25, 0.4, 0.55, 0.75);

    /// <summary>单次打工收益的排行百分位 → 4(垫底) 到 1(顶尖)</summary>
    public static int WorkMoneyTier(double rankPercentile) => 5 - Tier(rankPercentile, 0.25, 0.5, 0.75);

    /// <summary>打工时长占比的排行百分位 → 1(摸鱼) 到 6(卷王)</summary>
    public static int WorkTimeTier(double rankPercentile) => Tier(rankPercentile, 0.25, 0.35, 0.45, 0.55, 0.75);

    /// <summary>自动购买占比 → 4(亲力亲为) 到 1(全自动)</summary>
    public static int AutoBuyTier(double autoBuyRatio) => 5 - Tier(autoBuyRatio, 0.25, 0.5, 0.75);

    /// <summary>创意工坊 MOD 数的排行百分位 → 3(没装) 到 1(大师)</summary>
    /// <param name="modCount">装了几个 MOD</param>
    /// <param name="rankPercentile">排行百分位</param>
    /// 一个都没装时直接给 3 档, 不看百分位 —— 与 Windows 版一致
    public static int ModTier(int modCount, double rankPercentile)
        => modCount == 0 ? 3 : 4 - Tier(rankPercentile, 0.3, 0.7);

    /// <summary>好感度超过这个数就显示那个特殊图标</summary>
    public const double LikabilityIconThreshold = 100;

    // ---- 移动距离 ----

    /// <summary>
    /// 一英寸多少像素
    /// </summary>
    public const double DpiPerInch = 96;

    /// <summary>
    /// 把像素折算成合适的长度单位
    /// </summary>
    /// <param name="px">像素</param>
    /// <returns>数值和单位; 数值已经按 Windows 版的位数格式化过</returns>
    /// 阈值就是各单位的换算点: 1cm ≈ 37795px, 1km ≈ 377952755px.
    public static (string Value, string Unit) LengthFromPixels(long px)
    {
        if (px < 37795)
            return (px.ToString(), "px");
        if (px < 3779527)
            return ((px * 2.54 / DpiPerInch).ToString("f1"), "cm");
        if (px < 377952755)
            return ((px * 2.54 / (DpiPerInch * 100)).ToString("f1"), "m");
        return ((px * 2.54 / (DpiPerInch * 100000)).ToString("f1"), "km");
    }
}
