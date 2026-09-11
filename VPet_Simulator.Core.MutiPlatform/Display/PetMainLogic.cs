using LinePutScript;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using VPet_Simulator.Core.MutiPlatform.Graph;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体: 游戏逻辑
/// </summary>
/// 对应 Windows 版的 VPet-Simulator.Core/Display/MainLogic.cs.
///
/// 其中的数值模拟(FunctionSpend)刻意不在这里重写, 而是转发到平台无关的
/// PetStatLogic —— 那份源码被两个平台同时编译. 这段逻辑决定了体力/心情/饱腹/
/// 健康/金钱/经验怎么随时间演化, 也就是整个游戏的平衡性, 一旦两边分叉,
/// 表现是"两个平台玩起来手感不一样", 没有任何报错、极难回溯.
public partial class PetMain : IPetStatHost
{
    /// <summary>
    /// 每隔指定时间自动触发计算 可以关闭EventTimer后手动计算
    /// </summary>
    public Timer EventTimer { get; } = new Timer(15000)
    {
        AutoReset = true,
        Enabled = false,
    };

    /// <summary>
    /// 当前状态
    /// </summary>
    public WorkingState State = WorkingState.Nomal;

    /// <summary>
    /// 当前正在的状态
    /// </summary>
    /// 与 Windows 版 Main.WorkingState 逐项对应. 共享的数值模拟里用的是 internal 的
    /// PetWorkingState, 两者按数值强制转换, 所以顺序不能改.
    public enum WorkingState
    {
        /// <summary>
        /// 默认:啥都没干
        /// </summary>
        Nomal,
        /// <summary>
        /// 正在干活/学习中
        /// </summary>
        Work,
        /// <summary>
        /// 睡觉
        /// </summary>
        Sleep,
        /// <summary>
        /// 旅游中
        /// </summary>
        Travel,
        /// <summary>
        /// 其他状态,给开发者留个空位计算
        /// </summary>
        Empty,
    }

    /// <summary>
    /// 当前工作
    /// </summary>
    public GraphHelper.Work? NowWork;

    /// <summary>
    /// 工作计时器
    /// </summary>
    public WorkTimer WorkTimer = null!;

    /// <summary>
    /// 工具栏
    /// </summary>
    public ToolBar ToolBar = null!;

    /// <summary>
    /// 工作检测
    /// </summary>
    public Func<GraphHelper.Work, bool>? WorkCheck;

    /// <summary>
    /// 开始工作
    /// </summary>
    /// <param name="work">工作内容</param>
    /// Windows 版在等级不足和生病时弹 MessageBoxX, 跨平台层没有那个对话框,
    /// 改成让桌宠自己说出来 —— 反馈仍然到位, 也不用再引一套弹窗控件.
    public bool StartWork(GraphHelper.Work? work)
    {
        if (work == null)
            return false;
        if (!Core.Controller!.EnableFunction || Core.Save!.Mode != IGameSave.ModeType.Ill)
            if (!Core.Controller!.EnableFunction || Core.Save!.Level >= work.LevelLimit)
                if (State == WorkingState.Work && NowWork?.Name == work.Name)
                    WorkTimer?.Stop(reason: WorkTimer.FinishWorkInfo.StopReason.MenualStop);
                else
                {
                    if (WorkCheck != null && !WorkCheck.Invoke(work))
                        return false;
                    WorkTimer?.Start(work);
                    return true;
                }
            else
                SayRnd(LocalizeCore.Translate("您的桌宠等级不足{0}/{2}\n无法进行{1}", Core.Save!.Level.ToString()
                    , work.NameTrans, work.LevelLimit), true);
        else
            SayRnd(LocalizeCore.Translate("您的桌宠 {0} 生病啦,没法进行{1}", Core.Save!.Name,
                work.NameTrans), true);
        return false;
    }

    /// <summary>
    /// 任务开始时调用该参数
    /// </summary>
    public event Action<GraphHelper.Work>? Event_WorkStart;

    internal void Event_WorkStartInvoke(GraphHelper.Work work)
    {
        Event_WorkStart?.Invoke(work);
    }

    /// <summary>
    /// 任务完成时调用该参数 (重定向至WorkTimer.E_FinishWork)
    /// </summary>
    public event Action<WorkTimer.FinishWorkInfo> Event_WorkEnd
    {
        add
        {
            if (WorkTimer != null)
                WorkTimer.E_FinishWork += value;
        }
        remove
        {
            if (WorkTimer != null)
                WorkTimer.E_FinishWork -= value;
        }
    }

    /// <summary>
    /// 上次和桌宠互动的时间
    /// </summary>
    public DateTime LastInteractionTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 关闭数值计算时强制显示的状态
    /// </summary>
    public IGameSave.ModeType NoFunctionMOD = IGameSave.ModeType.Happy;

    /// <summary>
    /// 每次数值结算后触发
    /// </summary>
    public event Action? FunctionSpendHandle;

    /// <summary>
    /// 每次心跳触发
    /// </summary>
    public event Action<PetMain>? TimeHandle;

