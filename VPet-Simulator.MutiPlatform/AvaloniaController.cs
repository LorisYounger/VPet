using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 基于 Avalonia 窗口的桌宠控制器
/// </summary>
/// 对应 Windows 版的 MWController. 桌宠的爬行、贴边、回正都建立在
/// "能读写窗口的全局屏幕坐标"之上, 而这件事在各平台的支持程度差别很大:
///
///   Windows / X11 (含 XWayland) / macOS   完全支持
///   原生 Wayland                          协议上禁止应用自设全局坐标
///
/// 因此这里把能力做成可查询的: 平台不支持时 MoveWindows 变成空操作,
/// GetWindowsDistance* 返回一个"足够远"的有限值, 让 Core 的贴边判断恒不触发、
/// 爬墙类动画被排除、自由行走动画照常播放 —— 桌宠会原地动, 但不会崩也不会空转.
///
/// 单位约定: IController 上所有长度都是**设备无关单位**(WPF 的 Left/ActualWidth
/// 就是这个单位), 而 Avalonia 的 Window.Position 和 Screen.WorkingArea 都是物理
/// 像素. 两者之间差一个 DPI 缩放, 只在 100% 缩放的机器上看不出来, 到 150% 的屏幕
/// 上桌宠贴边就会冲过头. 所以这里对外一律换算成设备无关单位, 只在真正读写
/// Window.Position 时才乘回缩放.
public sealed class AvaloniaController : IController
{
    private readonly Window window;
    private readonly AppSettings settings;

    /// <summary>
    /// "足够远"的哨兵距离
    /// </summary>
    /// 不能用 double.MaxValue: Core 的贴边逻辑里有 -距离/缩放 - 长度 这样的算式,
    /// 用极大值会溢出成负无穷, 后续再参与运算就变成 NaN, 而 NaN 的比较全为 false,
    /// 可能走到意料之外的分支. 用一个有限的大数最安全.
    private const double FarAway = 1e5;

    public AvaloniaController(Window window, AppSettings settings)
    {
        this.window = window;
        this.settings = settings;
        CanPositionWindow = DetectPositioningSupport();
    }

    /// <summary>
    /// 当前平台是否支持由应用自行设置窗口的全局坐标
    /// </summary>
    public bool CanPositionWindow { get; }

