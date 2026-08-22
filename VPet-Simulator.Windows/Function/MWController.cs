using Panuon.WPF.UI;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
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
                IsPrimaryScreen = false;
            }
        }

        public void ResetScreenBorder()
        {
            IsPrimaryScreen = true;
        }

        /// <summary>
        /// 当前生效的活动边界: 用户设置了自定义活动区域就用它,
        /// 否则取窗口实际所在显示器的范围(按窗口DPI换算成逻辑坐标)。
        /// 之前默认永远按主屏尺寸判断, 副屏分辨率和主屏不一样时边距全是错的
        /// 需要在Dispatcher上下文中调用
        /// </summary>
        private Rectangle ActiveBounds()
        {
            if (!IsPrimaryScreen) return ScreenBorder;
            try
            {
                var helper = new WindowInteropHelper(mw);
                if (helper.Handle == IntPtr.Zero)
                    helper.EnsureHandle();
                double sx = 1.0, sy = 1.0;
                var hs = HwndSource.FromHwnd(helper.Handle);
                if (hs?.CompositionTarget != null)
                {
                    var t = hs.CompositionTarget.TransformToDevice;
                    sx = t.M11; sy = t.M22;
                }
                if (ScreenNative.TryGetMonitorBounds(helper.Handle, out var mx, out var my, out var w, out var h))
                    return new Rectangle((int)(mx / sx), (int)(my / sy), (int)(w / sx), (int)(h / sy));
            }
            catch { }
            return new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() => mw.Left - ActiveBounds().X);
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() => mw.Top - ActiveBounds().Y);
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var b = ActiveBounds();
                return b.X + b.Width - mw.Left - mw.ActualWidth;
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var b = ActiveBounds();
                return b.Y + b.Height - mw.Top - mw.ActualHeight;
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

        public bool IfInActivateScreen()
        {
            try
            {
                if (mw.Dispatcher.HasShutdownStarted || mw.Dispatcher.HasShutdownFinished) return false;
                if (mw.winSetting != null && mw.winSetting.Visibility == Visibility.Visible) return false;
                if (mw.winBetterBuy != null && mw.winBetterBuy.Visibility == Visibility.Visible) return false;
                if (mw.winWorkMenu != null && mw.winWorkMenu.Visibility == Visibility.Visible) return false;
                if (mw.winMutiPlayer != null && mw.winMutiPlayer.Visibility == Visibility.Visible) return false;
                for (int i = 0; i < mw.Windows.Count; i++)
                {
                    if (mw.Windows[i] != null && mw.Windows[i].Visibility == Visibility.Visible) return false;
                }
            }
            catch { }
            return mw.Dispatcher.Invoke(() =>
            {
                try
                {
                    var screen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(mw).Handle);
                    var screens = Screen.AllScreens;
                    for (int i = 0; i < screens.Length; i++)
                    {
                        if (screens[i].DeviceName == screen.DeviceName)
                        {
                            if(i == mw.Set.GameScreenIndex)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch(Exception)
                {
                    return true;
                }
            });
        }

        public void SetNowScreenActivate()
        {
            mw.Dispatcher.Invoke(() =>
            {
                if (!mw.IsLoaded) return;
                if (mw.winSetting != null && mw.winSetting.Visibility == Visibility.Visible) return;
                if (mw.winBetterBuy != null && mw.winBetterBuy.Visibility == Visibility.Visible) return;
                if (mw.winWorkMenu != null && mw.winWorkMenu.Visibility == Visibility.Visible) return;
                if (mw.winMutiPlayer != null && mw.winMutiPlayer.Visibility == Visibility.Visible) return;
                var helper = new WindowInteropHelper(mw);
                var currentScreen = Screen.FromHandle(helper.Handle);
                var hwndSource = HwndSource.FromHwnd(helper.Handle);

                Rectangle logicalBounds;

                if (hwndSource?.CompositionTarget != null)
                {
                    var dpi = hwndSource.CompositionTarget.TransformToDevice;

                    logicalBounds = new Rectangle(
                        (int)(currentScreen.Bounds.X / dpi.M11),
                        (int)(currentScreen.Bounds.Y / dpi.M22),
                        (int)(currentScreen.Bounds.Width / dpi.M11),
                        (int)(currentScreen.Bounds.Height / dpi.M22)
                    );
                }
                else
                {
                    logicalBounds = new Rectangle(
                        currentScreen.Bounds.X,
                        currentScreen.Bounds.Y,
                        currentScreen.Bounds.Width,
                        currentScreen.Bounds.Height
                    );
                }

                ScreenBorder = logicalBounds;

                var screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    if (screens[i].DeviceName == currentScreen.DeviceName)
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
            mw.Dispatcher.Invoke(() =>
            {
                var b = ActiveBounds();
                if (GetWindowsDistanceUp() < -0.25 * mw.ActualHeight && GetWindowsDistanceDown() < b.Height)
                {
                    MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
                }
                else if (GetWindowsDistanceDown() < -0.25 * mw.ActualHeight && GetWindowsDistanceUp() < b.Height)
                {
                    MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
                }
                if (GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth && GetWindowsDistanceRight() < b.Width)
                {
                    MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
                }
                else if (GetWindowsDistanceRight() < -0.25 * mw.ActualWidth && GetWindowsDistanceLeft() < b.Width)
                {
                    MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mw.Dispatcher.Invoke(() =>
        {
            var b = ActiveBounds();
            return    GetWindowsDistanceUp() < -0.25 * mw.ActualHeight && GetWindowsDistanceDown() < b.Height
                   || GetWindowsDistanceDown() < -0.25 * mw.ActualHeight && GetWindowsDistanceUp() < b.Height
                   || GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth && GetWindowsDistanceRight() < b.Width
                   || GetWindowsDistanceRight() < -0.25 * mw.ActualWidth && GetWindowsDistanceLeft() < b.Width;
        });

        public bool RePositionActive { get; set; } = true;

        public IntPtr GetWindowHandle()
        {
            var helper = new WindowInteropHelper(mw);
            if (helper.Handle == IntPtr.Zero)
                mw.Dispatcher.Invoke(() => helper.EnsureHandle());
            return helper.Handle;
        }

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;
    }
}
