using System;
using System.Globalization;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 图库解锁的日期与条件判断
/// </summary>
/// 从 Windows 版 Photo.UnlockCondition 里抽出来的. 解锁条件的载体(那个嵌套类)是
/// MOD 直接依赖的公开类型, 动不得, 所以留在原处; 挪过来的是它里面那些跟平台无关、
/// 又最容易抄歪的部分 —— 农历换算、日期窗口、节日日期表.
public static class GalleryUnlockRules
{
    /// <summary>
    /// 日期容错(天), 与 UnlockCondition.DateOffset 的默认值一致
    /// </summary>
    public const int DefaultDateOffset = 2;

    /// <summary>
    /// 时间容错(分钟), 与 UnlockCondition.TimeOffset 的默认值一致
    /// </summary>
    public const int DefaultTimeOffset = 60;

    /// <summary>
    /// 统计项缺失时按这个值算
    /// </summary>
    /// 缺失记 -1 而不是 0: 要求"至少 0 次"的条件在没有这条统计时也应当算不满足
    public const int MissingStatValue = -1;

    /// <summary>
    /// 现在是不是落在某个日子往后数几天的窗口里
    /// </summary>
    /// <param name="date">那个日子</param>
    /// <param name="now">现在</param>
    /// <param name="offsetDays">窗口有几天</param>
    public static bool InDateWindow(DateTime date, DateTime now, int offsetDays)
        => date < now && date.AddDays(offsetDays) > now;

    /// <summary>
    /// 现在是不是落在某个时刻往后数几分钟的窗口里
    /// </summary>
    /// <param name="time">那个时刻(取今天)</param>
    /// <param name="now">现在</param>
    /// <param name="offsetMinutes">窗口有几分钟</param>
    public static bool InTimeWindow(TimeOnly time, DateTime now, int offsetMinutes)
    {
        var at = new DateTime(now.Year, now.Month, now.Day, time.Hour, time.Minute, time.Second);
        return at <= now && at.AddMinutes(offsetMinutes) >= now;
    }

    /// <summary>
    /// 今年的某个农历日子是公历哪一天
    /// </summary>
    public static DateTime LunarDate(int month, int day)
        => LunarDate(month, day, DateTime.Now.Year);

    /// <summary>
    /// 指定年份的某个农历日子是公历哪一天
    /// </summary>
    public static DateTime LunarDate(int month, int day, int year)
    {
        var lunar = new ChineseLunisolarCalendar();
        return lunar.ToDateTime(year, month, day, 0, 0, 0, 0);
    }

    /// <summary>
    /// 节假日
    /// </summary>
    /// 数值与 Windows 版 Photo.UnlockCondition.HolidayType 逐项一致, 可以直接强转
    public enum HolidayKind
    {
        /// <summary>不启用</summary>
        None,
        /// <summary>中秋</summary>
        Mid_Autumn_Festival,
        /// <summary>端午</summary>
        Dragon_Boat_Festival,
        /// <summary>新年</summary>
        New_Years_Day,
        /// <summary>春节</summary>
        Spring_Festival,
        /// <summary>圣诞</summary>
        Christmas,
        /// <summary>生日(玩家)</summary>
        Player_Birthday,
        /// <summary>七夕</summary>
        Qixi_Festival,
    }

    /// <summary>
    /// 这个节今年是哪一天
    /// </summary>
    /// <returns>玩家生日和"不启用"没有固定日期, 返回 null</returns>
    public static DateTime? HolidayDate(HolidayKind kind, DateTime now) => kind switch
    {
        HolidayKind.Mid_Autumn_Festival => LunarDate(8, 15, now.Year),
        HolidayKind.Dragon_Boat_Festival => LunarDate(5, 5, now.Year),
        HolidayKind.New_Years_Day => new DateTime(now.Year, 1, 1),
        HolidayKind.Spring_Festival => LunarDate(1, 1, now.Year),
        HolidayKind.Christmas => new DateTime(now.Year, 12, 25),
        HolidayKind.Qixi_Festival => LunarDate(7, 7, now.Year),
        _ => null,
    };

    /// <summary>
    /// 今天是不是玩家生日
    /// </summary>
    /// 只比月和日, 不比年
    public static bool IsBirthday(DateTime birthday, DateTime now)
        => now.Month == birthday.Month && now.Day == birthday.Day;

    /// <summary>
    /// 这条统计满足要求了吗
    /// </summary>
    public static bool StatSatisfied(int statValue, int required) => statValue >= required;

    /// <summary>
    /// 这张照片该不该在检查时自动解锁
    /// </summary>
    /// <param name="isUnlock">已经解锁了吗</param>
    /// <param name="sellBoth">是不是"满足条件之后还要花钱"</param>
    /// <param name="conditionMet">条件满足了吗</param>
    /// 要花钱的那种不自动解锁 —— 玩家得自己去图库里买
    public static bool ShouldAutoUnlock(bool isUnlock, bool sellBoth, bool conditionMet)
        => !isUnlock && !sellBoth && conditionMet;
}
