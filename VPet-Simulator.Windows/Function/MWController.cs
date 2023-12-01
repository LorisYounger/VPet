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
                if (IsPrimaryScreen) return System.Windows.SystemParameters.PrimaryScreenWidth - mw.Left - mw.Width;
                return ScreenBorder.Width + ScreenBorder.X - mw.Left - mw.Width;
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (IsPrimaryScreen) return System.Windows.SystemParameters.PrimaryScreenHeight - mw.Top - mw.Height;
                return ScreenBorder.Height + ScreenBorder.Y - mw.Top - mw.Height;
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
                if (RePostionActive)
                {
                    if (mw.Core.Controller.GetWindowsDistanceUp() < -0.25 * mw.Height && mw.Core.Controller.GetWindowsDistanceDown() < System.Windows.SystemParameters.PrimaryScreenHeight)
                    {
                        mw.Core.Controller.MoveWindows(0, -mw.Core.Controller.GetWindowsDistanceUp() / mw.Core.Controller.ZoomRatio);
                    }
                    else if (mw.Core.Controller.GetWindowsDistanceDown() < -0.25 * mw.Height && mw.Core.Controller.GetWindowsDistanceUp() < System.Windows.SystemParameters.PrimaryScreenHeight)
                    {
                        mw.Core.Controller.MoveWindows(0, mw.Core.Controller.GetWindowsDistanceDown() / mw.Core.Controller.ZoomRatio);
                    }
                    if (mw.Core.Controller.GetWindowsDistanceLeft() < -0.25 * mw.Width && mw.Core.Controller.GetWindowsDistanceRight() < System.Windows.SystemParameters.PrimaryScreenWidth)
                    {
                        mw.Core.Controller.MoveWindows(-mw.Core.Controller.GetWindowsDistanceLeft() / mw.Core.Controller.ZoomRatio, 0);
                    }
                    else if (mw.Core.Controller.GetWindowsDistanceRight() < -0.25 * mw.Width && mw.Core.Controller.GetWindowsDistanceLeft() < System.Windows.SystemParameters.PrimaryScreenWidth)
                    {
                        mw.Core.Controller.MoveWindows(mw.Core.Controller.GetWindowsDistanceRight() / mw.Core.Controller.ZoomRatio, 0);
                    }
                }
            });
        }


        public bool RePostionActive = true;

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

    }
}
