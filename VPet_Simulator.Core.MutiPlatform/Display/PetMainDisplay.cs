using Avalonia.Controls;
using Avalonia.Media;
using System;
using VPet_Simulator.Core.MutiPlatform.Graph;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体: 动画状态机与双缓冲渲染
/// </summary>
/// 对应 Windows 版的 VPet-Simulator.Core/Display/MainDisplay.cs.
/// 查找与切换的逻辑逐行对应, 只替换了三处平台相关实现:
///   Dispatcher.Invoke      -> Dispatcher.UIThread.Invoke
///   Visibility.Visible/Hidden -> IsVisible (两层是重叠的同尺寸 Decorator,
///                                Hidden 与 Collapsed 在这里没有视觉差异)
///   GC.Collect()           -> 不保留, 理由见 Display 方法内的说明
public partial class PetMain
{
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
                        LabelDisplayText.Text = "未找到可播放动画, 已停止运行桌宠模块";
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
