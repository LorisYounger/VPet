using System;
using System.Windows;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 联机好友窗体控制器实现
    /// </summary>
    public class MPController : IController
    {
        readonly MPFriends mp;
        readonly MainWindow mw;
        public MPController(MPFriends mp, MainWindow mw)
        {
            this.mp = mp;
            this.mw = mw;
        }

        private double GetWindowWidth()
        {
            if (mp.ActualWidth > 0)
                return mp.ActualWidth;
            if (mp.Width > 0 && !double.IsNaN(mp.Width))
                return mp.Width;
            return 500 * ZoomRatio;
        }

        private double GetWindowHeight()
        {
            if (mp.ActualHeight > 0)
                return mp.ActualHeight;
            if (mp.Height > 0 && !double.IsNaN(mp.Height))
                return mp.Height;
            return 500 * ZoomRatio;
        }

        private Rect GetEffectiveBounds()
        {
            var screens = ScreenTopology.GetScreens(mp);
            var winRect = new Rect(mp.Left, mp.Top, GetWindowWidth(), GetWindowHeight());

            if (AutoChangeWindow)
                return ScreenTopology.GetUnionBounds(screens, winRect);

            if (mw.MWController.IsPrimaryScreen)
                return ScreenTopology.GetPrimaryScreenRect(screens);

            var lockedName = mw.Set.MoveAreaScreen;
            if (!string.IsNullOrEmpty(lockedName))
            {
                var screen = ScreenTopology.FindScreenByDeviceName(screens, lockedName);
                if (screen.HasValue)
                    return screen.Value.LogicalBounds;
                return ScreenTopology.GetPrimaryScreenRect(screens);
            }

            return ScreenTopology.GetCustomAreaBounds(
                screens,
                ScreenTopology.FromDrawingRectangle(mw.MWController.ScreenBorder),
                winRect
            );
        }

        /// <summary>
        /// 确保好友窗口位于当前有效边界之内
        /// </summary>
        public void EnsureWindowInBounds()
        {
            if (mp.Dispatcher.HasShutdownStarted || mp.Dispatcher.HasShutdownFinished)
                return;

            mp.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                var clamped = ScreenTopology.ClampWindowPosition(
                    bounds,
                    mp.Left,
                    mp.Top,
                    GetWindowWidth(),
                    GetWindowHeight()
                );
                if (Math.Abs(mp.Left - clamped.X) > 0.5)
                    mp.Left = clamped.X;
                if (Math.Abs(mp.Top - clamped.Y) > 0.5)
                    mp.Top = clamped.Y;
            });
        }

        public double GetWindowsDistanceLeft()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return mp.Left - bounds.Left;
            });
        }

        public double GetWindowsDistanceUp()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return mp.Top - bounds.Top;
            });
        }

        public double GetWindowsDistanceRight()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return bounds.Right - mp.Left - GetWindowWidth();
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return bounds.Bottom - mp.Top - GetWindowHeight();
            });
        }

        public void MoveWindows(double X, double Y)
        {
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
            mp.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                double w = GetWindowWidth();
                double h = GetWindowHeight();
                double maxLeft = Math.Max(bounds.Left, bounds.Right - w);
                double maxTop = Math.Max(bounds.Top, bounds.Bottom - h);

                if (mp.Top < bounds.Top - 0.25 * h)
                {
                    mp.Top = bounds.Top;
                }
                else if (mp.Top > maxTop + 0.25 * h)
                {
                    mp.Top = maxTop;
                }

                if (mp.Left < bounds.Left - 0.25 * w)
                {
                    mp.Left = bounds.Left;
                }
                else if (mp.Left > maxLeft + 0.25 * w)
                {
                    mp.Left = maxLeft;
                }
            });
        }

        public bool CheckPosition() => mp.Dispatcher.Invoke(() =>
        {
            var bounds = GetEffectiveBounds();
            double w = GetWindowWidth();
            double h = GetWindowHeight();
            double maxLeft = Math.Max(bounds.Left, bounds.Right - w);
            double maxTop = Math.Max(bounds.Top, bounds.Bottom - h);

            return mp.Top < bounds.Top - 0.25 * h
                || mp.Top > maxTop + 0.25 * h
                || mp.Left < bounds.Left - 0.25 * w
                || mp.Left > maxLeft + 0.25 * w;
        });

        public bool RePositionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => false;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;

        public bool IfInActivateScreen()
        {
            if (mp.Dispatcher.HasShutdownStarted || mp.Dispatcher.HasShutdownFinished)
                return true;

            if (AutoChangeWindow)
                return true;

            return mp.Dispatcher.Invoke(() =>
            {
                try
                {
                    var screens = ScreenTopology.GetScreens(mp);
                    var current = ScreenTopology.FindContainingOrNearestScreen(
                        screens,
                        new Rect(mp.Left, mp.Top, GetWindowWidth(), GetWindowHeight())
                    );
                    for (int i = 0; i < screens.Count; i++)
                    {
                        if (string.Equals(screens[i].DeviceName, current.DeviceName, StringComparison.OrdinalIgnoreCase))
                            return i == mw.Set.GameScreenIndex;
                    }
                    return false;
                }
                catch
                {
                    return true;
                }
            });
        }

        public void SetNowScreenActivate()
        {
            ScreenTopology.InvalidateCache();
            EnsureWindowInBounds();
        }
    }
}
