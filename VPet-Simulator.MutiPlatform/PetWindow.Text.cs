using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠窗口: 说话
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows/MainWindow.cs 里 GetClickText 和
/// lowStrength 那两段. 与那边一样放在宿主而不是 Core: 说话要用到 MOD 提供的文本表,
/// 而 Core 只管动画和数值, 不知道 MOD 里有什么.
public partial class PetWindow
{
    /// <summary>
    /// 上次点击说话的时间
    /// </summary>
    private long lastclicktime;

    /// <summary>
    /// 挂上点击说话和低状态说话
    /// </summary>
    private void LoadTalk(Core.MutiPlatform.Display.PetMain m)
    {
        m.DefaultClickAction = OnPetClicked;
        m.FunctionSpendHandle += lowStrength;
    }

    /// <summary>
    /// 获得自动点击的文本
    /// </summary>
    /// <returns>说话内容</returns>
    private ClickText? GetClickText()
    {
        ClickText.DayTime dt;
        var now = DateTime.Now.Hour;
        if (now < 6)
            dt = ClickText.DayTime.Midnight;
        else if (now < 12)
            dt = ClickText.DayTime.Morning;
        else if (now < 18)
            dt = ClickText.DayTime.Afternoon;
        else
            dt = ClickText.DayTime.Night;

        CheckText.ModeType mt;
        switch (core!.Save!.Mode)
        {
            case IGameSave.ModeType.PoorCondition:
                mt = CheckText.ModeType.PoorCondition;
                break;
            default:
            case IGameSave.ModeType.Nomal:
                mt = CheckText.ModeType.Nomal;
                break;
            case IGameSave.ModeType.Happy:
                mt = CheckText.ModeType.Happy;
                break;
            case IGameSave.ModeType.Ill:
                mt = CheckText.ModeType.Ill;
                break;
        }
        var list = resources.ClickTexts.FindAll(x => x.DaiTime.HasFlag(dt) && x.Mode.HasFlag(mt) && x.CheckState(pet!));
        if (list.Count == 0)
            return null;
        return list[Function.Rnd.Next(list.Count)];
    }

    /// <summary>
    /// 点击桌宠时随机说句话
    /// </summary>
    /// 二十秒一次的冷却与 Windows 版一致: 没有这个限制的话连点几下桌宠就会
    /// 一直刷屏, 而且每句话都会加数值, 等于白送.
    private void OnPetClicked()
    {
        if (new TimeSpan(DateTime.Now.Ticks - lastclicktime).TotalSeconds <= 20)
            return;
        lastclicktime = DateTime.Now.Ticks;
        var rt = GetClickText();
        if (rt == null)
            return;
        // 说话本身也会影响数值, 这是设计如此: 有的话加好感, 有的话掉心情
        core!.Save!.EatFood(rt);
        core.Save.Money += rt.Money;
        pet!.SayRnd(rt.TranslateTextConvert(core.Save), desc: FoodToDescription(rt));
    }

    private int lowstrengthAskCountFood = 20;
    private int lowstrengthAskCountDrink = 20;

