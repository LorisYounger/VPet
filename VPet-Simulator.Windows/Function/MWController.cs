using Panuon.WPF.UI;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return mw.Left;
                return mw.Left - ScreenBorder.X;
            });
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return mw.Top;
                return mw.Top - ScreenBorder.Y;
            });
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return System.Windows.SystemParameters.PrimaryScreenWidth - mw.Left - mw.ActualWidth;
                return ScreenBorder.Width + ScreenBorder.X - mw.Left - mw.ActualWidth;
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return System.Windows.SystemParameters.PrimaryScreenHeight - mw.Top - mw.ActualHeight;
                return ScreenBorder.Height + ScreenBorder.Y - mw.Top - mw.ActualHeight;
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
            }
            catch { }
            return mw.Dispatcher.Invoke(() =>
            {

                try
                {
                    var window = Window.GetWindow(mw.Main);
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
            if (!mw.IsLoaded) return;
            if (mw.winSetting != null && mw.winSetting.Visibility == Visibility.Visible) return;
            mw.Dispatcher.Invoke(() =>
            {
                var helper = new WindowInteropHelper(mw);
                var currentScreen = Screen.FromHandle(helper.Handle);
                var hwndSource = HwndSource.FromHwnd(helper.Handle);
                var _rect = new Rect();

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
                if (GetWindowsDistanceUp() < -0.25 * mw.ActualHeight && GetWindowsDistanceDown() < System.Windows.SystemParameters.PrimaryScreenHeight)
                {
                    MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
                }
                else if (GetWindowsDistanceDown() < -0.25 * mw.ActualHeight && GetWindowsDistanceUp() < System.Windows.SystemParameters.PrimaryScreenHeight)
                {
                    MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
                }
                if (GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth && GetWindowsDistanceRight() < System.Windows.SystemParameters.PrimaryScreenWidth)
                {
                    MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
                }
                else if (GetWindowsDistanceRight() < -0.25 * mw.ActualWidth && GetWindowsDistanceLeft() < System.Windows.SystemParameters.PrimaryScreenWidth)
                {
                    MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mw.Dispatcher.Invoke(() =>
               GetWindowsDistanceUp() < -0.25 * mw.ActualHeight && GetWindowsDistanceDown() < System.Windows.SystemParameters.PrimaryScreenHeight
            || GetWindowsDistanceDown() < -0.25 * mw.ActualHeight && GetWindowsDistanceUp() < System.Windows.SystemParameters.PrimaryScreenHeight
            || GetWindowsDistanceLeft() < -0.25 * mw.ActualWidth && GetWindowsDistanceRight() < System.Windows.SystemParameters.PrimaryScreenWidth
            || GetWindowsDistanceRight() < -0.25 * mw.ActualWidth && GetWindowsDistanceLeft() < System.Windows.SystemParameters.PrimaryScreenWidth
        );

        public bool RePositionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;
    }
}
