using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;
using VPet_Simulator.Core.MutiPlatform.Graph;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体
/// </summary>
/// 对应 Windows 版的 VPet_Simulator.Core.Main. 拆成三个部分:
///   PetMain.axaml.cs        生命周期与对外状态
///   PetMainDisplay.cs       动画状态机与双缓冲渲染
///
/// 与 Windows 版的差异只在平台相关的那几处(Dispatcher / 可见性 / 语音),
/// 动画选择和状态流转的逻辑必须逐行一致, 否则两个平台的桌宠行为会悄悄分叉.
public partial class PetMain : UserControl, GraphHelper.IMoveHost, IDisposable
{
    /// <summary>
    /// 游戏核心
    /// </summary>
    public GameCore Core { get; }

    /// <summary>
    /// 是否已开始运行
    /// </summary>
    public bool IsWorking { get; private set; }

    /// <summary>
    /// 每秒触发的界面刷新事件
    /// </summary>
    public event Action<PetMain>? TimeUIHandle;

    /// <summary>
    /// 无参构造仅供 Avalonia 设计器使用
    /// </summary>
    public PetMain() : this(new GameCore())
    {
    }

    public PetMain(GameCore core)
    {
        Core = core;
        InitializeComponent();

        MoveTimer = new Timer();
        MoveTimer.Elapsed += MoveTimer_Elapsed;
        EventTimer.Elapsed += (_, _) => EventTimer_Elapsed();
        SmartMoveTimer.Elapsed += SmartMoveTimer_Elapsed;
        labeldisplaytimer.Elapsed += Labledisplaytimer_Elapsed;
        Event_MoveEnd += (_) => MoveSideHideCheck();
        AttachPointerEvents();

        // 这几个都是可替换的委托, MOD 可以换掉它们来改变桌宠的默认行为.
        // 与 Windows 版 Main 构造函数里的绑定一一对应.
        DisplayNomal = DisplayDefault;
        DisplayMove = DisplayToMove;
        DisplayIdel = DisplayToIdel;
        DisplayIdel_StateONE = DisplayToIdel_StateONE;
        DisplayTouchBody = DisplayToTouchBody;
        DisplayTouchHead = DisplayToTouchHead;

        // 说话时随机挑一个表情: 优先用 Say 类型的动画, 没有就退回默认动画
        SayRndFunction = new Func<string, string>((x) =>
            Core.Graph?.FindName(GraphInfo.GraphType.Say) ?? Core.Graph?.FindName(GraphInfo.GraphType.Default) ?? "");

        Load_0_BaseConsole();
    }

    /// <summary>
    /// 加载动画时遇到的错误
    /// </summary>
    public List<string> ErrorMessage = new List<string>();

