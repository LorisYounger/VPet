using LinePutScript.Localization;
using System;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform.Graph;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体: 动画状态机
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Core/Display/MainDisplay.cs 里的 DisplayXXX 族.
/// 逐方法对应, 只替换平台相关的三处: Dispatcher 换成 RunOnUi、IGraph 换成
/// IAvaloniaGraph、本地化换成跨平台版的 LocalizeCore.
///
/// 这些方法决定了"什么时候播什么动画", 是桌宠行为的骨架. 里面大量用随机数配合
/// looptimes/CountNomal 做权重, 数值和判定顺序都不能改 —— 改了桌宠的性格就变了.
public partial class PetMain
{
    private int looptimes;

    /// <summary>
    /// 以标准形式显示当前默认状态
    /// </summary>
    public void DisplayToNomal()
    {
        switch (State)
        {
            default:
            case WorkingState.Nomal:
                DisplayNomal();
                return;
            case WorkingState.Sleep:
                DisplaySleep(true);
                return;
            case WorkingState.Work:
                NowWork?.Display(this);
                return;
            case WorkingState.Travel:
                //TODO
                return;
        }
    }

    /// <summary>
    /// 显示默认情况, 默认为默认动画
    /// </summary>
    public Action DisplayNomal { get; set; }

    /// <summary>
    /// 尝试触发移动
    /// </summary>
    public Func<bool> DisplayMove { get; set; }

    /// <summary>
    /// 显示待机情况 (只有符合条件的才会显示)
    /// </summary>
    public Func<bool> DisplayIdel { get; set; }

    /// <summary>
    /// 显示待机(模式1)情况
    /// </summary>
    public Action DisplayIdel_StateONE { get; set; }

    /// <summary>
    /// 显示摸头情况
    /// </summary>
    public Action DisplayTouchHead { get; set; }

    /// <summary>
    /// 显示摸身体情况
    /// </summary>
    public Action DisplayTouchBody { get; set; }

    /// <summary>
    /// 显示默认动画
    /// </summary>
    public void DisplayDefault()
    {
        CountNomal++;
        Display(GraphType.Default, AnimatType.Single, DisplayNomal);
    }

