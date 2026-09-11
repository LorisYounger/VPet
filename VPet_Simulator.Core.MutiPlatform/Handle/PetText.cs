using LinePutScript.Converter;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform.Display;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 所有会被桌宠说出来的文本
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows.Interface/Mod/IText.cs.
/// 那边叫 IText, 但它其实是个普通基类而不是接口, 这里换个名字免得误导.
public class PetText
{
    /// <summary>
    /// 说话的内容
    /// </summary>
    [Line(IgnoreCase = true)] public string Text { get; set; } = string.Empty;

    private string? transText = null;
    /// <summary>
    /// 说话的内容 (翻译)
    /// </summary>
    public string TranslateText
    {
        get => transText ??= LocalizeCore.Translate(Text);
        set => transText = value;
    }

    /// <summary>
    /// 文本内容标签
    /// </summary>
    [Line(IgnoreCase = true)]
    public string Tag
    {
        get => string.Join(",", tags);
        set => tags = value.Split(',');
    }

    private string[] tags = new string[] { "all" };

    /// <summary>
    /// 查找是否符合内容标签
    /// </summary>
    public bool FindTag(string[] tags) => tags.Any(tag => this.tags.Contains(tag));

    /// <summary>
    /// 将文本转换成实际值
    /// </summary>
    public string TranslateTextConvert(IGameSave save) => ConverText(TranslateText, save);

    /// <summary>
    /// 将文本转换成实际值 (注意: 会和 Trainslate({0}) 冲突), 先 Trainslate, 再 Convert 最后再 Format
    /// </summary>
    public static string ConverText(string text, IGameSave save)
        //占位符表在共享源码里, 与 Windows 版认得出同一批
        => VPet_Simulator.Windows.Interface.SaveTextTemplate.Convert(text, save);
}

/// <summary>
/// 所有可以检查的文本格式
/// </summary>
/// 对应 Windows 版 Mod/ICheckText.cs
public abstract class CheckText : PetText
{
    [Line(ignoreCase: true)]
    public int mode { get; set; } = 7;

    /// <summary>
    /// 需求状态模式
    /// </summary>
    public ModeType Mode
    {
        get => (ModeType)mode;
        set => mode = (int)value;
    }

    /// <summary>
    /// 宠物状态模式
    /// </summary>
    [Flags]
    public enum ModeType
    {
        /// <summary>
        /// 高兴
        /// </summary>
        Happy = 1,
        /// <summary>
        /// 普通
        /// </summary>
        Nomal = 2,
        /// <summary>
        /// 状态不佳
        /// </summary>
        PoorCondition = 4,
        /// <summary>
        /// 生病(躺床)
        /// </summary>
        Ill = 8
    }

    /// <summary>
    /// 好感度要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double LikeMin { get; set; } = 0;
    /// <summary>
    /// 好感度要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double LikeMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 健康度要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double HealthMin { get; set; } = 0;
    /// <summary>
    /// 健康度要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double HealthMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 等级要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double LevelMin { get; set; } = 0;
    /// <summary>
    /// 等级要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double LevelMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 金钱要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double MoneyMin { get; set; } = int.MinValue;
    /// <summary>
    /// 金钱要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double MoneyMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 食物要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double FoodMin { get; set; } = 0;
    /// <summary>
    /// 食物要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double FoodMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 口渴要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double DrinkMin { get; set; } = 0;
    /// <summary>
    /// 口渴要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double DrinkMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 心情要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double FeelMin { get; set; } = 0;
    /// <summary>
    /// 心情要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double FeelMax { get; set; } = int.MaxValue;
    /// <summary>
    /// 体力要求:最小值
    /// </summary>
    [Line(IgnoreCase = true)] public double StrengthMin { get; set; } = 0;
    /// <summary>
    /// 体力要求:最大值
    /// </summary>
    [Line(IgnoreCase = true)] public double StrengthMax { get; set; } = int.MaxValue;

    /// <summary>
    /// 检查部分状态是否满足需求
    /// </summary>之所以不是全部的,是因为挨个取效率太差了
    public virtual bool CheckState(IGameSave save)
    {
        if (save.Likability < LikeMin || save.Likability > LikeMax)
            return false;
        if (save.Health < HealthMin || save.Health > HealthMax)
            return false;
        if (save.Level < LevelMin || save.Level > LevelMax)
            return false;
        if (save.Money < MoneyMin || save.Money > MoneyMax)
            return false;
        if (save.StrengthFood < FoodMin || save.StrengthFood > FoodMax)
            return false;
        if (save.StrengthDrink < DrinkMin || save.StrengthDrink > DrinkMax)
            return false;
        if (save.Feeling < FeelMin || save.Feeling > FeelMax)
            return false;
        if (save.Strength < StrengthMin || save.Strength > StrengthMax)
            return false;
        return true;
    }

    /// <summary>
    /// 检查部分状态是否满足需求
    /// </summary>之所以不是全部的,是因为挨个取效率太差了
    public virtual bool CheckState(PetMain m) => CheckState(m.Core.Save!);
}

/// <summary>
/// 点击桌宠时触发的乱说话
/// </summary>
/// 对应 Windows 版 Mod/ClickText.cs
public class ClickText : CheckText, IFood
{
    public ClickText()
    {
    }

    public ClickText(string text)
    {
        Text = text;
    }

    /// <summary>
    /// 指定干活时说, 空为任意, sleep 为睡觉时
    /// </summary>
    [Line(ignoreCase: true)]
    public string? Working { get; set; } = null;

