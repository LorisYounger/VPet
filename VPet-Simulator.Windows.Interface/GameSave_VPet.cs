using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Windows.Threading;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.IGameSave;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 游戏存档 桌宠桌面端订制版本
/// </summary>
public class GameSave_VPet : IGameSave
{
    /// <summary>
    /// 宠物名字
    /// </summary>
    [Line(name: "name")]
    public string Name { get; set; }
    /// <summary>
    /// 主人称呼
    /// </summary>
    [Line(name: "hostname")]
    public string HostName { get; set; } = "";
    /// <summary>
    /// 金钱
    /// </summary>
    [Line(Type = LPSConvert.ConvertType.ToFloat, Name = "money")]
    public double Money { get; set; }


    double exp { get; set; }
    /// <summary>
    /// 等级
    /// </summary>
    [Line]
    public int Level { get; set; } = 1;
    /// <summary>
    /// 等级上限
    /// </summary>
    [Line]
    public int LevelMax { get; set; } = 0;
    /// <summary>
    /// 经验值
    /// </summary>
    [Line(type: LPSConvert.ConvertType.ToFloat, name: "exp")]
    public double Exp
    {
        get => exp;
        set
        {
            int lun = LevelUpNeed();
            bool islevelup = false;
            bool islevelmaxup = false;
            int BeforeLevel = Level;
            int BeforeLevelMax = LevelMax;
            while (value >= lun)
            {
                islevelup = true;
                value -= lun;
                LikabilityMax += 10;
                if (++Level > 1000 + LevelMax * 100)
                {
                    LevelMax++;
                    islevelmaxup = true;
                    Level = 100 * LevelMax;
                }
                lun = LevelUpNeed();
            }
            exp = value;
            if (islevelup)
            {
                Event_LevelUp?.Invoke(new LevelUpEventArgs()
                {
                    IsLevelMaxUp = islevelmaxup,
                    BeforeLevel = BeforeLevel,
                    BeforeLevelMax = BeforeLevelMax
                });
            }
        }
    }
    public class LevelUpEventArgs : EventArgs
    {
        /// <summary>
        /// 是否升级
        /// </summary>
        public bool IsLevelUp => true;
        /// <summary>
        /// 是否升级指上限
        /// </summary>
        public bool IsLevelMaxUp { get; set; }
        /// <summary>
        /// 之前的等级
        /// </summary>
        public int BeforeLevel { get; set; }
        /// <summary>
        /// 之前的等级上限
        /// </summary>
        public int BeforeLevelMax { get; set; }
    }
    public event Action<LevelUpEventArgs> Event_LevelUp;
    /// <summary>
    /// 玩家总共获得的经验值数量
    /// </summary>
    public double TotalExpGained()
    {
        double totalExp = 0;
        // 首先，添加LevelMax的经验值
        for (int i = 1; i <= LevelMax; i++)
        {
            for (int j = 100 * i + 1; j <= 1000 + 100 * i; j++)
                totalExp += 200 * j - 100;
        }
        // 然后，添加当前等级的经验值
        totalExp += (Level - 100 * LevelMax) * (200 * (Level - 1) - 100);
        // 最后，添加剩余的经验值
        totalExp += Exp;
        return totalExp;
    }
    /// <summary>
    /// 升级所需经验值
    /// </summary>
    public int LevelUpNeed() => 200 * Level - 100;
    /// <summary>
    /// 体力 0-100
    /// </summary>
    public double Strength { get => strength; set => strength = Math.Min(StrengthMax, Math.Max(0, value)); }

    public double StrengthMax => 100 + (int)(Math.Pow(Level * (1 + LevelMax), 0.75) * 4);

    [Line(Type = LPSConvert.ConvertType.ToFloat, IgnoreCase = true)]
    protected double strength { get; set; }
    /// <summary>
    /// 待补充的体力,随着时间缓慢加给桌宠
    /// </summary>//让游戏更有游戏性
    [Line(Type = LPSConvert.ConvertType.ToFloat, IgnoreCase = true)]
    public double StoreStrength { get; set; }
    /// <summary>
    /// 变化 体力
    /// </summary>
    public double ChangeStrength { get; set; } = 0;
    public void StrengthChange(double value)
    {
        ChangeStrength += value;
        Strength += value;
    }
    /// <summary>
    /// 饱腹度
    /// </summary>
    public double StrengthFood
    {
        get => strengthFood; set
        {
            value = Math.Min(StrengthMax, value);
            if (value <= 0)
            {
                Health += value;
                strengthFood = 0;
            }
            else
                strengthFood = value;
        }
    }
    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double strengthFood { get; set; }
    /// <summary>
    /// 待补充的饱腹度,随着时间缓慢加给桌宠
    /// </summary>//让游戏更有游戏性
    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    public double StoreStrengthFood { get; set; }
    public void StrengthChangeFood(double value)
    {
        ChangeStrengthFood += value;
        StrengthFood += value;
    }
    /// <summary>
    /// 变化 食物
    /// </summary>
    public double ChangeStrengthFood { get; set; } = 0;
    /// <summary>
    /// 口渴度
    /// </summary>
    public double StrengthDrink
    {
        get => strengthDrink; set
        {
            value = Math.Min(StrengthMax, value);
            if (value <= 0)
            {
                Health += value;
                strengthDrink = 0;
            }
            else
                strengthDrink = value;
        }
    }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double strengthDrink { get; set; }
    /// <summary>
    /// 待补充的口渴度,随着时间缓慢加给桌宠
    /// </summary>//让游戏更有游戏性
    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    public double StoreStrengthDrink { get; set; }
    /// <summary>
    /// 变化 口渴度
    /// </summary>
    public double ChangeStrengthDrink { get; set; } = 0;
    public void StrengthChangeDrink(double value)
    {
        ChangeStrengthDrink += value;
        StrengthDrink += value;
    }
    /// <summary>
    /// 心情
    /// </summary>
    public double Feeling
    {
        get => feeling; set
        {

            value = Math.Min(FeelingMax, value);
            if (value <= 0)
            {
                Health += value / 2;
                Likability += value / 2;
                feeling = 0;
            }
            else
                feeling = value;
        }
    }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double feeling { get; set; }
    /// <summary>
    /// 变化 心情
    /// </summary>
    public double ChangeFeeling { get; set; } = 0;
    public void FeelingChange(double value)
    {
        ChangeFeeling += value;
        Feeling += value;
    }
    /// <summary>
    /// 健康(生病)(隐藏)
    /// </summary>
    public double Health { get => health; set => health = Math.Min(100, Math.Max(0, value)); }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double health { get; set; }
    /// <summary>
    /// 好感度(隐藏)(累加值)
    /// </summary>
    public double Likability
    {
        get => likability; set
        {
            var max = LikabilityMax;
            value = Math.Max(0, value);
            if (value > max)
            {
                likability = max;
                Health += value - max;
            }
            else
                likability = value;
        }
    }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double likability { get; set; }

