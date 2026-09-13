using Avalonia.Controls;
using Avalonia.Media;
using LinePutScript.Localization;
using System.Linq;
using System;
using VPet_Simulator.Core.MutiPlatform.Graph;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体: 动画状态机与双缓冲渲染
/// </summary>
/// 对应 Windows 版的 VPet-Simulator.Core/Display/MainDisplay.cs.
/// 查找与切换的逻辑逐行对应, 只替换了三处平台相关实现:
///   Dispatcher.Invoke      -> RunOnUi
///   Visibility.Visible/Hidden -> IsVisible (两层是重叠的同尺寸 Decorator, Hidden 与 Collapsed 在这里没有视觉差异)
///   GC.Collect()           -> 不保留, 理由见 Display 方法内的说明
///
/// DisplayXXX 族决定了"什么时候播什么动画", 是桌宠行为的骨架. 里面大量用随机数配合
/// looptimes/CountNomal 做权重, 数值和判定顺序都不能改 —— 改了桌宠的性格就变了.
public partial class Main
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

    /// <summary>
    /// 当前显示的动画信息
    /// </summary>
    public GraphInfo? DisplayType { get; private set; }

    /// <summary>
    /// 连续播放普通动画的次数
    /// </summary>
    public int CountNomal { get; set; }

    /// <summary>
    /// 显示过的动画
    /// </summary>
    public event Action<GraphInfo>? GraphDisplayHandler;

    private bool petgridcrlf = true;
    private int nodisplayLoop = 0;

    /// <summary>
    /// 显示动画 (自动查找和匹配)
    /// </summary>
    /// <param name="type">动画类型</param>
    /// <param name="animat">动画的动作 Start Loop End</param>
    /// <param name="endAction">动画结束后操作(附带名字)</param>
    public void Display(GraphType type, AnimatType animat, Action<string>? endAction = null)
    {
        var name = Core.Graph!.FindName(type);
        if (name != null)
        {
            Display(name, animat, endAction);
        }
        else
        {
            endAction?.Invoke("");
        }
    }

    /// <summary>
    /// 显示动画 根据名字播放
    /// </summary>
    /// <param name="name">动画名称</param>
    /// <param name="animat">动画的动作 Start Loop End</param>
    /// <param name="endAction">动画结束后操作(附带名字)</param>
    public void Display(string? name, AnimatType animat, Action<string>? endAction)
    {
        Display(Core.Graph!.FindGraph(name, animat, Core.Save!.Mode), new Action(() => endAction?.Invoke(name ?? "")));
    }

    /// <summary>
    /// 显示动画 根据名字和类型查找运行,若无则查找类型
    /// </summary>
    /// <param name="name">动画名称</param>
    /// <param name="animat">动画的动作 Start Loop End</param>
    /// <param name="type">动画类型</param>
    /// <param name="endAction">动画结束后操作(附带名字)</param>
    public void Display(string name, AnimatType animat, GraphType type, Action<string>? endAction = null)
    {
        var list = Core.Graph!.FindGraphs(name, animat, Core.Save!.Mode)?.FindAll(x => x.GraphInfo.Type == type);
        if ((list?.Count ?? -1) > 0)
            Display(list![Function.Rnd.Next(list.Count)], () => endAction?.Invoke(name));
        else
            Display(type, animat, endAction);
    }

    /// <summary>
    /// 显示动画 根据名字和类型查找运行,若无则查找类型
    /// </summary>
    /// <param name="name">动画名称</param>
    /// <param name="animat">动画的动作 Start Loop End</param>
    /// <param name="type">动画类型</param>
    /// <param name="endAction">动画结束后操作</param>
    public void Display(string name, AnimatType animat, GraphType type, Action? endAction = null)
    {
        var list = Core.Graph!.FindGraphs(name, animat, Core.Save!.Mode)?.FindAll(x => x.GraphInfo.Type == type);
        if ((list?.Count ?? -1) > 0)
            Display(list![Function.Rnd.Next(list.Count)], endAction);
        else
            Display(type, animat, endAction);
    }

    /// <summary>
    /// 显示动画 (自动查找和匹配)
    /// </summary>
    /// <param name="type">动画类型</param>
    /// <param name="animat">动画的动作 Start Loop End</param>
    /// <param name="endAction">动画结束后操作</param>
    public void Display(GraphType type, AnimatType animat, Action? endAction = null)
    {
        var name = Core.Graph!.FindName(type);
        Display(name, animat, endAction);
    }

    /// <summary>
    /// 显示动画 根据名字播放
    /// </summary>
    /// <param name="name">动画名称</param>
    /// <param name="animat">动画的动作 Start Loop End</param>
    /// <param name="endAction">动画结束后操作</param>
    public void Display(string? name, AnimatType animat, Action? endAction = null)
    {
        Display(Core.Graph!.FindGraph(name, animat, Core.Save!.Mode), endAction);
    }

    /// <summary>
    /// 显示带图片的动画 (投喂)
    /// </summary>
    /// <param name="name">动画名称</param>
    /// <param name="img">要塞进动画里的图片, 例如食物</param>
    /// <param name="endAction">动画结束后操作</param>
    /// 与 Windows 版 MainDisplay.Display(string, ImageSource, Action) 对应.
    /// 找不到动画时不能把 endAction 吞掉, 否则桌宠会永远停在上一个动作上.
    public void Display(string name, IImage img, Action endAction)
    {
        var ig = Core.Graph!.FindGraph(name, AnimatType.Single, Core.Save!.Mode);
        if (ig is IAvaloniaRunImageGraph rig)
        {
            var b = FindDisplayBorder(ig);
            rig.Run(b, img, endAction);
        }
        else
        {
            endAction();
        }
    }

    /// <summary>
    /// 显示动画 (自动多层切换)
    /// </summary>
    /// <param name="graph">动画</param>
    /// <param name="endAction">结束操作</param>
    public void Display(IAvaloniaGraph? graph, Action? endAction = null)
    {
        if (graph == null)
        {
            if (nodisplayLoop++ > 20)
            {//无动画时运行兼容性动画
                if (nodisplayLoop < 100)
                    Display(GraphType.Default, AnimatType.Single, endAction);
                else
                {//连Nomal都没有, 证明是未完成的动画, 提示并停止桌宠模块
                    RunOnUi(() =>
                    {
                        LabelDisplayText.Text = "未找到可播放动画, 已停止运行桌宠模块".Translate();
                        LabelDisplay.IsVisible = true;
                        IsEnabled = false;
                    });
                }
            }
            else
                endAction?.Invoke();
            return;
        }
        else
        {
            nodisplayLoop = 0;
        }

        DisplayType = graph.GraphInfo;
        GraphDisplayHandler?.Invoke(graph.GraphInfo);
        var petGridTag = RunOnUi(() => PetGrid.Tag);
        var petGrid2Tag = RunOnUi(() => PetGrid2.Tag);
        if (graph.Equals(petGridTag))
        {
            petgridcrlf = true;
            if (petGrid2Tag is IAvaloniaGraph ig)
                ig.Stop(true);
            RunOnUi(() =>
            {
                PetGrid.IsVisible = true;
                PetGrid2.IsVisible = false;
            });
            graph.Run(PetGrid, endAction);
            return;
        }
        else if (graph.Equals(petGrid2Tag))
        {
            petgridcrlf = false;
            if (petGridTag is IAvaloniaGraph ig)
                ig.Stop(true);
            RunOnUi(() =>
            {
                PetGrid2.IsVisible = true;
                PetGrid.IsVisible = false;
            });
            graph.Run(PetGrid2, endAction);
            return;
        }

        // 顺序不可调整: 先让新动画在隐藏的那一层上跑起来渲染出首帧, 再停掉旧动画,
        // 最后才翻转可见性. 这是双缓冲不闪烁的关键.
        if (petgridcrlf)
        {
            graph.Run(PetGrid2, endAction);
            (petGridTag as IAvaloniaGraph)?.Stop(true);
            RunOnUi(() =>
            {
                PetGrid.IsVisible = false;
                PetGrid2.IsVisible = true;
            });
        }
        else
        {
            graph.Run(PetGrid, endAction);
            (petGrid2Tag as IAvaloniaGraph)?.Stop(true);
            RunOnUi(() =>
            {
                PetGrid2.IsVisible = false;
                PetGrid.IsVisible = true;
            });
        }
        petgridcrlf = !petgridcrlf;
        // Windows 版在这里有一次 GC.Collect(), 用来压制 WPF BitmapSource 的堆积.
        // 跨平台侧的动画走的是共享一张雪碧图的 SkiaSharp 实现, 没有那个堆积来源,
        // 而每次切换动画都强制 GC 在 Linux 上是明显的卡顿来源, 所以不保留.
    }

    /// <summary>
    /// 查找可用与显示的 Decorator (自动多层切换)
    /// </summary>
    /// <param name="graph">动画</param>
    public Decorator FindDisplayBorder(IAvaloniaGraph graph)
    {
        DisplayType = graph.GraphInfo;
        var petGridTag = RunOnUi(() => PetGrid.Tag);
        var petGrid2Tag = RunOnUi(() => PetGrid2.Tag);
        if (ReferenceEquals(petGridTag, graph))
        {
            petgridcrlf = true;
            (petGrid2Tag as IAvaloniaGraph)?.Stop(true);
            RunOnUi(() =>
            {
                PetGrid.IsVisible = true;
                PetGrid2.IsVisible = false;
            });
            return PetGrid;
        }
        else if (ReferenceEquals(petGrid2Tag, graph))
        {
            petgridcrlf = false;
            (petGridTag as IAvaloniaGraph)?.Stop(true);
            RunOnUi(() =>
            {
                PetGrid2.IsVisible = true;
                PetGrid.IsVisible = false;
            });
            return PetGrid2;
        }

        if (petgridcrlf)
        {
            (petGridTag as IAvaloniaGraph)?.Stop(true);
            RunOnUi(() =>
            {
                PetGrid.IsVisible = false;
                PetGrid2.IsVisible = true;
            });
            petgridcrlf = !petgridcrlf;
            return PetGrid2;
        }
        else
        {
            (petGrid2Tag as IAvaloniaGraph)?.Stop(true);
            RunOnUi(() =>
            {
                PetGrid2.IsVisible = false;
                PetGrid.IsVisible = true;
            });
            petgridcrlf = !petgridcrlf;
            return PetGrid;
        }
    }

    /// <summary>
    /// 停止两层上正在播放的动画
    /// </summary>
    internal void StopCurrentGraphs()
    {
        if (RunOnUi(() => PetGrid.Tag) is IAvaloniaGraph first)
            first.Stop(true);
        if (RunOnUi(() => PetGrid2.Tag) is IAvaloniaGraph second)
            second.Stop(true);
    }
}