    /// <summary>
    /// 日期区间
    /// </summary>
    [Flags]
    public enum DayTime
    {
        Morning = 1,
        Afternoon = 2,
        Night = 4,
        Midnight = 8,
    }

    /// <summary>
    /// 当前时间
    /// </summary>
    [Line(ignoreCase: true)]
    private int dayTime { get; set; } = 15;

    /// <summary>
    /// 日期区间
    /// </summary>
    public DayTime DaiTime
    {
        get => (DayTime)dayTime;
        set => dayTime = (int)value;
    }

    /// <summary>
    /// 工作状态
    /// </summary>
    [Line(IgnoreCase = true)]
    public PetMain.WorkingState State { get; set; } = PetMain.WorkingState.Nomal;

    /// <summary>
    /// 检查部分状态是否满足需求
    /// </summary>之所以不是全部的,是因为挨个取效率太差了
    public override bool CheckState(PetMain m)
    {
        if (!base.CheckState(m))
            return false;

        if (string.IsNullOrWhiteSpace(Working))
        {
            if (State != m.State)
                return false;
        }
        else
        {
            if (m.State != PetMain.WorkingState.Work)
                return false;
            if (m.NowWork?.Name != Working)
                return false;
        }
        return true;
    }

    [Line(ignoreCase: true)]
    public double Money { get; set; }

    [Line(ignoreCase: true)]
    public int Exp { get; set; }
    [Line(ignoreCase: true)]
    public double Strength { get; set; }
    [Line(ignoreCase: true)]
    public double StrengthFood { get; set; }
    [Line(ignoreCase: true)]
    public double StrengthDrink { get; set; }
    [Line(ignoreCase: true)]
    public double Feeling { get; set; }
    [Line(ignoreCase: true)]
    public double Health { get; set; }
    [Line(ignoreCase: true)]
    public double Likability { get; set; }
}

/// <summary>
/// 低状态自动说的话
/// </summary>
/// 对应 Windows 版 Mod/LowText.cs
public class LowText : PetText
{
    /// <summary>
    /// 状态
    /// </summary>
    public enum ModeType
    {
        /// <summary>
        /// 高状态: 开心/普通
        /// </summary>
        H,
        /// <summary>
        /// 低状态: 低状态/生病
        /// </summary>
        L,
    }

    /// <summary>
    /// 状态
    /// </summary>
    [Line(IgnoreCase = true)] public ModeType Mode { get; set; } = ModeType.L;

    /// <summary>
    /// 体力
    /// </summary>
    public enum StrengthType
    {
        /// <summary>
        /// 一般口渴/饥饿
        /// </summary>
        L,
        /// <summary>
        /// 有点口渴/饥饿
        /// </summary>
        M,
        /// <summary>
        /// 非常口渴/饥饿
        /// </summary>
        S,
    }

    /// <summary>
    /// 体力
    /// </summary>
    [Line(IgnoreCase = true)] public StrengthType Strength { get; set; } = StrengthType.S;

    /// <summary>
    /// 好感度要求
    /// </summary>
    public enum LikeType
    {
        /// <summary>
        /// 不需要好感度
        /// </summary>
        N,
        /// <summary>
        /// 低好感度需求
        /// </summary>
        S,
        /// <summary>
        /// 中好感度需求
        /// </summary>
        M,
        /// <summary>
        /// 高好感度
        /// </summary>
        L,
    }

    /// <summary>
    /// 好感度要求
    /// </summary>
    [Line(IgnoreCase = true)] public LikeType Like { get; set; } = LikeType.N;
}

/// <summary>
/// 供玩家选择说话的文本
/// </summary>
/// 对应 Windows 版 Mod/SelectText.cs. 那边直接实现 ICheckText, 这边 CheckText 是基类,
/// 状态判定沿用基类的 CheckState —— 与 ClickText 不同, 对话框选项不看时段和工作状态.
public class SelectText : CheckText, IFood
{
    /// <summary>
    /// 玩家选项名称
    /// </summary>
    [Line(IgnoreCase = true)]
    public string? Choose { get; set; } = null;

    private string? transChoose = null;
    /// <summary>
    /// 玩家选项名称 (翻译)
    /// </summary>
    public string? TranslateChoose
    {
        get
        {
            if (Choose == null)
            {
                return null;
            }
            if (transChoose == null)
            {
                transChoose = LocalizeCore.Translate(Choose);
            }
            return transChoose;
        }
        set
        {
            transChoose = value;
        }
    }
    /// <summary>
    /// 标签
    /// </summary>
    [Line(IgnoreCase = true)]
    public List<string> Tags { get; set; } = new List<string>();
    /// <summary>
    /// 查看标签是否命中
    /// </summary>
    /// <param name="totag">跳转到标签</param>
    public bool ContainsTag(IEnumerable<string> totag)
    {
        foreach (var tag in totag)
        {
            if (Tags.Contains(tag))
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 跳转到标签
    /// </summary>
    [Line(IgnoreCase = true)] public List<string> ToTags { get; set; } = new List<string>();

    [Line(ignoreCase: true)]
    public double Money { get; set; }

    [Line(ignoreCase: true)]
    public int Exp { get; set; }
    [Line(ignoreCase: true)]
    public double Strength { get; set; }
    [Line(ignoreCase: true)]
    public double StrengthFood { get; set; }
    [Line(ignoreCase: true)]
    public double StrengthDrink { get; set; }
    [Line(ignoreCase: true)]
    public double Feeling { get; set; }
    [Line(ignoreCase: true)]
    public double Health { get; set; }
    [Line(ignoreCase: true)]
    public double Likability { get; set; }
}
