using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public double WindowsWidth { get => mw.Dispatcher.Invoke(() => mw.Width); set => mw.Dispatcher.Invoke(() => mw.Width = value); }
        public double WindowsHight { get => mw.Dispatcher.Invoke(() => mw.Height); set => mw.Dispatcher.Invoke(() => mw.Height = value); }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() => System.Windows.SystemParameters.PrimaryScreenHeight - mw.Top - mw.Height);
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() => mw.Left);
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() => System.Windows.SystemParameters.PrimaryScreenWidth - mw.Left - mw.Width);
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() => mw.Top);
        }

        public void MoveWindows(double X, double Y)
        {
            mw.Dispatcher.Invoke(() =>
            {
                mw.Left += X * ZoomRatio;
                mw.Top += Y * ZoomRatio;
            });
        }
        public double ZoomRatio => 0.5;
    }
}
