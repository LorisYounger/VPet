using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 买东西的规矩
/// </summary>
/// 从 Windows 版 winBetterBuy / MainWindow.lowStrength / everydaygift 里抽出来的那些
/// 判断和阈值, 数值一个没改. 这里只做判断, 不碰食物列表也不碰界面 —— 挑哪一份食物、
/// 弹哪个对话框仍然是各自宿主的事.
///
/// 抽出来的理由: 这些阈值原本以字面量的形式散在三个窗口里, 跨平台版重写商店时只要
/// 抄漏一个, 两边的经济系统就悄悄不一样了.
public static class PurchaseRules
{
    /// <summary>
    /// 赊账上限: 价格和经验都低于这个数才允许买不起也买
    /// </summary>
    public const double CreditLimit = 1000;

    /// <summary>
    /// 私房钱数额
    /// </summary>
    public const double LoanAmount = 1000;

    /// <summary>
    /// 钱少到这个数, 桌宠就拿私房钱出来
    /// </summary>
    public const double LoanGiveMoney = 1;

    /// <summary>
    /// 钱多到这个数, 桌宠把私房钱偷偷收回去
    /// </summary>
    public const double LoanTakeBackMoney = 11000;

    /// <summary>
    /// 自动购买要求的最低余额
    /// </summary>
    public const double AutoBuyMinMoney = 100;

    /// <summary>
    /// 自动购买最多花掉余额的多少
    /// </summary>
    public const double AutoBuyBudgetRate = 0.8;

    /// <summary>
    /// 自动购买的加价: 省事费
    /// </summary>
    public const double AutoBuyPriceRate = 1.2;

    /// <summary>
    /// 体力/饱腹低于上限的多少算"该补了"
    /// </summary>
    public const double LowRate = 0.70;

    /// <summary>
    /// 私房钱该怎么处理
    /// </summary>
    public enum LoanAction
    {
        /// <summary>什么都不用做</summary>
        None,
        /// <summary>拿私房钱出来给玩家</summary>
        Give,
        /// <summary>把私房钱偷偷收回去</summary>
        TakeBack,
        /// <summary>已经给过了, 只提醒一句可以赊账</summary>
        RemindCredit,
    }

    /// <summary>
    /// 进商店时看看要不要动私房钱
    /// </summary>
    /// <param name="money">当前金钱</param>
    /// <param name="alreadyLoaned">存档里的 self 标记: 私房钱是不是已经拿出来了</param>
    public static LoanAction CheckLoan(double money, bool alreadyLoaned)
    {
        if (money <= LoanGiveMoney)
            return alreadyLoaned ? LoanAction.RemindCredit : LoanAction.Give;
        if (money >= LoanTakeBackMoney && alreadyLoaned)
            return LoanAction.TakeBack;
        return LoanAction.None;
    }

    /// <summary>
    /// 这件东西买不买得起
    /// </summary>
    /// <param name="price">价格</param>
    /// <param name="exp">经验值</param>
    /// <param name="money">当前金钱</param>
    /// 便宜东西可以赊账, 所以只有"价格或经验达到赊账上限"的贵重物品才真的会被拦下.
    public static bool CanAfford(double price, double exp, double money)
        => !((price >= CreditLimit || exp >= CreditLimit) && price >= money);

    /// <summary>
    /// 自动购买时最缺哪一类
    /// </summary>
    public enum AutoBuyNeed
    {
        /// <summary>什么都不缺</summary>
        None,
        /// <summary>饿了, 该吃正餐</summary>
        Meal,
        /// <summary>渴了</summary>
        Drink,
        /// <summary>心情差, 买礼物</summary>
        Gift,
        /// <summary>心情差但没开自动买礼物, 退而求其次买零食</summary>
        Snack,
    }

    /// <summary>
    /// 自动购买该出手了吗, 出手买哪一类
    /// </summary>
    /// <param name="strengthFood">饱腹(含存储)</param>
    /// <param name="strengthDrink">口渴(含存储)</param>
    /// <param name="strengthMax">体力上限</param>
    /// <param name="feeling">心情</param>
    /// <param name="feelingMax">心情上限</param>
    /// <param name="money">当前金钱</param>
    /// <param name="autoGift">有没有开自动购买礼物</param>
    /// 顺序是有讲究的: 先管饿, 再管渴, 最后才管心情 —— 与 Windows 版一致.
    public static AutoBuyNeed WhatToBuy(double strengthFood, double strengthDrink, double strengthMax,
        double feeling, double feelingMax, double money, bool autoGift)
    {
        if (money < AutoBuyMinMoney)
            return AutoBuyNeed.None;
        var low = strengthMax * LowRate;
        if (strengthFood < low)
            return AutoBuyNeed.Meal;
        if (strengthDrink < low)
            return AutoBuyNeed.Drink;
        if (feeling < feelingMax * 0.50)
            return autoGift ? AutoBuyNeed.Gift : AutoBuyNeed.Snack;
        return AutoBuyNeed.None;
    }

    /// <summary>
    /// 这次自动购买最多能花多少
    /// </summary>
    public static double AutoBuyBudget(double money) => money * AutoBuyBudgetRate;

    /// <summary>
    /// 自动购买时该扣多少钱
    /// </summary>
    /// 比标价贵两成 —— 桌宠自己跑腿的辛苦费
    public static double AutoBuyCost(double price) => price * AutoBuyPriceRate;

    /// <summary>
    /// 这份食物能不能进自动购买的候选
    /// </summary>
    /// <param name="price">价格</param>
    /// <param name="health">健康影响</param>
    /// <param name="exp">经验</param>
    /// <param name="likability">好感度影响</param>
    /// <param name="budget">本次预算</param>
    /// <param name="isOverLoad">是不是超模</param>
    /// 桌宠不会自己去买负面的东西, 也不碰超模食物.
    public static bool IsAutoBuyCandidate(double price, double health, double exp,
        double likability, double budget, bool isOverLoad)
        => price >= 2 && health >= -5 && exp >= -10 && likability >= 0
            && price < budget && !isOverLoad;

    /// <summary>
    /// 自动买正餐时要求的最低饱腹回复
    /// </summary>
    public static double MealThreshold(double strengthMax) => Math.Min(strengthMax * 0.20, 100);

    /// <summary>
    /// 自动买饮料时要求的最低解渴量
    /// </summary>
    public static double DrinkThreshold(double strengthMax) => Math.Min(strengthMax * 0.20, 50);

    /// <summary>
    /// 自动买礼物时要求的最低心情回复
    /// </summary>
    public static double GiftThreshold(double feelingMax) => Math.Min(feelingMax * 0.10, 50);

    /// <summary>
    /// 自动买零食时要求的最低心情回复
    /// </summary>
    public static double SnackThreshold(double feelingMax) => Math.Min(feelingMax * 0.10, 40);

    /// <summary>
    /// 每日礼包的设置行名
    /// </summary>
    public const string DailyGiftLineName = "dailydata";

    /// <summary>
    /// 每日礼包的设置子项名
    /// </summary>
    public const string DailyGiftSubName = "everydaygift";

    /// <summary>
    /// 今天的礼包还没领吗
    /// </summary>
    /// <param name="lastGiftDayOfYear">上次领取那天是一年里的第几天</param>
    /// <param name="now">现在</param>
    /// 只比"第几天"是 Windows 版原有的做法: 隔了整一年再打开会领不到, 这个概率
    /// 低到不值得为它多存一个年份, 保持原样.
    public static bool ShouldGiveDailyGift(int lastGiftDayOfYear, DateTime now)
        => lastGiftDayOfYear != now.DayOfYear;
}
