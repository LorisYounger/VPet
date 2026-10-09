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
        /// 获取桌宠窗口当前所在屏幕的逻辑 (DIP) 边界, 拖到哪个屏幕就按哪个屏幕算,
        /// 多显示器 (含混合缩放) 下贴边/回正判定不再被锁死在主屏
        /// </summary>
        private Rectangle CurrentScreenBorder()
        {
            var helper = new WindowInteropHelper(mw);
            if (helper.Handle == IntPtr.Zero)
                return new Rectangle(0, 0,
                    (int)System.Windows.SystemParameters.PrimaryScreenWidth,
                    (int)System.Windows.SystemParameters.PrimaryScreenHeight);

            var screen = Screen.FromHandle(helper.Handle);
            var transform = HwndSource.FromHwnd(helper.Handle)?.CompositionTarget?.TransformToDevice;
            if (transform.HasValue && transform.Value.M11 > 0 && transform.Value.M22 > 0)
            {
                return new Rectangle(
                    (int)Math.Round(screen.Bounds.X / transform.Value.M11),
                    (int)Math.Round(screen.Bounds.Y / transform.Value.M22),
                    (int)Math.Round(screen.Bounds.Width / transform.Value.M11),
                    (int)Math.Round(screen.Bounds.Height / transform.Value.M22));
            }
            return screen.Bounds;
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() => mw.Left - CurrentScreenBorder().X);
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() => mw.Top - CurrentScreenBorder().Y);
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var border = CurrentScreenBorder();
                return border.X + border.Width - mw.Left - mw.ActualWidth;
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                var border = CurrentScreenBorder();
                return border.Y + border.Height - mw.Top - mw.ActualHeight;
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
                if (GetWindowsDistanceUp() < -0.25 * mw.ActualHeight)
                {
                    MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
                }
                else if (GetWindowsDistanceDown() < -0.25 * mw.ActualHeight)
                {
                    MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
                }
                if (GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth)
                {
                    MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
                }
                else if (GetWindowsDistanceRight() < -0.25 * mw.ActualWidth)
                {
                    MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mw.Dispatcher.Invoke(() =>
               GetWindowsDistanceUp() < -0.25 * mw.ActualHeight
            || GetWindowsDistanceDown() < -0.25 * mw.ActualHeight
            || GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth
            || GetWindowsDistanceRight() < -0.25 * mw.ActualWidth
        );

        public bool RePositionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;
    }
}