    /// <summary>
    /// 饿了渴了就吭声
    /// </summary>
    /// 与 Windows 版 lowStrength 的分支和阈值逐条对应, 只去掉了自动购买那一段 ——
    /// 那需要商店和背包, 还没移植.
    ///
    /// Rnd.Next(计数--) 这个写法看着奇怪但是有意的: 计数从互动周期开始往下减,
    /// 越久没吭声命中的概率越大, 说完一次再重置回互动周期. 不要"优化"成固定概率.
    private void lowStrength()
    {
        if (core?.Save is not IGameSave save || pet == null)
            return;
        var sm = save.StrengthMax;
        var sm75 = sm * 0.70;
        if (save.Mode == IGameSave.ModeType.Happy || save.Mode == IGameSave.ModeType.Nomal)
        {
            if (save.StrengthFood < sm75 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
            {
                lowstrengthAskCountFood = settings.InteractionCycle;
                SayLowText(resources.LowFoodTexts, LowText.ModeType.H, false,
                    save.StrengthFood, sm * 0.60, sm * 0.40);
                pet.DisplayStopForce(() => pet.Display(GraphType.Switch_Hunger, AnimatType.Single, pet.DisplayToNomal));
                return;
            }
            if (save.StrengthDrink < sm75 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
            {
                lowstrengthAskCountDrink = settings.InteractionCycle;
                SayLowText(resources.LowDrinkTexts, LowText.ModeType.H, false,
                    save.StrengthDrink, sm * 0.60, sm * 0.40);
                pet.DisplayStopForce(() => pet.Display(GraphType.Switch_Thirsty, AnimatType.Single, pet.DisplayToNomal));
                return;
            }
        }
        else
        {
            // 状态不佳或生病时话说得更早也更凶: 触发线从 0.70 降到 0.60,
            // 三个档位的分界也整体往下挪一档
            if (save.StrengthFood < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
            {
                lowstrengthAskCountFood = settings.InteractionCycle;
                SayLowText(resources.LowFoodTexts, LowText.ModeType.L, true,
                    save.StrengthFood, sm * 0.40, sm * 0.20);
                pet.DisplayStopForce(() => pet.Display(GraphType.Switch_Hunger, AnimatType.Single, pet.DisplayToNomal));
                return;
            }
            if (save.StrengthDrink < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
            {
                lowstrengthAskCountDrink = settings.InteractionCycle;
                SayLowText(resources.LowDrinkTexts, LowText.ModeType.L, true,
                    save.StrengthDrink, sm * 0.40, sm * 0.20);
                pet.DisplayStopForce(() => pet.Display(GraphType.Switch_Thirsty, AnimatType.Single, pet.DisplayToNomal));
                return;
            }
        }
    }

    /// <summary>
    /// 按当前状态挑一句低状态的话说出来
    /// </summary>
    /// <param name="texts">候选文本</param>
    /// <param name="mode">高状态还是低状态</param>
    /// <param name="likeStrict">好感度门槛是否取严格小于</param>
    /// <param name="value">当前的饱腹度/口渴度</param>
    /// <param name="high">高于这个值算"一般", 用 L 档文本</param>
    /// <param name="low">高于这个值算"有点", 用 M 档文本; 低于则用 S 档</param>
    /// Windows 版把这段按"饿"和"渴"抄了两遍、每遍里三个档位又抄了三遍, 六段代码
    /// 只差几个数. 这里合成一个方法, 阈值和挑选规则一字未改.
    ///
    /// likeStrict 是照抄过来的一处不对称: 高状态分支用的是 Like &lt;= like,
    /// 低状态分支用的是 Like &lt; like. 看着像是原作者手误, 但这会改变桌宠说什么话,
    /// 属于可观察行为, 不在迁移里顺手"修正".
    private void SayLowText(List<LowText> texts, LowText.ModeType mode, bool likeStrict,
        double value, double high, double low)
    {
        var like = core!.Save!.Likability < 40 ? 0
            : (core.Save.Likability < 70 ? 1 : (core.Save.Likability < 100 ? 2 : 3));
        var txt = texts.FindAll(x => x.Mode == mode
            && (likeStrict ? (int)x.Like < like : (int)x.Like <= like));
        if (txt.Count == 0)
            return;
        LowText.StrengthType strength;
        if (value > high)
            strength = LowText.StrengthType.L;
        else if (value > low)
            strength = LowText.StrengthType.M;
        else
            strength = LowText.StrengthType.S;
        txt = txt.FindAll(x => x.Strength == strength);
        if (txt.Count == 0)
            return;
        pet!.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(core.Save));
    }

    /// <summary>
    /// 把食物的数值变化描述成一串加号减号
    /// </summary>
    /// 对应 Windows 版 ExtensionFunction.FoodToDescription, 显示在消息栏文本下方
    private static string FoodToDescription(IFood food)
    {
        var items = new (string Name, double Value, string Mark)[]
        {
            (LocalizeCore.Translate("经验值"), food.Exp, ValueToPlusPlus(food.Exp, 1 / 4, 5)),
            (LocalizeCore.Translate("饱腹度"), food.StrengthFood, ValueToPlusPlus(food.StrengthFood, 1 / 2, 5)),
            (LocalizeCore.Translate("口渴度"), food.StrengthDrink, ValueToPlusPlus(food.StrengthDrink, 1 / 2.5, 5)),
            (LocalizeCore.Translate("体力"), food.Strength, ValueToPlusPlus(food.Strength, 1 / 4, 5)),
            (LocalizeCore.Translate("心情"), food.Feeling, ValueToPlusPlus(food.Feeling, 1 / 3, 5)),
            (LocalizeCore.Translate("健康"), food.Health, ValueToPlusPlus(food.Health, 1, 5)),
            (LocalizeCore.Translate("好感度"), food.Likability, ValueToPlusPlus(food.Likability, 1.5, 5)),
        };
        return string.Join("\n", items.Where(x => x.Value != 0).Select(x => x.Name + x.Mark));
    }

    /// <summary>
    /// 把值变成++
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="magnification">倍率</param>
    private static string ValueToPlusPlus(double value, double magnification, int max = 10)
    {
        int v = (int)Math.Abs(value);
        v = (int)(Math.Pow(v, magnification));
        v = Math.Min(Math.Max(v, 0), max);
        if (value < 0)
            return new string('-', v);
        else
            return new string('+', v);
    }
}
