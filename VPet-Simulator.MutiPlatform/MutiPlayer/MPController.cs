using System;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// 窗体控制器实现
    /// </summary>
    /// 跨平台: 原文复制自 VPet-Simulator.Windows/MutiPlayer/MPController.cs. 平台差异与 MWController 相同:
    /// SystemParameters.PrimaryScreenWidth/Height 换成 MWController.PrimaryScreen (设备无关单位),
    /// 原生 Wayland 下不能自设窗口位置 (见 MWController.CanPositionWindow), Screen.FromHandle 换成 Avalonia 的 Screens
    public class MPController : IController
    {
        readonly MPFriends mp;
        readonly MainWindow mw;
        public MPController(MPFriends mp, MainWindow mw)
        {
            this.mp = mp;
            this.mw = mw;
        }

        public double GetWindowsDistanceLeft()
        {
            if (!mw.MWController.CanPositionWindow)
                return MWController.FarAway * ZoomRatio;
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return mp.Left;
                return mp.Left - mw.MWController.ScreenBorder.X;
            });
        }

        public double GetWindowsDistanceUp()
        {
            if (!mw.MWController.CanPositionWindow)
                return MWController.FarAway * ZoomRatio;
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return mp.Top;
                return mp.Top - mw.MWController.ScreenBorder.Y;
            });
        }

        public double GetWindowsDistanceRight()
        {
            if (!mw.MWController.CanPositionWindow)
                return MWController.FarAway * ZoomRatio;
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return mw.MWController.PrimaryScreen.Width - mp.Left - mp.ActualWidth;
                return mw.MWController.ScreenBorder.Width + mw.MWController.ScreenBorder.X - mp.Left - mp.ActualWidth;
            });
        }

        public double GetWindowsDistanceDown()
        {
            if (!mw.MWController.CanPositionWindow)
                return MWController.FarAway * ZoomRatio;
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return mw.MWController.PrimaryScreen.Height - mp.Top - mp.ActualHeight;
                return mw.MWController.ScreenBorder.Height + mw.MWController.ScreenBorder.Y - mp.Top - mp.ActualHeight;
            });
        }

        public void MoveWindows(double X, double Y)
        {
            if (!mw.MWController.CanPositionWindow)
                return;
            mp.Dispatcher.Invoke(() =>
            {
                mp.Left += X * ZoomRatio;
                mp.Top += Y * ZoomRatio;
            });
        }

        public void ShowSetting()
        {

        }

        public void ShowPanel()
        {

        }

        public void ResetPosition()
        {
            if (!mw.MWController.CanPositionWindow)
                return;
            mp.Dispatcher.Invoke(() =>
            {
                if (GetWindowsDistanceUp() < -0.25 * mp.ActualHeight && GetWindowsDistanceDown() < mw.MWController.PrimaryScreen.Height)
                {
                    MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
                }
                else if (GetWindowsDistanceDown() < -0.25 * mp.ActualHeight && GetWindowsDistanceUp() < mw.MWController.PrimaryScreen.Height)
                {
                    MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
                }
                if (GetWindowsDistanceLeft() < -0.25 * mp.ActualWidth && GetWindowsDistanceRight() < mw.MWController.PrimaryScreen.Width)
                {
                    MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
                }
                else if (GetWindowsDistanceRight() < -0.25 * mp.ActualWidth && GetWindowsDistanceLeft() < mw.MWController.PrimaryScreen.Width)
                {
                    MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mw.MWController.CanPositionWindow && mp.Dispatcher.Invoke(() =>
               GetWindowsDistanceUp() < -0.25 * mp.ActualHeight && GetWindowsDistanceDown() < mw.MWController.PrimaryScreen.Height
            || GetWindowsDistanceDown() < -0.25 * mp.ActualHeight && GetWindowsDistanceUp() < mw.MWController.PrimaryScreen.Height
            || GetWindowsDistanceLeft() < -0.25 * mp.ActualWidth && GetWindowsDistanceRight() < mw.MWController.PrimaryScreen.Width
            || GetWindowsDistanceRight() < -0.25 * mp.ActualWidth && GetWindowsDistanceLeft() < mw.MWController.PrimaryScreen.Width
        );

        public bool RePositionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => false;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;

        public bool IfInActivateScreen()
        {
            if (!mw.MWController.CanPositionWindow)
                return true;
            //跨平台: Avalonia 的 Dispatcher 没有 HasShutdownStarted, 退出中的情况由下面的 try/catch 兜住
            return mp.Dispatcher.Invoke(() =>
            {

                try
                {
                    var screen = mp.Screens?.ScreenFromWindow(mp);
                    var screens = mp.Screens?.All;
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

        }
    }
}
