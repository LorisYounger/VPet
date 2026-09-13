using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Drawing;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// 窗体控制器实现
    /// </summary>
    /// 跨平台: 原文复制自 VPet-Simulator.Windows/Function/MWController.cs, 只有两处平台差异:
    ///   1. WPF 的 Left/ActualWidth/SystemParameters 都是设备无关单位, Avalonia 的 Window.Position 和 Screen.Bounds
    ///      是物理像素, 这里在读写位置时按当前屏幕缩放换算 (见 MainWindow.Left/Top 与 ToDip), 对外单位与 Windows 版一致;
    ///   2. 原生 Wayland 协议上禁止应用自设全局坐标, 那种会话下 MoveWindows 是空操作、距离返回"足够远"的有限值,
    ///      让 Core 的贴边判断恒不触发 (见 CanPositionWindow).
    /// Screen.AllScreens 换成 Avalonia 的 Screens.All, 屏幕的比对用 Equals 而不是 DeviceName.
    internal class MWController : IController
    {
        readonly MainWindow mw;
        public MWController(MainWindow mw)
        {
            this.mw = mw;
            _isPrimaryScreen = mw.Set.MoveAreaDefault;
            _screenBorder = mw.Set.MoveArea;
            CanPositionWindow = DetectPositioningSupport();
        }

        /// <summary>
        /// 当前平台是否支持由应用自行设置窗口的全局坐标
        /// </summary>
        public bool CanPositionWindow { get; }

        /// <summary>
        /// "足够远"的哨兵距离
        /// </summary>
        /// 不能用 double.MaxValue: Core 的贴边逻辑里有 -距离/缩放 - 长度 这样的算式,
        /// 用极大值会溢出成负无穷, 后续再参与运算就变成 NaN, 而 NaN 的比较全为 false,
        /// 可能走到意料之外的分支. 用一个有限的大数最安全.
        internal const double FarAway = 1e5;

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

        private Rectangle _screenBorder;
        private bool _isPrimaryScreen = true;
        public bool IsPrimaryScreen
        {
            get
            {
                return _isPrimaryScreen;
            }
            private set
            {
                _isPrimaryScreen = value;
                mw.Set.MoveAreaDefault = value;
            }
        }
        public Rectangle ScreenBorder
        {
            get
            {
                return _screenBorder;
            }
            set
            {
                _screenBorder = value;
                mw.Set.MoveArea = value;
                IsPrimaryScreen = false;
            }
        }

        public void ResetScreenBorder()
        {
            IsPrimaryScreen = true;
        }

        private Screen? CurrentScreen => mw.Screens?.ScreenFromWindow(mw)
            ?? mw.Screens?.Primary
            ?? mw.Screens?.All.FirstOrDefault();

        /// <summary>
        /// 主屏幕的范围 (设备无关单位), 对应 WPF 的 SystemParameters.PrimaryScreenWidth/Height
        /// </summary>
        internal Rect PrimaryScreen => ToDip(mw.Screens?.Primary ?? CurrentScreen, new Rect(0, 0, 1920, 1080));

        /// <summary>
        /// 屏幕的完整范围换成设备无关单位
        /// </summary>
        /// 优先取屏幕自己的缩放而不是 window.RenderScaling: 窗口刚 Opened 时
        /// RenderScaling 还可能是 1(尚未落到具体屏幕上), 这时候拿它换算初始位置,
        /// 在 175% 的屏幕上会把桌宠摆到屏幕外去.
        private static Rect ToDip(Screen? screen, Rect fallback)
        {
            if (screen == null)
                return fallback;
            var scaling = screen.Scaling > 0 ? screen.Scaling : 1;
            var r = screen.Bounds;
            return new Rect(r.X / scaling, r.Y / scaling, r.Width / scaling, r.Height / scaling);
        }

        public double GetWindowsDistanceLeft()
        {
            if (!CanPositionWindow)
                return FarAway * ZoomRatio;
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return mw.Left;
                return mw.Left - ScreenBorder.X;
            });
        }

        public double GetWindowsDistanceUp()
        {
            if (!CanPositionWindow)
                return FarAway * ZoomRatio;
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return mw.Top;
                return mw.Top - ScreenBorder.Y;
            });
        }

        public double GetWindowsDistanceRight()
        {
            if (!CanPositionWindow)
                return FarAway * ZoomRatio;
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return PrimaryScreen.Width - mw.Left - mw.ActualWidth;
                return ScreenBorder.Width + ScreenBorder.X - mw.Left - mw.ActualWidth;
            });
        }

        public double GetWindowsDistanceDown()
        {
            if (!CanPositionWindow)
                return FarAway * ZoomRatio;
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return PrimaryScreen.Height - mw.Top - mw.ActualHeight;
                return ScreenBorder.Height + ScreenBorder.Y - mw.Top - mw.ActualHeight;
            });
        }

        public void MoveWindows(double X, double Y)
        {
            if (!CanPositionWindow)
                return;
            mw.Dispatcher.Invoke(() =>
            {
                mw.Left += X * ZoomRatio;
                mw.Top += Y * ZoomRatio;
            });
        }

        public bool IfInActivateScreen()
        {
            if (!CanPositionWindow)
                return true;
            try
            {
                //跨平台: Avalonia 的 Dispatcher 没有 HasShutdownStarted, 退出中的情况由下面的 try/catch 兜住
                if (mw.winSetting != null && mw.winSetting.IsVisible) return false;
                if (mw.winBetterBuy != null && mw.winBetterBuy.IsVisible) return false;
                if (mw.winWorkMenu != null && mw.winWorkMenu.IsVisible) return false;
                for (int i = 0; i < mw.Windows.Count; i++)
                {
                    if (mw.Windows[i] != null && mw.Windows[i].IsVisible) return false;
                }
            }
            catch { }
            return mw.Dispatcher.Invoke(() =>
            {
                try
                {
                    var screen = CurrentScreen;
                    var screens = mw.Screens?.All;
                    if (screen == null || screens == null)
                        return true;
                    for (int i = 0; i < screens.Count; i++)
                    {
                        if (screens[i].Equals(screen))
                        {
                            if (i == mw.Set.GameScreenIndex)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            });
        }

        public void SetNowScreenActivate()
        {
            if (!CanPositionWindow)
                return;
            mw.Dispatcher.Invoke(() =>
            {
                if (!mw.IsLoaded) return;
                if (mw.winSetting != null && mw.winSetting.IsVisible) return;
                if (mw.winBetterBuy != null && mw.winBetterBuy.IsVisible) return;
                if (mw.winWorkMenu != null && mw.winWorkMenu.IsVisible) return;
                var currentScreen = CurrentScreen;
                var screens = mw.Screens?.All;
                if (currentScreen == null || screens == null)
                    return;

                var bounds = ToDip(currentScreen, new Rect(0, 0, 1920, 1080));
                Rectangle logicalBounds = new Rectangle(
                    (int)bounds.X,
                    (int)bounds.Y,
                    (int)bounds.Width,
                    (int)bounds.Height
                );

                ScreenBorder = logicalBounds;

                for (int i = 0; i < screens.Count; i++)
                {
                    if (screens[i].Equals(currentScreen))
                    {
                        mw.Set.GameScreenIndex = i;
                        break;
                    }
                }
            });
        }

        public void ShowSetting()
        {
            mw.Topmost = false;
            mw.ShowSetting();
        }

        public void ShowPanel()
        {
            var panelWindow = new winCharacterPanel(mw);
            panelWindow.Show();
        }

        public void ResetPosition()
        {
            if (!CanPositionWindow)
                return;
            mw.Dispatcher.Invoke(() =>
            {
                if (GetWindowsDistanceUp() < -0.25 * mw.ActualHeight && GetWindowsDistanceDown() < PrimaryScreen.Height)
                {
                    MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
                }
                else if (GetWindowsDistanceDown() < -0.25 * mw.ActualHeight && GetWindowsDistanceUp() < PrimaryScreen.Height)
                {
                    MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
                }
                if (GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth && GetWindowsDistanceRight() < PrimaryScreen.Width)
                {
                    MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
                }
                else if (GetWindowsDistanceRight() < -0.25 * mw.ActualWidth && GetWindowsDistanceLeft() < PrimaryScreen.Width)
                {
                    MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => CanPositionWindow && mw.Dispatcher.Invoke(() =>
               GetWindowsDistanceUp() < -0.25 * mw.ActualHeight && GetWindowsDistanceDown() < PrimaryScreen.Height
            || GetWindowsDistanceDown() < -0.25 * mw.ActualHeight && GetWindowsDistanceUp() < PrimaryScreen.Height
            || GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth && GetWindowsDistanceRight() < PrimaryScreen.Width
            || GetWindowsDistanceRight() < -0.25 * mw.ActualWidth && GetWindowsDistanceLeft() < PrimaryScreen.Width
        );

        public bool RePositionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;
    }
}