    /// <summary>
    /// 等待所有动画加载完成
    /// </summary>
    /// <param name="waitCountAction">已等待完成的动画个数, 两秒回调一次</param>
    /// 与 Windows 版 Main.Load_2_WaitGraph 逐行对应. 加载失败的动画会从
    /// GraphsList 里摘掉并把原因记进 ErrorMessage —— 之前宿主里那段内联的等待
    /// 循环只会干等到超时, 既不摘也不记, 一张坏图就能让整只桌宠卡在加载界面.
    public async Task Load_2_WaitGraph(Action<int>? waitCountAction = null)
    {
        int count = 0;
        DateTime start = DateTime.Now.AddSeconds(2);
        var tasks = new List<Task>();

        foreach (var igs in Core.Graph!.GraphsList.Values)
        {
            foreach (var ig2 in igs.Values)
            {
                for (int i = 0; i < ig2.Count; i++)
                {
                    var ig3 = ig2[i];
                    tasks.Add(Task.Run(async () =>
                    {
                        while (!ig3.IsReady)
                        {
                            if (ig3.IsFail)
                            {
                                lock (ErrorMessage) // 确保线程安全
                                {
                                    ErrorMessage.Add(ig3.FailMessage);
                                    ig2.Remove(ig3);
                                }
                                break;
                            }
                            else
                            {
                                await Task.Delay(100);
                            }
                        }
                        System.Threading.Interlocked.Increment(ref count);
                        if (waitCountAction != null && start < DateTime.Now)
                        {
                            start = DateTime.Now.AddSeconds(2);
                            waitCountAction.Invoke(count);
                        }
                    }));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 开始运行桌宠
    /// </summary>
    public void Start()
    {
        if (IsWorking)
            return;
        IsWorking = true;
        EventTimer.Enabled = true;
        DisplayToNomal();
    }

    /// <summary>
    /// 停止运行桌宠
    /// </summary>
    public void Stop()
    {
        IsWorking = false;
        EventTimer.Enabled = false;
        MoveTimer.Enabled = false;
        StopCurrentGraphs();
    }

    /// <summary>
    /// 触发界面刷新
    /// </summary>
    public void RaiseTimeUIHandle() => TimeUIHandle?.Invoke(this);

    /// <summary>
    /// 释放计时器等资源
    /// </summary>
    /// 桌宠一共有四个后台计时器(心跳/移动/智能移动/提示气泡淡出)加上消息栏的三个,
    /// 都是 System.Timers.Timer, 不显式释放的话宿主关闭后线程池回调还会继续跑.
    public void Dispose()
    {
        Stop();
        ToolBar?.Dispose();
        EventTimer.Dispose();
        MoveTimer.Dispose();
        SmartMoveTimer.Dispose();
        labeldisplaytimer.Dispose();
        MsgBar?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ---------------- 移动 ----------------

    /// <summary>
    /// 移动计时器
    /// </summary>
    /// 与 Windows 版一致用 System.Timers.Timer (线程池回调), 而不是 UI 线程计时器:
    /// 动画帧循环本来就跑在后台线程上, 保持一致才不会引入新的时序差异.
    public Timer MoveTimer { get; }

    /// <summary>
    /// 每次移动的位移
    /// </summary>
    public Point MoveTimerPoint { get; set; }

    /// <summary>
    /// 是否启用智能移动
    /// </summary>
    public bool MoveTimerSmartMove { get; private set; }

    private readonly Timer SmartMoveTimer = new Timer(20 * 60)
    {
        AutoReset = true,
    };

    /// <summary>
    /// 是否启用智能移动
    /// </summary>
    private bool SmartMove;

    /// <summary>
    /// 设置移动模式
    /// </summary>
    /// <param name="AllowMove">允许移动</param>
    /// <param name="smartMove">启用智能移动</param>
    /// <param name="SmartMoveInterval">智能移动周期</param>
    public void SetMoveMode(bool AllowMove, bool smartMove, int SmartMoveInterval)
    {
        MoveTimer.Enabled = false;
        if (AllowMove)
        {
            MoveTimerSmartMove = true;
            if (smartMove)
            {
                SmartMoveTimer.Interval = SmartMoveInterval;
                SmartMoveTimer.Start();
                SmartMove = true;
            }
            else
            {
                SmartMoveTimer.Enabled = false;
                SmartMove = false;
            }
        }
        else
        {
            MoveTimerSmartMove = false;
        }
    }

    private void SmartMoveTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        MoveTimerSmartMove = false;
    }

    private void MoveTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (DisplayType?.Type != GraphInfo.GraphType.Move || !MoveTimerSmartMove)
        {
            MoveTimer.Enabled = false;
            return;
        }
        var controller = Core.Controller;
        if (controller == null)
            return;
        // 平台不支持窗口移动时(例如原生 Wayland), 控制器的实现会把 MoveWindows
        // 做成空操作, 这里不需要额外判断
        controller.MoveWindows(MoveTimerPoint.X, MoveTimerPoint.Y);
    }

    void GraphHelper.IMoveHost.Event_MoveStartInvoke(GraphHelper.Move move) => Event_MoveStart?.Invoke(move);

    void GraphHelper.IMoveHost.Event_MoveEndInvoke(GraphHelper.Move move) => Event_MoveEnd?.Invoke(move);

    /// <summary>
    /// 开始移动时触发
    /// </summary>
    public event Action<GraphHelper.Move>? Event_MoveStart;

    /// <summary>
    /// 结束移动时触发
    /// </summary>
    public event Action<GraphHelper.Move>? Event_MoveEnd;

    // ---------------- 提示气泡 ----------------

    private int labeldisplaycount = 100;
    private int labeldisplayhash = 0;
    private readonly Timer labeldisplaytimer = new Timer(10)
    {
        AutoReset = true,
    };
    private double labeldisplaychangenum1 = 0;
    private double labeldisplaychangenum2 = 0;

    /// <summary>
    /// 显示消息弹窗Label
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="time">持续时间</param>
    public void LabelDisplayShow(string text, int time = 2000)
    {
        labeldisplayhash = text.GetHashCode();
        RunOnUi(() =>
        {
            LabelDisplayText.Text = text;
            LabelDisplay.Opacity = 1;
            LabelDisplay.IsVisible = true;
            labeldisplaycount = time / 10;
            labeldisplaytimer.Start();
        });
    }

    /// <summary>
    /// 显示消息弹窗Lable,自动统计数值变化
    /// </summary>
    /// <param name="text">文本, 使用{0:f2}</param>
    /// <param name="changenum1">变化值1</param>
    /// <param name="changenum2">变化值2</param>
    /// <param name="time">持续时间</param>
    /// 同一句提示在短时间内反复出现时(例如连续摸头), 数值是累加显示的,
    /// 靠文本的哈希判断是不是同一句. 与 Windows 版行为一致.
    public void LabelDisplayShowChangeNumber(string text, double changenum1, double changenum2 = 0, int time = 2000)
    {
        if (labeldisplayhash == text.GetHashCode())
        {
            labeldisplaychangenum1 += changenum1;
            labeldisplaychangenum2 += changenum2;
        }
        else
        {
            labeldisplaychangenum1 = changenum1;
            labeldisplaychangenum2 = changenum2;
            labeldisplayhash = text.GetHashCode();
        }
        RunOnUi(() =>
        {
            LabelDisplayText.Text = string.Format(text, labeldisplaychangenum1, labeldisplaychangenum2);
            LabelDisplay.Opacity = 1;
            LabelDisplay.IsVisible = true;
            labeldisplaycount = time / 10;
            labeldisplaytimer.Start();
        });
    }

    private void Labledisplaytimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (--labeldisplaycount <= 0)
        {
            labeldisplaytimer.Enabled = false;
            labeldisplaychangenum1 = 0;
            labeldisplaychangenum2 = 0;
            RunOnUi(() => LabelDisplay.IsVisible = false);
        }
        else if (labeldisplaycount < 50)
        {
            RunOnUi(() => LabelDisplay.Opacity = labeldisplaycount / 50.0);
        }
    }

    /// <summary>
    /// 隐藏提示文本
    /// </summary>
    public void LabelDisplayHide() => RunOnUi(() => LabelDisplay.IsVisible = false);

    /// <summary>
    /// 在 UI 线程上同步执行
    /// </summary>
    /// 已经在 UI 线程时 Avalonia 会直接内联执行, 与 WPF 的 Dispatcher.Invoke 行为一致
    internal static void RunOnUi(Action action) => Dispatcher.UIThread.Invoke(action);

    /// <summary>
    /// 在 UI 线程上同步取值
    /// </summary>
    internal static T RunOnUi<T>(Func<T> func) => Dispatcher.UIThread.Invoke(func);
}
