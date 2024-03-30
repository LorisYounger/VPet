using System.Drawing;
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

        public bool RePostionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

    }
}