    /// <summary>
    /// 状态切换时触发
    /// </summary>
    public event Action<IGameSave.ModeType, IGameSave.ModeType>? Event_ModeSwitch;

    /// <summary>
    /// 设置计算间隔
    /// </summary>
    /// <param name="Interval">计算间隔</param>
    public void SetLogicInterval(int Interval)
    {
        EventTimer.Interval = Interval;
    }

    /// <summary>
    /// 播放状态切换动画
    /// </summary>
    /// <param name="before">切换前状态</param>
    /// <param name="after">切换后状态</param>
    /// 与 Windows 版 MainLogic.PlaySwitchAnimat 逐行对应: 一级一级地播, 每播完一段
    /// 递归到下一级, 直到追上目标状态.
    public void PlaySwitchAnimat(IGameSave.ModeType before, IGameSave.ModeType after)
    {
        if (!(DisplayType?.Type == GraphInfo.GraphType.Default || DisplayType?.Type == GraphInfo.GraphType.Switch_Down
            || DisplayType?.Type == GraphInfo.GraphType.Switch_Up))
        {
            return;
        }
        if (before == after)
        {
            DisplayToNomal();
            return;
        }
        if (before < after)
        {
            Display(Core.Graph!.FindGraph(Core.Graph!.FindName(GraphInfo.GraphType.Switch_Down), GraphInfo.AnimatType.Single, before),
                () => PlaySwitchAnimat((IGameSave.ModeType)(((int)before) + 1), after));
        }
        else
        {
            Display(Core.Graph!.FindGraph(Core.Graph!.FindName(GraphInfo.GraphType.Switch_Up), GraphInfo.AnimatType.Single, before),
                () => PlaySwitchAnimat((IGameSave.ModeType)(((int)before) - 1), after));
        }
    }

    /// <summary>
    /// 根据消耗计算相关数据
    /// </summary>
    /// <param name="timePass">过去时间倍率</param>
    public void FunctionSpend(double timePass) => PetStatLogic.FunctionSpend(this, timePass);

    /// <summary>
    /// 供 MOD 挂载的随机互动
    /// </summary>
    /// 返回 true 表示这次互动被采纳, 不再尝试其它的
    public List<Func<bool>> RandomInteractionAction = new List<Func<bool>>();

    /// <summary>
    /// 是否处于可以被随机打断的空闲状态
    /// </summary>
    public bool IsIdel => (DisplayType?.Type == GraphInfo.GraphType.Default
        || DisplayType?.Type == GraphInfo.GraphType.Work) && !isPress;