    /// <summary>
    /// 探测当前平台能否自设窗口位置
    /// </summary>
    /// 判据是"有没有 X11 显示服务可用": Linux 上只要 DISPLAY 能连通(原生 X11 或
    /// XWayland 都算), Avalonia 就会走 X11 后端, 绝对定位可用; 只有在纯原生
    /// Wayland 会话下才需要降级.
    private static bool DetectPositioningSupport()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            return true;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            return true;
        // 只有 WAYLAND_DISPLAY 而没有 DISPLAY, 说明没有 XWayland 兜底
        return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }

    // 这四项都是用户设置, 直接读设置对象而不是拷一份: 菜单里改完要立刻生效

    public double ZoomRatio => settings.ZoomLevel;

    public int PressLength => settings.PressLength;

    public bool EnableFunction => settings.EnableFunction;

    public int InteractionCycle => settings.InteractionCycle;

    public bool RePositionActive { get; set; } = true;

    private Screen? CurrentScreen => window.Screens?.ScreenFromWindow(window)
        ?? window.Screens?.Primary
        ?? window.Screens?.All.FirstOrDefault();

    /// <summary>
    /// 桌宠当前所在屏幕的缩放倍率
    /// </summary>
    /// 优先取屏幕自己的缩放而不是 window.RenderScaling: 窗口刚 Opened 时
    /// RenderScaling 还可能是 1(尚未落到具体屏幕上), 这时候拿它换算初始位置,
    /// 在 175% 的屏幕上会把桌宠摆到屏幕外去.
    private double Scaling
    {
        get
        {
            var scaling = CurrentScreen?.Scaling ?? 0;
            if (scaling <= 0)
                scaling = window.RenderScaling;
            return scaling > 0 ? scaling : 1;
        }
    }

    /// <summary>
    /// 桌宠当前所在屏幕的工作区 (设备无关单位)
    /// </summary>
    /// 用工作区而不是整个屏幕: 否则桌宠会跑到任务栏/Dock 底下
    private Rect WorkingArea => ToDip(CurrentScreen?.WorkingArea, new Rect(0, 0, 1920, 1080));

    /// <summary>
    /// 桌宠当前所在屏幕的完整范围 (设备无关单位)
    /// </summary>
    /// 只用来做"对面那侧的距离是不是离谱"的兜底判断, 与 Windows 版用
    /// SystemParameters.PrimaryScreen* 的位置一致
    private Rect ScreenArea => ToDip(CurrentScreen?.Bounds, new Rect(0, 0, 1920, 1080));

    private Rect ToDip(PixelRect? rect, Rect fallback)
    {
        if (rect is not PixelRect r)
            return fallback;
        var scaling = Scaling;
        return new Rect(r.X / scaling, r.Y / scaling, r.Width / scaling, r.Height / scaling);
    }

    /// <summary>
    /// 窗口当前占据的矩形 (设备无关单位)
    /// </summary>
    private Rect WindowRect
    {
        get
        {
            // 以显式设定的 Width/Height 为准, Bounds 只是兜底: 桌宠窗口的尺寸是
            // 按缩放倍率算出来直接赋值的, 而 Bounds 要等一次排版才会跟上 ——
            // 刚改完缩放就拿 Bounds 定位, 会按旧尺寸把窗口摆到屏幕外面去
            var width = double.IsNaN(window.Width) ? window.Bounds.Width : window.Width;
            var height = double.IsNaN(window.Height) ? window.Bounds.Height : window.Height;
            return new Rect(window.Position.X / Scaling, window.Position.Y / Scaling, width, height);
        }
    }

    public void MoveWindows(double X, double Y)
    {
        if (!CanPositionWindow)
            return;
        // 与 Windows 版一致: 传进来的位移是未缩放的, 这里乘上缩放倍率.
        // 调用方都会先除以 ZoomRatio, 这是双方约定好的, 不要"顺手简化"
        var dx = X * ZoomRatio;
        var dy = Y * ZoomRatio;
        Dispatcher.UIThread.Invoke(() =>
        {
            var scaling = Scaling;
            window.Position = new PixelPoint(
                window.Position.X + (int)Math.Round(dx * scaling),
                window.Position.Y + (int)Math.Round(dy * scaling));
        });
    }

    public double GetWindowsDistanceLeft()
    {
        if (!CanPositionWindow)
            return FarAway * ZoomRatio;
        return Dispatcher.UIThread.Invoke(() => WindowRect.X - WorkingArea.X);
    }

    public double GetWindowsDistanceRight()
    {
        if (!CanPositionWindow)
            return FarAway * ZoomRatio;
        return Dispatcher.UIThread.Invoke(() => WorkingArea.Right - WindowRect.Right);
    }

    public double GetWindowsDistanceUp()
    {
        if (!CanPositionWindow)
            return FarAway * ZoomRatio;
        return Dispatcher.UIThread.Invoke(() => WindowRect.Y - WorkingArea.Y);
    }

    public double GetWindowsDistanceDown()
    {
        if (!CanPositionWindow)
            return FarAway * ZoomRatio;
        return Dispatcher.UIThread.Invoke(() => WorkingArea.Bottom - WindowRect.Bottom);
    }

    public bool AutoChangeWindow => settings.AutoChangeWindow;

    /// <summary>
    /// 桌宠当前是不是待在"游戏屏幕"上
    /// </summary>
    /// 对应 Windows 版 MWController.IfInActivateScreen. 那边还会在设置/商店/工作/
    /// 联机窗口打开时返回 false(免得桌宠在你操作界面时乱跑), 跨平台侧这些窗口还没
    /// 移植, 等它们落地后要在这里补上同样的判断.
    public bool IfInActivateScreen()
    {
        if (!CanPositionWindow)
            return true;
        return Dispatcher.UIThread.Invoke(() =>
        {
            try
            {
                var screens = window.Screens?.All;
                var current = CurrentScreen;
                if (screens == null || current == null)
                    return true;
                for (int i = 0; i < screens.Count; i++)
                {
                    if (screens[i].Equals(current))
                        return i == settings.GameScreenIndex;
                }
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        });
    }

    /// <summary>
    /// 把桌宠现在所在的屏幕记为"游戏屏幕"
    /// </summary>
    /// 对应 Windows 版 MWController.SetNowScreenActivate
    public void SetNowScreenActivate()
    {
        if (!CanPositionWindow)
            return;
        Dispatcher.UIThread.Invoke(() =>
        {
            var screens = window.Screens?.All;
            var current = CurrentScreen;
            if (screens == null || current == null)
                return;
            for (int i = 0; i < screens.Count; i++)
            {
                if (screens[i].Equals(current))
                {
                    settings.GameScreenIndex = i;
                    break;
                }
            }
        });
    }

    public void ShowPanel()
    {
        // 角色面板尚未移植
    }

    /// <summary>
    /// 在边缘时重新靠边，防止被阻挡
    /// </summary>
    /// 注意这个方法**不是**"把桌宠摆回某个默认位置": Core 在每段行走结束时都会调它,
    /// 真按字面意思实现的话桌宠每走一步就会被弹回原点. 它的职责只有一个 ——
    /// 身子有超过四分之一跑到屏幕外时, 把跑出去的那部分挪回来.
    /// 逻辑与 Windows 版 MWController.ResetPosition 逐行对应.
    public void ResetPosition()
    {
        if (!CanPositionWindow)
            return;
        Dispatcher.UIThread.Invoke(() =>
        {
            var rect = WindowRect;
            var screen = ScreenArea;
            if (GetWindowsDistanceUp() < -0.25 * rect.Height && GetWindowsDistanceDown() < screen.Height)
            {
                MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
            }
            else if (GetWindowsDistanceDown() < -0.25 * rect.Height && GetWindowsDistanceUp() < screen.Height)
            {
                MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
            }
            if (GetWindowsDistanceLeft() < -0.25 * rect.Width && GetWindowsDistanceRight() < screen.Width)
            {
                MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
            }
            else if (GetWindowsDistanceRight() < -0.25 * rect.Width && GetWindowsDistanceLeft() < screen.Width)
            {
                MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
            }
        });
    }

    /// <summary>
    /// 判断桌宠是否靠边
    /// </summary>
    public bool CheckPosition()
    {
        if (!CanPositionWindow)
            return false;
        return Dispatcher.UIThread.Invoke(() =>
        {
            var rect = WindowRect;
            var screen = ScreenArea;
            return GetWindowsDistanceUp() < -0.25 * rect.Height && GetWindowsDistanceDown() < screen.Height
                || GetWindowsDistanceDown() < -0.25 * rect.Height && GetWindowsDistanceUp() < screen.Height
                || GetWindowsDistanceLeft() < -0.25 * rect.Width && GetWindowsDistanceRight() < screen.Width
                || GetWindowsDistanceRight() < -0.25 * rect.Width && GetWindowsDistanceLeft() < screen.Width;
        });
    }

    /// <summary>
    /// 把桌宠摆到工作区右下角
    /// </summary>
    /// 这是宿主启动时的初始摆位, 不属于 IController —— 那上面的 ResetPosition
    /// 是"防止被挡住的回正", 不是"回到默认位置".
    public void MoveToDefaultPosition()
    {
        if (!CanPositionWindow)
            return;
        Dispatcher.UIThread.Invoke(() =>
        {
            var area = CurrentScreen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
            var rect = WindowRect;
            window.Position = new PixelPoint(
                area.Right - (int)Math.Round(rect.Width * Scaling),
                area.Bottom - (int)Math.Round(rect.Height * Scaling));
        });
    }
}
