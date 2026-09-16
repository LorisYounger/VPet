namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 食物的推荐价与超模判定
/// </summary>
/// 从 Windows 版 Food.RealPrice / IsOverLoad 抽出来的, 式子一字未改.
/// 超模判定直接决定"这份食物能不能自动购买""吃了会不会提醒玩家", 两端必须一致,
/// 否则同一份 MOD 食物在两个平台上的待遇会不一样。
public static class FoodPricing
{
    /// <summary>
    /// 按数值算出的推荐价格
    /// </summary>
    public static double RealPrice(double exp, double strength, double strengthFood,
        double strengthDrink, double feeling, double health, double likability)
        => (exp / 3 + strength / 5 + strengthDrink / 3 + strengthFood / 2 + feeling / 6) / 3
            + health + likability * 10;

    /// <summary>
    /// 超模的容错比例
    /// </summary>
    /// 定价低于推荐价的七成就算超模, 留三成容错
    public const double OverLoadRate = 0.7;

    /// <summary>
    /// 推荐价上的固定让步
    /// </summary>
    public const double OverLoadOffset = 10;

    /// <summary>
    /// 这份食物是不是超模
    /// </summary>
    /// <param name="price">标价</param>
    /// <param name="realPrice">推荐价</param>
    /// 只判"卖得太便宜", 不判"卖得太贵" —— 卖贵了坑的是玩家自己, 不影响平衡
    public static bool IsOverLoad(double price, double realPrice)
        => price < (realPrice - OverLoadOffset) * OverLoadRate;
}
