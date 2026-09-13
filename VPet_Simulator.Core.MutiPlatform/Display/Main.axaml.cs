using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using System;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Core.MutiPlatform.Graph;
using VPet_Simulator.Unified.Interface;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体
/// </summary>
/// 对应 Windows 版的 VPet-Simulator.Core/Display/Main.xaml.cs, 按那边的切法分三个文件:
///   Main.axaml.cs     生命周期、指针输入、语音
///   MainDisplay.cs    动画状态机与双缓冲渲染
///   MainLogic.cs      游戏逻辑与说话
///
/// 与 Windows 版的差异只在平台相关的那几处 (Dispatcher / 可见性 / 语音 / 指针事件),
/// 动画选择和状态流转的逻辑必须逐行一致, 否则两个平台的桌宠行为会悄悄分叉.
///
/// 指针输入的一处实现差异: Windows 版是在后台线程睡够长按时长之后, 再用 Mouse.GetPosition
/// 去查当前鼠标位置 (而且用的是 BeginInvoke(...).Wait(), 在 Avalonia 上会死锁). Avalonia 没有
/// 全局指针位置查询, 这里改为在指针事件里记下最后位置, 长按判定时直接读 —— 语义一致.
///
/// 语音: 桌宠自己不带音频后端 (Avalonia 没有内置的), 定成统一契约里的 IVoicePlayer 由语音 MOD
/// 实现, 这边只负责转发和口型同步; 没装语音 MOD 时一切照常, 只是不出声.
public partial class Main : UserControl, GraphHelper.IMoveHost, IDisposable
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
    public event Action<Main>? TimeUIHandle;

    /// <summary>
    /// 无参构造仅供 Avalonia 设计器使用
    /// </summary>
    public Main() : this(new GameCore())
    {
    }

    public Main(GameCore core)
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
    /// 开始运行桌宠: 播完启动动画再回到默认动画
    /// </summary>
    /// 与 Windows 版 Main.Load_4_Start 逐行对应 (计时器在构造函数里就绑好了, 这里只把心跳打开)
    public void Load_4_Start(IAvaloniaGraph? startUPGraph = null)
    {
        IAvaloniaGraph? ig = startUPGraph ?? Core.Graph!.FindGraph(Core.Graph!.FindName(GraphType.StartUP), AnimatType.Single, Core.Save!.Mode);
        ig ??= Core.Graph!.FindGraph(Core.Graph!.FindName(GraphType.Default), AnimatType.Single, Core.Save!.Mode);
        if (ig == null)
        {
            MessageBoxX.Show("Did not find the Default animation, please check the graph configuration.", "Error", MessageBoxButton.OK, MessageBoxIcon.Error);
            return;
        }
        Task.Run(() =>
        {
            ig.Run(PetGrid, () =>
            {
                DisplayNomal();
            });
        });

        IsWorking = true;
        EventTimer.Enabled = true;
    }

    /// <summary>
    /// 等待图像加载和开始
    /// </summary>
    public void Load_24_WaitAndStart()
    {
        Load_2_WaitGraph().Wait();
        Dispatcher.UIThread.Invoke(() => Load_4_Start());
    }
    /// <summary>
    /// 等待图像加载和开始
    /// </summary>
    /// <param name="WaitCountAction">当前已等待图像个数</param>
    /// <param name="startUPGraph">开始运行初始动画</param>
    public void Load_24_WaitAndStart(Action<int> WaitCountAction, IAvaloniaGraph? startUPGraph = null)
    {
        Load_2_WaitGraph(WaitCountAction).Wait();
        Dispatcher.UIThread.Invoke(() => Load_4_Start(startUPGraph));
    }

    /// <summary>
    /// 开始运行桌宠 (不播启动动画, 直接到默认动画)
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

    /// <summary>
    /// 默认点击事件
    /// </summary>
    public Action? DefaultClickAction;

    /// <summary>
    /// 默认长按事件
    /// </summary>
    public Action? DefaultPressAction;

    /// <summary>
    /// 右键点击事件
    /// </summary>
    public Action? DefaultRightClickAction;

    /// <summary>
    /// 是否正按着
    /// </summary>
    public bool isPress = false;

    private long presstime;

    /// <summary>
    /// 指针在桌宠坐标系(500x500)里的最后位置
    /// </summary>
    private Point lastPointerPosition;

    /// <summary>
    /// 自动加载触摸事件
    /// </summary>
    /// 必须在 Core.Graph 就绪之后调用, 触摸区域是从宠物配置里读的
    public void Load_2_TouchEvent()
    {
        //让侧挂回正
        Core.TouchEvent.Add(new TouchArea(new Point(0, 0), new Size(500, 500), () =>
        {
            if (DisplayType?.Type == GraphType.SideHide_Left_Main || DisplayType?.Type == GraphType.SideHide_Left_Rise)
            {
                Core.Controller!.MoveWindows(-Core.Controller!.GetWindowsDistanceLeft() / Core.Controller!.ZoomRatio, 0);
                DisplayCEndtoNomal(Core.Graph!.FindName(GraphType.SideHide_Left_Main));
                return true;
            }
            if (DisplayType?.Type == GraphType.SideHide_Right_Main || DisplayType?.Type == GraphType.SideHide_Right_Rise)
            {
                Core.Controller!.MoveWindows(Core.Controller!.GetWindowsDistanceRight() / Core.Controller!.ZoomRatio, 0);
                DisplayCEndtoNomal(Core.Graph!.FindName(GraphType.SideHide_Right_Main));
                return true;
            }
            return false;
        }));
        Core.TouchEvent.Add(new TouchArea(Core.Graph!.GraphConfig!.TouchHeadLocate, Core.Graph!.GraphConfig!.TouchHeadSize,
            () => { DisplayTouchHead(); return true; }));
        Core.TouchEvent.Add(new TouchArea(Core.Graph!.GraphConfig!.TouchBodyLocate, Core.Graph!.GraphConfig!.TouchBodySize,
            () => { DisplayTouchBody(); return true; }));
        for (int i = 0; i < 4; i++)
        {
            IGameSave.ModeType m = (IGameSave.ModeType)i;
            Core.TouchEvent.Add(new TouchArea(Core.Graph!.GraphConfig!.TouchRaisedLocate[i], Core.Graph!.GraphConfig!.TouchRaisedSize[i],
                () =>
                {
                    if (Core.Save!.Mode == m)
                    {
                        DisplayRaised();
                        return true;
                    }
                    else
                        return false;
                }, true));
        }
    }

    /// <summary>
    /// 挂上指针事件
    /// </summary>
    private void AttachPointerEvents()
    {
        MainGrid.PointerPressed += MainGrid_PointerPressed;
        MainGrid.PointerReleased += MainGrid_MouseLeftButtonUp;
        MainGrid.PointerMoved += MainGrid_PointerMoved;
        MainGrid.PointerEntered += MainGrid_MouseEnter;
        MainGrid.PointerExited += MainGrid_MouseLeave;
    }

    /// <summary>
    /// 这个指针事件是不是从工具栏 (含它的弹出菜单) 里冒上来的
    /// </summary>
    /// WPF 的 Menu 会把按下标记 Handled, 事件到不了 MainGrid; Avalonia 的菜单只对有子菜单的项
    /// 这么做, 叶子项的按下会一路冒到这里, 被 Capture(MainGrid) 抢走捕获之后 MenuItem 就收不到
    /// Click 了 —— 表现是"菜单点了没反应, 桌宠反而说话". 弹出层挂在 Popup 的逻辑树下,
    /// 所以按逻辑树判
    private bool FromToolBar(RoutedEventArgs e)
        => ToolBar != null && e.Source is ILogical source && ToolBar.IsLogicalAncestorOf(source);

    //跨平台: Avalonia 只有一个 PointerPressed, 按哪个键在这里分流到 Windows 版同名的两个处理器
    private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled || FromToolBar(e))
            return;
        var point = e.GetCurrentPoint(MainGrid);
        lastPointerPosition = point.Position;

        if (point.Properties.IsRightButtonPressed)
        {
            MainGrid_MouseRightButtonDown(sender, e);
            return;
        }
        if (point.Properties.IsLeftButtonPressed)
            MainGrid_MouseLeftButtonDown(sender, e);
    }

    private void MainGrid_MouseRightButtonDown(object? sender, PointerPressedEventArgs e)
    {
        if (ToolBar?.IsVisible == true)
            ToolBar.Hide();
        else
            ToolBar?.Show();
        DefaultRightClickAction?.Invoke();
    }

    private void MainGrid_MouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
    {
        e.Pointer.Capture(MainGrid);
        isPress = true;
        CountNomal = 0;
        Task.Run(() =>
        {
            var pth = DateTime.Now.Ticks;
            presstime = pth;
            Thread.Sleep(Core.Controller!.PressLength);
            var mp = lastPointerPosition;
            if (isPress && presstime == pth)
            {//历遍长按事件
                LastInteractionTime = DateTime.Now;
                foreach (var x in Core.TouchEvent)
                {
                    if (x.IsPress == true && x.Touch(mp) && x.DoAction())
                        return;
                }
                DefaultPressAction?.Invoke();
            }
            else
            {//历遍点击事件
                LastInteractionTime = DateTime.Now;
                foreach (var x in Core.TouchEvent)
                {
                    if (x.IsPress == false && x.Touch(mp) && x.DoAction())
                        return;
                }
                //普通点击验证
                if (DisplayType?.Type != GraphType.Default)
                {//不是nomal! 可能会卡timer,所有全部timer清空下
                    CleanState();
                    if (!IsIdel && State != WorkingState.Sleep && DisplayStop(DisplayToNomal))
                        return;
                }
                DefaultClickAction?.Invoke();
            }
        });
    }

    private void MainGrid_MouseLeftButtonUp(object? sender, PointerReleasedEventArgs e)
    {
        if (FromToolBar(e))
            return;
        isPress = false;
        e.Pointer.Capture(null);
        if (DisplayType != null && DisplayType.Type.ToString().StartsWith("Raised", StringComparison.Ordinal))
        {
            rasetype = -1;
            DisplayRaising();
        }
        else if (SmartMove)
        {
            MoveTimerSmartMove = true;
            SmartMoveTimer.Enabled = false;
            SmartMoveTimer.Start();
        }
    }

    private void MainGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (FromToolBar(e))
            return;
        lastPointerPosition = e.GetPosition(MainGrid);
        if (!isPress)
        {
            // Windows 版是在提起/放下时来回换 MouseMove 的处理器, 这里合成一个:
            // 没按着就是"滑动摸头", 按着就是"拖动"
            MainGrid_MouseWave(e);
            return;
        }
        // 只有正处于"被提起"状态时才跟随指针移动窗口
        if (DisplayType == null || !DisplayType.Type.ToString().StartsWith("Raised", StringComparison.Ordinal))
            return;

        var x = lastPointerPosition.X - Core.Graph!.GraphConfig!.RaisePoint[(int)Core.Save!.Mode].X;
        var y = lastPointerPosition.Y - Core.Graph!.GraphConfig!.RaisePoint[(int)Core.Save!.Mode].Y;
        if (Math.Abs(x) < 1)
            x = 0;
        if (Math.Abs(y) < 1)
            y = 0;
        Core.Controller!.MoveWindows(x, y);
        if (Math.Abs(x) + Math.Abs(y) > 20 && rasetype >= 1)
            rasetype = 0;
    }

    private int wavetimes = 0;
    private int switchcount = 0;
    private bool? waveleft = null;
    private bool? wavetop = null;
    private DateTime wavespan;

    /// <summary>
    /// 鼠标在桌宠身上来回滑动: 头部横扫算摸头, 身体横扫算摸身体
    /// </summary>
    /// 与 Windows 版 Main.xaml.cs 的 MainGrid_MouseWave 逐行对应. 两秒不动就重新计数;
    /// 在同一半边(上/下)里反复横扫超过 150 次会把计时归零, 防止一直贴着不放手刷数值.
    private void MainGrid_MouseWave(PointerEventArgs e)
    {
        if (e.GetCurrentPoint(MainGrid).Properties.IsLeftButtonPressed)
            return;
        isPress = false;
        if (rasetype >= 0 || State != WorkingState.Nomal)
            return;

        if ((DateTime.Now - wavespan).TotalSeconds > 2)
        {
            wavetimes = 0;
            switchcount = 0;
            waveleft = null;
            wavetop = null;
        }
        wavespan = DateTime.Now;
        bool active = false;
        var p = e.GetPosition(MainGrid);

        if (p.Y < 200)
        {
            if (wavetop != false)
                wavetop = true;
            else
            {
                if (switchcount++ > 150)
                    wavespan = DateTime.MinValue;
                return;
            }
        }
        else
        {
            if (wavetop != true)
                wavetop = false;
            else
            {
                if (switchcount++ > 150)
                    wavespan = DateTime.MinValue;
                return;
            }
        }

        if (p.X < 200 && waveleft != true)
        {
            waveleft = true;
            active = true;
        }
        if (p.X > 300 && waveleft != false)
        {
            active = true;
            waveleft = false;
        }

        if (active)
        {
            if (wavetimes++ > 4)
                if (wavetop == true)
                {
                    if (wavetimes >= 10 || IsIdel || DisplayType?.Type == GraphType.Touch_Head)
                        DisplayTouchHead();
                    LastInteractionTime = DateTime.Now;
                }
                else
                {
                    if (wavetimes >= 10 || IsIdel || DisplayType?.Type == GraphType.Touch_Body)
                        DisplayTouchBody();
                    LastInteractionTime = DateTime.Now;
                }
        }
    }

    private void MainGrid_MouseEnter(object? sender, PointerEventArgs e)
    {
        //如果是在侧边模式, 播放鼠标进入动画
        string? gfname;
        if (DisplayType?.Type == GraphType.SideHide_Left_Main && (gfname = Core.Graph!.FindName(GraphType.SideHide_Left_Rise)) != null)
        {
            Display(gfname, AnimatType.A_Start, DisplayBLoopingForce);
        }
        else if (DisplayType?.Type == GraphType.SideHide_Right_Main && (gfname = Core.Graph!.FindName(GraphType.SideHide_Right_Rise)) != null)
        {
            Display(gfname, AnimatType.A_Start, DisplayBLoopingForce);
        }
    }

    private void MainGrid_MouseLeave(object? sender, PointerEventArgs e)
    {
        //如果是在侧边模式, 播放鼠标离开动画
        if (DisplayType?.Type == GraphType.SideHide_Left_Rise)
        {
            Display(GraphType.SideHide_Left_Rise, AnimatType.C_End, () => Display(GraphType.SideHide_Left_Main, AnimatType.B_Loop, DisplayBLoopingForce));
        }
        else if (DisplayType?.Type == GraphType.SideHide_Right_Rise)
        {
            Display(GraphType.SideHide_Right_Rise, AnimatType.C_End, () => Display(GraphType.SideHide_Right_Main, AnimatType.B_Loop, DisplayBLoopingForce));
        }
    }

    /// <summary>
    /// 显示拖拽情况
    /// </summary>
    public void DisplayRaised()
    {
        var x = lastPointerPosition.X - Core.Graph!.GraphConfig!.RaisePoint[(int)Core.Save!.Mode].X;
        var y = lastPointerPosition.Y - Core.Graph!.GraphConfig!.RaisePoint[(int)Core.Save!.Mode].Y;
        if (Math.Abs(x) < 1)
            x = 0;
        if (Math.Abs(y) < 1)
            y = 0;
        Core.Controller!.MoveWindows(x, y);
        rasetype = 0;
        DisplayRaising();
    }

    private int rasetype = int.MinValue;

    /// <summary>
    /// 显示拖拽中
    /// </summary>
    private void DisplayRaising(string? name = null)
    {
        switch (rasetype)
        {
            case int.MinValue:
                break;
            case -1:
                rasetype = int.MinValue;
                Core.Controller!.RePositionActive = !Core.Controller!.CheckPosition();
                //判断侧边隐藏
                if (!MoveSideHideCheck())
                {
                    if (string.IsNullOrEmpty(name))
                        Display(GraphType.Raised_Static, AnimatType.C_End, DisplayToNomal);
                    else
                        Display(name, AnimatType.C_End, GraphType.Raised_Static, DisplayToNomal);
                }
                return;
            case 0:
            case 1:
            case 2:
                rasetype++;
                if (string.IsNullOrEmpty(name))
                    Display(GraphType.Raised_Dynamic, AnimatType.Single, DisplayRaising);
                else
                    Display(name, AnimatType.Single, GraphType.Raised_Dynamic, DisplayRaising);
                return;
            case 3:
                rasetype++;
                if (string.IsNullOrEmpty(name))
                    Display(name, AnimatType.A_Start, DisplayRaising);
                else
                    Display(name, AnimatType.A_Start, GraphType.Raised_Static, DisplayRaising);
                return;
            default:
                rasetype = 4;
                if (string.IsNullOrEmpty(name))
                    Display(name, AnimatType.B_Loop, DisplayRaising);
                else
                    Display(name, AnimatType.B_Loop, GraphType.Raised_Static, DisplayRaising);
                return;
        }
    }

    /// <summary>
    /// 清理所有状态
    /// </summary>
    public void CleanState()
    {
        MoveTimer.Enabled = false;
        rasetype = int.MinValue;
    }

    /// <summary>
    /// MOD 提供的语音播放器, 没有就是 null
    /// </summary>
    public IVoicePlayer? VoicePlayer { get; set; }

    /// <summary>
    /// 正在放语音吗
    /// </summary>
    public bool PlayingVoice => VoicePlayer?.IsPlaying == true;

    /// <summary>
    /// 语音还剩多久播完
    /// </summary>
    /// 消息栏靠它决定气泡什么时候收起 —— 只有一个"播完了"的事件是不够的,
    /// 气泡得在话说完之前就一直挂着
    public TimeSpan VoiceRemaining => VoicePlayer?.Remaining ?? TimeSpan.Zero;

    private double playVoiceVolume = 1;

    /// <summary>
    /// 语音音量 0~1
    /// </summary>
    public double PlayVoiceVolume
    {
        get => playVoiceVolume;
        set
        {
            playVoiceVolume = value;
            if (VoicePlayer != null)
                VoicePlayer.Volume = value;
        }
    }

    /// <summary>
    /// 播放一段语音
    /// </summary>
    /// <param name="path">音频文件路径</param>
    public void PlayVoice(string path)
    {
        var player = VoicePlayer;
        if (player == null)
            return;
        try
        {
            player.Volume = playVoiceVolume;
            player.Play(path);
        }
        catch (Exception)
        {
            // 一句语音放不出来不该把说话整个打断
        }
    }

    /// <summary>
    /// 停止语音
    /// </summary>
    public void StopVoice()
    {
        try { VoicePlayer?.Stop(); }
        catch (Exception) { }
    }
}