    /// <summary>
    /// 心跳: 结算数值、刷新界面, 并在空闲时随机挑一个动作
    /// </summary>
    /// 与 Windows 版 MainLogic.EventTimer_Elapsed 逐行对应. 那些随机数的分支权重
    /// (0~2 移动 / 3~5 待机 / 6 待机模式一 / 7 睡觉 / 8~10 交给 MOD)决定了桌宠的
    /// 性格, 不要"优化".
    public void EventTimer_Elapsed()
    {
        //所有Handle
        TimeHandle?.Invoke(this);
        if (Core.Controller!.EnableFunction)
        {
            FunctionSpend(0.05);
        }
        else
        {
            Core.Save!.Mode = NoFunctionMOD;
        }

        //UIHandle
        RunOnUi(RaiseTimeUIHandle);

        if (IsIdel)
        {
            int rnddisplay = Math.Max(20, Core.Controller!.InteractionCycle - CountNomal);
            if (DisplayType?.Type == GraphInfo.GraphType.Work)
                rnddisplay = 2 * rnddisplay + 20;
            switch (Function.Rnd.Next(rnddisplay))
            {
                case 0:
                case 1:
                case 2:
                    //显示移动
                    DisplayMove();
                    break;
                case 3:
                case 4:
                case 5:
                    //显示待机
                    DisplayIdel();
                    break;
                case 6:
                    DisplayIdel_StateONE();
                    break;
                case 7:
                    DisplaySleep();
                    break;
                case 8:
                case 9:
                case 10:
                    //给其他显示留个机会
                    var list = RandomInteractionAction.ToList();
                    for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
                    {
                        var act = list[i];
                        if (act.Invoke())
                        {
                            break;
                        }
                        else
                        {
                            list.RemoveAt(i);
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 边缘检查和回正检测, 如果有靠边就进入侧边隐藏模式, 没有就回正
    /// </summary>
    /// <returns>是否成功进入侧边隐藏模式</returns>
    /// 平台不支持读写窗口全局坐标时(例如原生 Wayland), 控制器会让
    /// GetWindowsDistance* 返回一个足够远的值, 下面所有判断都恒不成立,
    /// 于是既不会进侧边隐藏也不会回正 —— 这正是降级模式想要的行为.
    internal bool MoveSideHideCheck()
    {
        var result = Core.Controller!.IfInActivateScreen();
        if (result == false && Core.Controller!.AutoChangeWindow == true)
        {
            Core.Controller!.SetNowScreenActivate();
        }
        //判断是否靠边,如果靠边就进入侧边隐藏模式
        if (Core.Controller!.GetWindowsDistanceLeft() < -50 * Core.Controller!.ZoomRatio)
        {
            //检查下是否有SideLoad
            if (Core.Graph!.FindName(GraphInfo.GraphType.SideHide_Left_Main) != null)
            {
                Core.Controller!.MoveWindows(-Core.Controller!.GetWindowsDistanceLeft() / Core.Controller!.ZoomRatio - Core.Graph!.GraphConfig!.Data["side"][(gdbe)"left"], 0);
                if (Core.Controller!.GetWindowsDistanceDown() < 0) Core.Controller!.MoveWindows(0, Core.Controller!.GetWindowsDistanceDown() / Core.Controller!.ZoomRatio - 100);
                else if (Core.Controller!.GetWindowsDistanceUp() < 0) Core.Controller!.MoveWindows(0, -Core.Controller!.GetWindowsDistanceUp() / Core.Controller!.ZoomRatio);
                Display(GraphInfo.GraphType.SideHide_Left_Main, GraphInfo.AnimatType.A_Start, DisplayBLoopingForce);
                return true;
            }
            else if (Core.Controller!.RePositionActive)
            {//没有就回正
                Core.Controller!.MoveWindows(-Core.Controller!.GetWindowsDistanceLeft() / Core.Controller!.ZoomRatio, 0);
            }
        }
        else if (Core.Controller!.GetWindowsDistanceRight() < -50 * Core.Controller!.ZoomRatio)
        {
            if (Core.Graph!.FindName(GraphInfo.GraphType.SideHide_Right_Main) != null)
            {
                Core.Controller!.MoveWindows(Core.Controller!.GetWindowsDistanceRight() / Core.Controller!.ZoomRatio + 500 - Core.Graph!.GraphConfig!.Data["side"][(gdbe)"right"], 0);
                if (Core.Controller!.GetWindowsDistanceDown() < 0) Core.Controller!.MoveWindows(0, Core.Controller!.GetWindowsDistanceDown() / Core.Controller!.ZoomRatio - 100);
                else if (Core.Controller!.GetWindowsDistanceUp() < 0) Core.Controller!.MoveWindows(0, -Core.Controller!.GetWindowsDistanceUp() / Core.Controller!.ZoomRatio);
                Display(GraphInfo.GraphType.SideHide_Right_Main, GraphInfo.AnimatType.A_Start, DisplayBLoopingForce);
                return true;
            }
            else if (Core.Controller!.RePositionActive)
            {
                Core.Controller!.MoveWindows(Core.Controller!.GetWindowsDistanceRight() / Core.Controller!.ZoomRatio, 0);
            }
        }
        return false;
    }

    /// <summary>
    /// 获得工作列表分类
    /// </summary>
    /// <param name="ws">所有工作</param>
    /// <param name="ss">所有学习</param>
    /// <param name="ps">所有娱乐</param>
    public void WorkList(out List<GraphHelper.Work> ws, out List<GraphHelper.Work> ss, out List<GraphHelper.Work> ps)
    {
        ws = new List<GraphHelper.Work>();
        ss = new List<GraphHelper.Work>();
        ps = new List<GraphHelper.Work>();
        foreach (var w in Core.Graph!.GraphConfig!.Works)
        {
            switch (w.Type)
            {
                case GraphHelper.Work.WorkType.Study:
                    ss.Add(w);
                    break;
                case GraphHelper.Work.WorkType.Work:
                    ws.Add(w);
                    break;
                case GraphHelper.Work.WorkType.Play:
                    ps.Add(w);
                    break;
            }
        }
    }

    // ---- 数值模拟的宿主实现 ----
    // 全部显式实现, 不占用公开名字

    IGameSave IPetStatHost.Save => Core.Save!;

    Random IPetStatHost.Rnd => Function.Rnd;

    PetWorkingState IPetStatHost.WorkingState => (PetWorkingState)State;

    IWorkDefinition? IPetStatHost.CurrentWork => NowWork;

    DateTime IPetStatHost.LastInteraction
    {
        get => LastInteractionTime;
        set => LastInteractionTime = value;
    }

    void IPetStatHost.AddWorkCount(double value)
    {
        if (WorkTimer != null)
            WorkTimer.GetCount += value;
    }

    void IPetStatHost.RaiseFunctionSpend() => FunctionSpendHandle?.Invoke();

    void IPetStatHost.PlaySwitchAnimat(IGameSave.ModeType before, IGameSave.ModeType after)
        => Event_ModeSwitch?.Invoke(before, after);

    void IPetStatHost.StopWorkByStateFail()
        // 数值结算跑在计时器线程上, 停止工作要动可视树, 必须回到 UI 线程
        => RunOnUi(() => WorkTimer?.Stop(reason: WorkTimer.FinishWorkInfo.StopReason.StateFail));
}