    /// <summary>
    /// 显示结束动画
    /// </summary>
    /// <param name="endAction">结束后接下来,不结束不运行</param>
    /// <returns>是否成功结束</returns>
    public bool DisplayStop(Action endAction)
    {
        var graph = Core.Graph!.FindGraph(DisplayType?.Name, AnimatType.C_End, Core.Save!.Mode);
        if (graph != null)
        {
            if (State == WorkingState.Sleep)
                State = WorkingState.Nomal;
            Display(graph, endAction);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 显示结束动画 无论是否结束,都强制结束
    /// </summary>
    /// <param name="endAction">结束后接下来,不结束也运行</param>
    public void DisplayStopForce(Action endAction)
    {
        if (!DisplayStop(endAction))
            endAction?.Invoke();
    }

    /// <summary>
    /// 尝试触发移动
    /// </summary>
    public bool DisplayToMove()
    {
        var list = Core.Graph!.GraphConfig!.Moves.ToList();
        for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
        {
            var move = list[i];
            if (move.Triggered(this))
            {
                move.Display(this);
                return true;
            }
            else
            {
                list.RemoveAt(i);
            }
        }
        return false;
    }

    /// <summary>
    /// 当发生摸头时触发改方法
    /// </summary>
    public event Action? Event_TouchHead;

    /// <summary>
    /// 显示摸头情况
    /// </summary>
    public void DisplayToTouchHead()
    {
        CountNomal = 0;
        if (Core.Controller!.EnableFunction && Core.Save!.Strength >= 10 && Core.Save!.Feeling < Core.Save!.FeelingMax)
        {
            Core.Save!.StrengthChange(-2);
            Core.Save!.FeelingChange(1);
            Core.Save!.Mode = Core.Save!.CalMode();
            LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
        }
        if (DisplayType?.Type == GraphType.Touch_Head)
        {
            if (DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (DisplayType.Animat == AnimatType.B_Loop)
                if (RunOnUi(() => PetGrid.Tag) is IAvaloniaGraph ig
                    && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (RunOnUi(() => PetGrid2.Tag) is IAvaloniaGraph ig2
                    && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        Event_TouchHead?.Invoke();
        Display(GraphType.Touch_Head, AnimatType.A_Start, (graphname) =>
           Display(graphname, AnimatType.B_Loop, (graphname) =>
           DisplayCEndtoNomal(graphname)));
    }

    /// <summary>
    /// 当发生摸身体时触发改方法
    /// </summary>
    public event Action? Event_TouchBody;

    /// <summary>
    /// 显示摸身体情况
    /// </summary>
    public void DisplayToTouchBody()
    {
        CountNomal = 0;
        if (Core.Controller!.EnableFunction && Core.Save!.Strength >= 10 && Core.Save!.Feeling < Core.Save!.FeelingMax)
        {
            Core.Save!.StrengthChange(-2);
            Core.Save!.FeelingChange(1);
            Core.Save!.Mode = Core.Save!.CalMode();
            LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
        }
        if (DisplayType?.Type == GraphType.Touch_Body)
        {
            if (DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (DisplayType.Animat == AnimatType.B_Loop)
                if (RunOnUi(() => PetGrid.Tag) is IAvaloniaGraph ig
                    && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (RunOnUi(() => PetGrid2.Tag) is IAvaloniaGraph ig2
                    && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        Event_TouchBody?.Invoke();
        Display(GraphType.Touch_Body, AnimatType.A_Start, (graphname) =>
         Display(graphname, AnimatType.B_Loop, (graphname) =>
         DisplayCEndtoNomal(graphname)));
    }

    /// <summary>
    /// 显示待机(模式1)情况
    /// </summary>
    public void DisplayToIdel_StateONE()
    {
        looptimes = 0;
        CountNomal = 0;
        var name = Core.Graph!.FindName(GraphType.StateONE);
        if (name == null)
        {
            DisplayIdel();
            return;
        }
        var list = Core.Graph!.FindGraphs(name, AnimatType.A_Start, Core.Save!.Mode)?
            .FindAll(x => x.GraphInfo.Type == GraphType.StateONE);
        if (list != null && list.Count > 0)
            Display(list[Function.Rnd.Next(list.Count)], () => DisplayIdel_StateONEing(name));
        else
            DisplayIdel();
    }

    /// <summary>
    /// 显示待机(模式1)情况
    /// </summary>
    private void DisplayIdel_StateONEing(string graphname)
    {
        if (Function.Rnd.Next(++looptimes) > Core.Graph!.GraphConfig!.GetDuration(graphname))
            switch (Function.Rnd.Next(2 + CountNomal))
            {
                case 0:
                    DisplayIdel_StateTWO(graphname);
                    break;
                default:
                    Display(graphname, AnimatType.C_End, GraphType.StateONE, DisplayNomal);
                    break;
            }
        else
        {
            Display(graphname, AnimatType.B_Loop, GraphType.StateONE, DisplayIdel_StateONEing);
        }
    }

    /// <summary>
    /// 显示待机(模式2)情况
    /// </summary>
    public void DisplayIdel_StateTWO(string graphname)
    {
        looptimes = 0;
        CountNomal++;
        Display(graphname, AnimatType.A_Start, GraphType.StateTWO, DisplayIdel_StateTWOing);
    }

    /// <summary>
    /// 显示待机(模式2)情况
    /// </summary>
    private void DisplayIdel_StateTWOing(string graphname)
    {
        if (Function.Rnd.Next(++looptimes) > Core.Graph!.GraphConfig!.GetDuration(graphname))
        {
            looptimes = 0;
            Display(graphname, AnimatType.C_End, GraphType.StateTWO, DisplayIdel_StateONEing);
        }
        else
        {
            Display(graphname, AnimatType.B_Loop, GraphType.StateTWO, DisplayIdel_StateTWOing);
        }
    }

    /// <summary>
    /// 显示待机情况 (只有符合条件的才会显示)
    /// </summary>
    public bool DisplayToIdel()
    {
        if (Core.Graph!.GraphsName.TryGetValue(GraphType.Idel, out var gl))
        {
            var list = gl.ToList();
            for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
            {
                var idelname = list[i];
                var ig = Core.Graph!.FindGraphs(idelname, AnimatType.A_Start, Core.Save!.Mode);
                if (ig != null && ig.Count != 0)
                {
                    looptimes = 0;
                    CountNomal = 0;
                    Display(ig[Function.Rnd.Next(ig.Count)], () =>
                    DisplayBLoopingToNomal(idelname, Core.Graph!.GraphConfig!.GetDuration(idelname)));
                    return true;
                }
                else
                {
                    ig = Core.Graph!.FindGraphs(idelname, AnimatType.Single, Core.Save!.Mode);
                    if (ig != null && ig.Count != 0)
                    {
                        looptimes = 0;
                        CountNomal = 0;
                        Display(ig[Function.Rnd.Next(ig.Count)], DisplayToNomal);
                        return true;
                    }
                    list.RemoveAt(i);
                }
            }
            return false;
        }
        else
            return false;
    }

    /// <summary>
    /// 显示B循环+C循环+ToNomal
    /// </summary>
    public Action<string?> DisplayBLoopingToNomal(int looplength) => (gn) => DisplayBLoopingToNomal(gn, looplength);

    /// <summary>
    /// 显示B循环+C循环+ToNomal
    /// </summary>
    public void DisplayBLoopingToNomal(string? graphname, int loopLength)
    {
        if (Function.Rnd.Next(++looptimes) > loopLength)
            DisplayCEndtoNomal(graphname);
        else
            Display(graphname, AnimatType.B_Loop, DisplayBLoopingToNomal(loopLength));
    }

    /// <summary>
    /// 显示睡觉情况
    /// </summary>
    public void DisplaySleep(bool force = false)
    {
        looptimes = 0;
        CountNomal = 0;
        if (force)
        {
            State = WorkingState.Sleep;
            Display(GraphType.Sleep, AnimatType.A_Start, DisplayBLoopingForce);
        }
        else
            Display(GraphType.Sleep, AnimatType.A_Start, (x) => DisplayBLoopingToNomal(x, Core.Graph!.GraphConfig!.GetDuration(x)));
    }

    /// <summary>
    /// 显示B循环 (强制)
    /// </summary>
    public void DisplayBLoopingForce(string graphname)
    {
        Display(graphname, AnimatType.B_Loop, DisplayBLoopingForce);
    }

    //显示工作现在直接由显示调用,没有DisplayWork, 学习同理

    /// <summary>
    /// 显示结束动画到正常动画 (DisplayToNomal)
    /// </summary>
    public void DisplayCEndtoNomal(string? graphname)
    {
        Display(graphname, AnimatType.C_End, DisplayToNomal);
    }
}
