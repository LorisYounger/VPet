using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    public class MWController : IController
    {
        MainWindow mw;
        public MWController(MainWindow mw)
        {
            this.mw = mw;
        }

        public override double WindowsWidth { get => mw.Width; set => mw.Width = value; }
        public override double WindowsHight { get => mw.Height; set => mw.Height = value; }

        public override double GetWindowsDistanceDown()
        {
            return System.Windows.SystemParameters.PrimaryScreenHeight - mw.Top - mw.Height;
        }

        public override double GetWindowsDistanceLeft()
        {
            return mw.Left;
        }

        public override double GetWindowsDistanceRight()
        {
            return System.Windows.SystemParameters.PrimaryScreenWidth - mw.Left - mw.Width;
        }

        public override double GetWindowsDistanceUp()
        {
            return mw.Top;
        }

        public override void MoveWindows(double X, double Y)
        {
            mw.Left += X * .5;
            mw.Top += Y * .5;
        }
    }
}
