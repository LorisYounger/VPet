using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 窗体控制器实现
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

        public double GetWindowsDistanceLeft()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return mp.Left;
                return mp.Left - mw.MWController.ScreenBorder.X;
            });
        }

        public double GetWindowsDistanceUp()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return mp.Top;
                return mp.Top - mw.MWController.ScreenBorder.Y;
            });
        }

        public double GetWindowsDistanceRight()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return System.Windows.SystemParameters.PrimaryScreenWidth - mp.Left - mp.ActualWidth;
                return mw.MWController.ScreenBorder.Width + mw.MWController.ScreenBorder.X - mp.Left - mp.ActualWidth;
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mp.Dispatcher.Invoke(() =>
            {
                if (mw.MWController.IsPrimaryScreen) return System.Windows.SystemParameters.PrimaryScreenHeight - mp.Top - mp.ActualHeight;
                return mw.MWController.ScreenBorder.Height + mw.MWController.ScreenBorder.Y - mp.Top - mp.ActualHeight;
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
                if (GetWindowsDistanceUp() < -0.25 * mp.ActualHeight && GetWindowsDistanceDown() < System.Windows.SystemParameters.PrimaryScreenHeight)
                {
                    MoveWindows(0, -GetWindowsDistanceUp() / ZoomRatio);
                }
                else if (GetWindowsDistanceDown() < -0.25 * mp.ActualHeight && GetWindowsDistanceUp() < System.Windows.SystemParameters.PrimaryScreenHeight)
                {
                    MoveWindows(0, GetWindowsDistanceDown() / ZoomRatio);
                }
                if (GetWindowsDistanceLeft() < -0.25 * mp.ActualWidth && GetWindowsDistanceRight() < System.Windows.SystemParameters.PrimaryScreenWidth)
                {
                    MoveWindows(-GetWindowsDistanceLeft() / ZoomRatio, 0);
                }
                else if (GetWindowsDistanceRight() < -0.25 * mp.ActualWidth && GetWindowsDistanceLeft() < System.Windows.SystemParameters.PrimaryScreenWidth)
                {
                    MoveWindows(GetWindowsDistanceRight() / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mp.Dispatcher.Invoke(() =>
               GetWindowsDistanceUp() < -0.25 * mp.ActualHeight && GetWindowsDistanceDown() < System.Windows.SystemParameters.PrimaryScreenHeight
            || GetWindowsDistanceDown() < -0.25 * mp.ActualHeight && GetWindowsDistanceUp() < System.Windows.SystemParameters.PrimaryScreenHeight
            || GetWindowsDistanceLeft() < -0.25 * mp.ActualWidth && GetWindowsDistanceRight() < System.Windows.SystemParameters.PrimaryScreenWidth
            || GetWindowsDistanceRight() < -0.25 * mp.ActualWidth && GetWindowsDistanceLeft() < System.Windows.SystemParameters.PrimaryScreenWidth
        );

        public bool RePostionActive { get; set; } = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => false;

        public int InteractionCycle => mw.Set.InteractionCycle;

    }
}
