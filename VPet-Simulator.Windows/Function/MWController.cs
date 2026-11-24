using System;
using System.Drawing;
using System.Windows;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 窗体控制器实现
    /// </summary>
    public class MWController : IController
    {
        readonly MainWindow mw;
        public MWController(MainWindow mw)
        {
            this.mw = mw;
            _isPrimaryScreen = mw.Set.MoveAreaDefault;
            _screenBorder = mw.Set.MoveArea;
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
                mw.Set.MoveAreaScreen = string.Empty;
                mw.Set.AutoChangeWindow = false;
                IsPrimaryScreen = false;
            }
        }

        public void ResetScreenBorder()
        {
            mw.Set.MoveAreaScreen = string.Empty;
            IsPrimaryScreen = true;
        }

        internal double GetWindowWidth()
        {
            if (mw.ActualWidth > 0)
                return mw.ActualWidth;
            if (mw.Width > 0 && !double.IsNaN(mw.Width))
                return mw.Width;
            return 500 * ZoomRatio;
        }

        internal double GetWindowHeight()
        {
            if (mw.ActualHeight > 0)
                return mw.ActualHeight;
            if (mw.Height > 0 && !double.IsNaN(mw.Height))
                return mw.Height;
            return 500 * ZoomRatio;
        }

        private Rect GetCurrentWindowRect()
            => new Rect(mw.Left, mw.Top, GetWindowWidth(), GetWindowHeight());

        /// <summary>
        /// 计算桌宠当前位置对应的实时有效移动边界（必须在 UI 线程调用）
        /// </summary>
        internal Rect GetEffectiveBounds()
        {
            var screens = ScreenTopology.GetScreens(mw);
            var winRect = GetCurrentWindowRect();

            // 1. 多屏自由跨屏模式：使用实时多屏并集可达边界
            if (AutoChangeWindow)
                return ScreenTopology.GetUnionBounds(screens, winRect);

            // 2. 仅主屏模式
            if (IsPrimaryScreen)
                return ScreenTopology.GetPrimaryScreenRect(screens);

            // 3. 锁定指定屏幕模式：按设备名实时查找当前边界
            var lockedName = mw.Set.MoveAreaScreen;
            if (!string.IsNullOrEmpty(lockedName))
            {
                var screen = ScreenTopology.FindScreenByDeviceName(screens, lockedName);
                if (screen.HasValue)
                {
                    var rect = ScreenTopology.ToDrawingRectangle(screen.Value.LogicalBounds);
                    if (rect != _screenBorder)
                    {
                        _screenBorder = rect;
                        mw.Set.MoveArea = rect;
                    }
                    return screen.Value.LogicalBounds;
                }
                // 目标显示器已断开，回退主屏
                return ScreenTopology.GetPrimaryScreenRect(screens);
            }

            // 4. 自定义矩形 / 历史 movearea：与真实显示器集合求交，杜绝进入虚空
            return ScreenTopology.GetCustomAreaBounds(
                screens,
                ScreenTopology.FromDrawingRectangle(_screenBorder),
                winRect
            );
        }

        /// <summary>
        /// 校验并修复过期/落入虚空的移动区域配置（在启动与显示器变更时调用）
        /// </summary>
        public void ValidateAndRepairMoveArea()
        {
            mw.Dispatcher.Invoke(() =>
            {
                ScreenTopology.InvalidateCache();
                var screens = ScreenTopology.GetScreens(mw);

                if (AutoChangeWindow || IsPrimaryScreen)
                    return;

                var lockedName = mw.Set.MoveAreaScreen;
                if (!string.IsNullOrEmpty(lockedName))
                {
                    var screen = ScreenTopology.FindScreenByDeviceName(screens, lockedName);
                    if (screen.HasValue)
                    {
                        var rect = ScreenTopology.ToDrawingRectangle(screen.Value.LogicalBounds);
                        _screenBorder = rect;
                        mw.Set.MoveArea = rect;
                    }
                    else
                    {
                        // 锁定的显示器已不存在，回退到主屏幕
                        ResetScreenBorder();
                    }
                    return;
                }

                // 自定义矩形或旧版快照：检查是否存在虚空区域
                var currentRect = ScreenTopology.FromDrawingRectangle(_screenBorder);
                if (ScreenTopology.TryRepairMoveArea(screens, currentRect, out var repaired))
                {
                    if (repaired.IsEmpty)
                    {
                        ResetScreenBorder();
                    }
                    else
                    {
                        var fixedRect = ScreenTopology.ToDrawingRectangle(repaired);
                        _screenBorder = fixedRect;
                        mw.Set.MoveArea = fixedRect;
                    }
                }
            });
        }

        /// <summary>
        /// 确保桌宠窗口位于当前有效边界之内（启动与显示器热插拔后调用）
        /// </summary>
        public void EnsureWindowInBounds()
        {
            mw.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                var clamped = ScreenTopology.ClampWindowPosition(
                    bounds,
                    mw.Left,
                    mw.Top,
                    GetWindowWidth(),
                    GetWindowHeight()
                );
                if (Math.Abs(mw.Left - clamped.X) > 0.5)
                    mw.Left = clamped.X;
                if (Math.Abs(mw.Top - clamped.Y) > 0.5)
                    mw.Top = clamped.Y;
            });
        }

        /// <summary>
        /// 将当前桌宠所在屏幕设为锁定活动屏幕（供设置页「设为当前屏幕」按钮显式调用）
        /// </summary>
        public void LockToCurrentScreen()
        {
            SetNowScreenActivateCore(ignoreOpenDialogs: true, lockSingleScreen: true);
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return mw.Left - bounds.Left;
            });
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return mw.Top - bounds.Top;
            });
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return bounds.Right - mw.Left - GetWindowWidth();
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                return bounds.Bottom - mw.Top - GetWindowHeight();
            });
        }

        public void MoveWindows(double X, double Y)
        {
            mw.Dispatcher.Invoke(() =>
            {
                mw.Left += X * ZoomRatio;
                mw.Top += Y * ZoomRatio;
            });
        }

        private bool HasBlockingDialogOpen()
        {
            if ((mw.winSetting != null && mw.winSetting.Visibility == Visibility.Visible)
                || (mw.winBetterBuy != null && mw.winBetterBuy.Visibility == Visibility.Visible)
                || (mw.winWorkMenu != null && mw.winWorkMenu.Visibility == Visibility.Visible)
                || (mw.winMutiPlayer != null && mw.winMutiPlayer.Visibility == Visibility.Visible))
            {
                return true;
            }
            for (int i = 0; i < mw.Windows.Count; i++)
            {
                if (mw.Windows[i] != null && mw.Windows[i].Visibility == Visibility.Visible)
                    return true;
            }
            return false;
        }

        public bool IfInActivateScreen()
        {
            if (mw.Dispatcher.HasShutdownStarted || mw.Dispatcher.HasShutdownFinished)
                return true;

            return mw.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (HasBlockingDialogOpen())
                        return true;

                    // 多屏并集模式下所有真实屏幕均实时处于活动状态，无需切换单屏矩形
                    if (AutoChangeWindow)
                    {
                        SyncGameScreenIndex();
                        return true;
                    }

                    var screens = ScreenTopology.GetScreens(mw);
                    var current = ScreenTopology.FindContainingOrNearestScreen(screens, GetCurrentWindowRect());
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
            SetNowScreenActivateCore(ignoreOpenDialogs: false, lockSingleScreen: !AutoChangeWindow);
        }

        private void SetNowScreenActivateCore(bool ignoreOpenDialogs, bool lockSingleScreen)
        {
            if (mw.Dispatcher.HasShutdownStarted || mw.Dispatcher.HasShutdownFinished)
                return;

            mw.Dispatcher.Invoke(() =>
            {
                if (!mw.IsLoaded)
                    return;
                if (!ignoreOpenDialogs && HasBlockingDialogOpen())
                    return;

                ScreenTopology.InvalidateCache();
                var screens = ScreenTopology.GetScreens(mw);
                var currentScreen = ScreenTopology.FindContainingOrNearestScreen(screens, GetCurrentWindowRect());

                for (int i = 0; i < screens.Count; i++)
                {
                    if (string.Equals(screens[i].DeviceName, currentScreen.DeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        mw.Set.GameScreenIndex = i;
                        break;
                    }
                }

                if (lockSingleScreen)
                {
                    var rect = ScreenTopology.ToDrawingRectangle(currentScreen.LogicalBounds);
                    _screenBorder = rect;
                    mw.Set.MoveArea = rect;
                    mw.Set.MoveAreaScreen = currentScreen.DeviceName;
                    mw.Set.AutoChangeWindow = false;
                    IsPrimaryScreen = false;
                }
            });
        }

        private void SyncGameScreenIndex()
        {
            var screens = ScreenTopology.GetScreens(mw);
            var current = ScreenTopology.FindContainingOrNearestScreen(screens, GetCurrentWindowRect());
            for (int i = 0; i < screens.Count; i++)
            {
                if (string.Equals(screens[i].DeviceName, current.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (mw.Set.GameScreenIndex != i)
                        mw.Set.GameScreenIndex = i;
                    break;
                }
            }
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
            mw.Dispatcher.Invoke(() =>
            {
                var bounds = GetEffectiveBounds();
                double w = GetWindowWidth();
                double h = GetWindowHeight();
                double maxLeft = Math.Max(bounds.Left, bounds.Right - w);
                double maxTop = Math.Max(bounds.Top, bounds.Bottom - h);

                if (mw.Top < bounds.Top - 0.25 * h)
                {
                    mw.Top = bounds.Top;
                }
                else if (mw.Top > maxTop + 0.25 * h)
                {
                    mw.Top = maxTop;
                }

                if (mw.Left < bounds.Left - 0.25 * w)
                {
                    mw.Left = bounds.Left;
                }
                else if (mw.Left > maxLeft + 0.25 * w)
                {
                    mw.Left = maxLeft;
                }
            });
        }

        public bool CheckPosition() => mw.Dispatcher.Invoke(() =>
        {
            var bounds = GetEffectiveBounds();
            double w = GetWindowWidth();
            double h = GetWindowHeight();
            double maxLeft = Math.Max(bounds.Left, bounds.Right - w);
            double maxTop = Math.Max(bounds.Top, bounds.Bottom - h);

            return mw.Top < bounds.Top - 0.25 * h
                || mw.Top > maxTop + 0.25 * h
                || mw.Left < bounds.Left - 0.25 * w
                || mw.Left > maxLeft + 0.25 * w;
        });

        public bool RePositionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;
    }
}