    /// <summary>
    /// 清除变化
    /// </summary>
    public void CleanChange()
    {
        ChangeStrength /= 2;
        ChangeFeeling /= 2;
        ChangeStrengthDrink /= 2;
        ChangeStrengthFood /= 2;
    }
    /// <summary>
    /// 取回被储存的体力
    /// </summary>
    public void StoreTake()
    {
        const int t = 10;

        var s = StoreStrength / t;
        StoreStrength -= s;
        if (Math.Abs(StoreStrength) < 1)
            StoreStrength = 0;
        else
            StrengthChange(s);

        s = StoreStrengthDrink / t;
        StoreStrengthDrink -= s;
        if (Math.Abs(StoreStrengthDrink) < 1)
            StoreStrengthDrink = 0;
        else
            StrengthChangeDrink(s);

        s = StoreStrengthFood / t;
        StoreStrengthFood -= s;
        if (Math.Abs(StoreStrengthFood) < 1)
            StoreStrengthFood = 0;
        else
            StrengthChangeFood(s);
    }
    /// <summary>
    /// 吃食物
    /// </summary>
    /// <param name="food">食物类</param>
    public void EatFood(IFood food)
    {
        Exp += food.Exp;
        var tmp = food.Strength / 2;
        StrengthChange(tmp);
        StoreStrength += tmp;
        tmp = food.StrengthFood / 2;
        StrengthChangeFood(tmp);
        StoreStrengthFood += tmp;
        tmp = food.StrengthDrink / 2;
        StrengthChangeDrink(tmp);
        StoreStrengthDrink += tmp;
        FeelingChange(food.Feeling);
        Health += food.Health;
        Likability += food.Likability;
    }
    /// <summary>
    /// 宠物当前状态
    /// </summary>
    [Line(name: "mode")]
    public ModeType Mode { get; set; } = ModeType.Nomal;
    [Line]
    public double LikabilityMax { get; set; } = 100;

    public double FeelingMax => 100 + (int)(Math.Pow(Level * (1 + LevelMax), 0.75) * 2);
    /// <summary>
    /// 经验值加成 TODO
    /// </summary>
    public double ExpBonus { get; set; } = 1;

    /// <summary>
    /// 计算宠物当前状态
    /// </summary>
    public ModeType CalMode()
    {
        int realhel = 60 - (Feeling / FeelingMax >= 80 ? 12 : 0) - (Likability >= 80 ? 12 : (Likability >= 40 ? 6 : 0));
        //先从最次的开始
        if (Health <= realhel)
        {
            //可以确认从状态不佳和生病二选一
            if (Health <= realhel / 2)
            {//生病
                return ModeType.Ill;
            }
            else
            {
                return ModeType.PoorCondition;
            }
        }
        //然后判断是高兴还是普通
        double realfel = .90 - (Likability >= 80 ? .20 : (Likability >= 40 ? .10 : 0));
        double felps = Feeling / FeelingMax;
        if (felps >= realfel)
        {
            return ModeType.Happy;
        }
        else if (felps <= realfel / 2)
        {
            return ModeType.PoorCondition;
        }
        return ModeType.Nomal;
    }
    /// <summary>
    /// 新游戏
    /// </summary>
    public GameSave_VPet(string name)
    {
        Name = name;
        Money = 100;
        Exp = 0;
        Strength = 100;
        StrengthFood = 100;
        StrengthDrink = 100;
        Feeling = 60;
        Health = 100;
        Likability = 0;
        Mode = CalMode();
    }
    /// <summary>
    /// 读档
    /// </summary>
    public GameSave_VPet()
    {
    }
    /// <summary>
    /// 读档
    /// </summary>
    public static GameSave_VPet Load(ILine data) => LPSConvert.DeserializeObject<GameSave_VPet>(data);
    /// <summary>
    /// 存档
    /// </summary>
    /// <returns>存档行</returns>
    public Line ToLine()
    {
        //Line save = new Line("vpet", Name);
        //save.SetFloat("money", Money);
        //save.SetInt("exp", Exp);
        //save.SetFloat("strength", Strength);
        //save.SetFloat("strengthdrink", StrengthDrink);
        //save.SetFloat("strengthfood", StrengthFood);
        //save.SetFloat("feeling", Feeling);
        //save.SetFloat("health", Health);
        //save.SetFloat("Likability", Likability);
        return LPSConvert.SerializeObject(this, "vpet");
    }

}
