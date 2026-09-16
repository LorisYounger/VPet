using System;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 桌宠的数值存档
/// </summary>
/// 对应 VPet_Simulator.Core 的 IGameSave, 成员逐项一致. 之所以重新声明一遍而不是
/// 直接引用, 见 Enums.cs 开头的说明.
public interface IPetSave
{
    /// <summary>桌宠名字</summary>
    string Name { get; set; }
    /// <summary>主人名字</summary>
    string HostName { get; set; }

    /// <summary>金钱</summary>
    double Money { get; set; }
    /// <summary>经验值</summary>
    double Exp { get; set; }
    /// <summary>经验值获取倍率</summary>
    double ExpBonus { get; }
    /// <summary>等级</summary>
    int Level { get; }
    /// <summary>升级所需经验</summary>
    int LevelUpNeed();

    /// <summary>体力</summary>
    double Strength { get; set; }
    /// <summary>体力上限</summary>
    double StrengthMax { get; }
    /// <summary>储存的体力</summary>
    double StoreStrength { get; set; }
    /// <summary>本次结算体力的变化量</summary>
    double ChangeStrength { get; set; }
    /// <summary>改变体力</summary>
    void StrengthChange(double value);

    /// <summary>饱腹度</summary>
    double StrengthFood { get; set; }
    /// <summary>储存的饱腹度</summary>
    double StoreStrengthFood { get; set; }
    /// <summary>本次结算饱腹度的变化量</summary>
    double ChangeStrengthFood { get; set; }
    /// <summary>改变饱腹度</summary>
    void StrengthChangeFood(double value);

    /// <summary>口渴度</summary>
    double StrengthDrink { get; set; }
    /// <summary>储存的口渴度</summary>
    double StoreStrengthDrink { get; set; }
    /// <summary>本次结算口渴度的变化量</summary>
    double ChangeStrengthDrink { get; set; }
    /// <summary>改变口渴度</summary>
    void StrengthChangeDrink(double value);

    /// <summary>心情</summary>
    double Feeling { get; set; }
    /// <summary>心情上限</summary>
    double FeelingMax { get; }
    /// <summary>本次结算心情的变化量</summary>
    double ChangeFeeling { get; set; }
    /// <summary>改变心情</summary>
    void FeelingChange(double value);

    /// <summary>健康</summary>
    double Health { get; set; }
    /// <summary>好感度</summary>
    double Likability { get; set; }
    /// <summary>好感度上限</summary>
    double LikabilityMax { get; }

    /// <summary>清空本次结算的变化量</summary>
    void CleanChange();
    /// <summary>把储存的数值取出来一点</summary>
    void StoreTake();

    /// <summary>当前状态模式</summary>
    PetModeType Mode { get; set; }
    /// <summary>按当前数值算出该处于什么状态</summary>
    PetModeType CalMode();
}

/// <summary>
/// 统计数据
/// </summary>
/// 对应 VPet-Simulator.Windows.Interface 的 Statistics. 图鉴解锁条件、成就、年度
/// 报告都读它, 所以即便宿主没有统计界面也必须保留, 否则存档往返会丢玩家的记录.
public interface IPetStatistics
{
    int GetInt(string name, int defaultValue = 0);
    void SetInt(string name, int value);
    long GetInt64(string name, long defaultValue = 0);
    void SetInt64(string name, long value);
    double GetDouble(string name, double defaultValue = 0);
    void SetDouble(string name, double value);
    bool GetBool(string name);
    void SetBool(string name, bool value);
    DateTime GetDateTime(string name, DateTime defaultValue = default);
    void SetDateTime(string name, DateTime value);
    string? GetString(string name, string? defaultValue = null);
    void SetString(string name, string? value);

    /// <summary>全部统计项的名字</summary>
    System.Collections.Generic.IEnumerable<string> Keys { get; }

    /// <summary>某一项被改动时触发</summary>
    event Action<string>? Changed;
}
