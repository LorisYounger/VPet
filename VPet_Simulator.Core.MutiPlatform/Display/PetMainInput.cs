using Avalonia;
using Avalonia.Input;
using System;
using System.Threading;
using System.Threading.Tasks;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体: 指针输入
/// </summary>
/// 对应 Windows 版 Main.xaml.cs 里的鼠标处理和 Load_2_TouchEvent.
///
/// 与 Windows 版的一处实现差异: 那边是在后台线程睡够长按时长之后, 再用
/// Mouse.GetPosition 去查当前鼠标位置(而且用的是 BeginInvoke(...).Wait(),
/// 在 Avalonia 上会死锁). Avalonia 没有全局指针位置查询, 这里改为在指针事件里
/// 记下最后位置, 长按判定时直接读 —— 既避开了死锁, 语义也一致.
public partial class PetMain
{
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
        MainGrid.PointerReleased += MainGrid_PointerReleased;
        MainGrid.PointerMoved += MainGrid_PointerMoved;
        MainGrid.PointerEntered += MainGrid_PointerEntered;
        MainGrid.PointerExited += MainGrid_PointerExited;
    }

    private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(MainGrid);
        lastPointerPosition = point.Position;

        if (point.Properties.IsRightButtonPressed)
        {
            if (ToolBar?.IsVisible == true)
            {
                ToolBar.CloseTimer.Enabled = false;
                ToolBar.IsVisible = false;
            }
            else
                ToolBar?.Show();
            DefaultRightClickAction?.Invoke();
            return;
        }
        if (!point.Properties.IsLeftButtonPressed)
            return;

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

    private void MainGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
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
        lastPointerPosition = e.GetPosition(MainGrid);
        if (!isPress)
        {
            // Windows 版是在提起/放下时来回换 MouseMove 的处理器, 这里合成一个:
            // 没按着就是"滑动摸头", 按着就是"拖动"
            MainGrid_PointerWave(e);
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
    private void MainGrid_PointerWave(PointerEventArgs e)
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

    private void MainGrid_PointerEntered(object? sender, PointerEventArgs e)
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

    private void MainGrid_PointerExited(object? sender, PointerEventArgs e)
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
}
