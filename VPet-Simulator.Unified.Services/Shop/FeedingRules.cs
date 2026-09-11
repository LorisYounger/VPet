using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 投喂的吃腻度与统计
/// </summary>
/// 从 Windows 版 MainWindow.TakeItem 里抽出来的. 吃腻度决定同一样东西连着吃会
/// 越吃越没用, 是投喂手感的核心; 统计项名则决定年度报告和成就能不能对上号 ——
/// 两端各写一遍必然会歪。
public static class FeedingRules
{
    /// <summary>
    /// 记录每样东西"腻到什么时候"的存档行名
    /// </summary>
    public const string BuyTimeLineName = "buytime";

    /// <summary>
    /// 吃腻之后最少还能吃出几成效果
    /// </summary>
    public const double MinEffectiveness = 0.5;

    /// <summary>
    /// 食物类型
    /// </summary>
    /// 数值与 Windows 版 Food.FoodType 和跨平台版 FoodItem.FoodType 逐项一致
    public enum FoodKind
    {
        /// <summary>食物 (默认)</summary>
        Food,
        /// <summary>收藏 (自定义)</summary>
        Star,
        /// <summary>正餐</summary>
        Meal,
        /// <summary>零食</summary>
        Snack,
        /// <summary>饮料</summary>
        Drink,
        /// <summary>功能性</summary>
        Functional,
        /// <summary>药品</summary>
        Drug,
        /// <summary>礼品</summary>
        Gift,
    }

    /// <summary>
    /// 上次吃这样东西留下的"腻"还剩几小时
    /// </summary>
    /// <param name="boredUntil">存档里记的"腻到什么时候"</param>
    /// <param name="now">现在</param>
    public static double RemainingBoredom(DateTime boredUntil, DateTime now)
        => boredUntil > now ? (boredUntil - now).TotalHours : 0;

    /// <summary>
    /// 这一口能吃出几成效果
    /// </summary>
    /// <param name="boredomHours">还剩几小时的腻</param>
    /// <param name="isGift">礼品腻得慢一些</param>
    /// 平方衰减, 但兜底在五成 —— 再腻也不至于白吃
    public static double Effectiveness(double boredomHours, bool isGift)
        => Math.Max(MinEffectiveness, 1 - boredomHours * boredomHours * (isGift ? 0.01 : 0.02));

    /// <summary>
    /// 吃完之后再往上加多少小时的腻
    /// </summary>
    /// <param name="likability">这样东西加多少好感</param>
    /// <param name="feeling">这样东西加多少心情</param>
    /// 越讨喜的东西腻得越慢, 但不会低于半小时也不会高于四小时
    public static double AddedBoredom(double likability, double feeling)
        => Math.Max(0.5, Math.Min(4, 2 - (likability + feeling / 2) / 5));

    // ---- 统计项名 ----

    /// <summary>买了多少次</summary>
    public const string BuyTimesStat = "stat_buytimes";

    /// <summary>一共花了多少钱</summary>
    public const string TotalSpendStat = "stat_betterbuy";

    /// <summary>某样东西买了多少次</summary>
    public static string BuyCountStat(string foodName) => "buy_" + foodName;

    /// <summary>
    /// 按类型分开记的花费统计项名
    /// </summary>
    /// <returns>这个类型不单独记账时返回 null</returns>
    /// 只有"收藏"没有专属账本 —— 与 Windows 版一致
    public static string? SpendStat(FoodKind kind) => kind switch
    {
        FoodKind.Food => "stat_bb_food",
        FoodKind.Drink => "stat_bb_drink",
        FoodKind.Drug => "stat_bb_drug",
        FoodKind.Snack => "stat_bb_snack",
        FoodKind.Functional => "stat_bb_functional",
        FoodKind.Meal => "stat_bb_meal",
        FoodKind.Gift => "stat_bb_gift",
        _ => null,
    };

    /// <summary>药品累计加了多少经验</summary>
    public const string DrugExpStat = "stat_bb_drug_exp";

    /// <summary>礼品累计加了多少好感</summary>
    public const string GiftLikeStat = "stat_bb_gift_like";
}
