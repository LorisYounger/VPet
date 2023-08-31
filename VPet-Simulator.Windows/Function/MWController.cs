using System.Windows.Forms;
using System.Windows.Interop;
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
        }

        private Rectangle? _screenBorder = null;
        private Rectangle ScreenBorder
        {
            get
            {
                if (_screenBorder == null)
                {
                    var windowInteropHelper = new WindowInteropHelper(mw);
                    var currentScreen = Screen.FromHandle(windowInteropHelper.Handle);
                    _screenBorder = currentScreen.Bounds;
                }
                return (Rectangle)_screenBorder;
            }
        }
        public void ClearScreenBorderCache()
        {
            _screenBorder = null;
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() => mw.Left - ScreenBorder.X);
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() => mw.Top - ScreenBorder.Y);
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() => ScreenBorder.Width + ScreenBorder.X - mw.Left - mw.Width);
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() => ScreenBorder.Height + ScreenBorder.Y - mw.Top - mw.Height);
        }

        public void MoveWindows(double X, double Y)
        {
            mw.Dispatcher.Invoke(() =>
            {
                mw.Left += X * ZoomRatio;
                mw.Top += Y * ZoomRatio;
                ClearScreenBorderCache();
            });
        }

        public void ShowSetting()
        {
            mw.Topmost = false;
            mw.ShowSetting();
        }

        public void ShowPanel()
        {
            var panelWindow = new winCharacterPanel();
            panelWindow.ShowDialog();
        }

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

    }
}
