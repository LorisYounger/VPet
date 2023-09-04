using System.Windows.Forms;
using System.Windows.Interop;
using System.Drawing;
using VPet_Simulator.Core;
using System.Windows;
using System.Reflection;
using System;

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

        private char _screenDetected;
        private Screen _cachedScreen;
        private bool _isScaled;
        public Screen CurrentScreen
        {
            get
            {
                if (_screenDetected <= 0) DetectCurrentScreen();
                return _cachedScreen;
            }
        }
        public bool IsScaled => _isScaled;
        private void DetectCurrentScreen()
        {
            _isScaled = IsPixelScaled;

            Screen currentScreen;
            // 更重的遍历拿屏幕方式
            if (_isScaled)
            {
                currentScreen = GetNearestScreen();
            }
            // 普通拿屏幕方式
            else
            {
                var windowInteropHelper = new WindowInteropHelper(mw);
                currentScreen = Screen.FromHandle(windowInteropHelper.Handle);
            }

            _cachedScreen = currentScreen;
            _screenDetected = (char)10;
        }
        public void ClearScreenBorderCache(bool weak = false)
        {
            if (weak && _screenDetected > 0) _screenDetected--;
            else _screenDetected = (char)0;
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (CurrentScreen.Primary) return mw.Left;
                if (IsScaled) return GetScreenLeftScaled(CurrentScreen);
                return GetScreenLeft(CurrentScreen);
            });
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (CurrentScreen.Primary) return mw.Top;
                if (IsScaled) return GetScreenUpScaled(CurrentScreen);
                return GetScreenUp(CurrentScreen);
            });
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (CurrentScreen.Primary) return SystemParameters.PrimaryScreenWidth - mw.Left - mw.Width;
                if (IsScaled) return GetScreenRightScaled(CurrentScreen);
                return GetScreenRight(CurrentScreen);
            });
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() =>
            {
                if (CurrentScreen.Primary) return SystemParameters.PrimaryScreenHeight - mw.Top - mw.Height;
                if (IsScaled) return GetScreenDownScaled(CurrentScreen);
                return GetScreenDown(CurrentScreen);
            });
        }

        public void MoveWindows(double X, double Y)
        {
            mw.Dispatcher.Invoke(() =>
            {
                mw.Left += X * ZoomRatio;
                mw.Top += Y * ZoomRatio;
                ClearScreenBorderCache(true);
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

        #region 各种屏幕计算函数
        // 反射拿分辨率缩放
        static MethodInfo _convertPixel;
        static object[] _convertPixelInput = new object[1];
        static void _reflectConvertPixel()
        {
            if (_convertPixel == null)
            {
                _convertPixel = typeof(SystemParameters).GetMethod("ConvertPixel", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }
        static double ScalePixel(double original)
        {
            _reflectConvertPixel();
            _convertPixelInput[0] = (int)original;
            return (double)_convertPixel.Invoke(null, _convertPixelInput);
        }
        static double ReverseScalePixel(double original)
        {
            if (original == 0) return 0;
            var inverseVal = ScalePixel(original);
            return original * original / inverseVal;
        }
        static bool IsPixelScaled => ScalePixel(114) != 114;
        // 屏幕四边距离
        double GetScreenLeft(Screen target) => mw.Left - target.Bounds.X;
        double GetScreenRight(Screen target) => target.Bounds.Width + target.Bounds.X - mw.Left - mw.Width;
        double GetScreenUp(Screen target) => mw.Top - target.Bounds.Y;
        double GetScreenDown(Screen target) => target.Bounds.Height + target.Bounds.Y - mw.Top - mw.Height;
        #region 带缩放版本
        // 窗口参数
        double ScaledLeft => ReverseScalePixel(mw.Left);
        double ScaledTop => ReverseScalePixel(mw.Top);
        double ScaledWidth => ReverseScalePixel(mw.Width);
        double ScaledHeight => ReverseScalePixel(mw.Height);
        // 现象：150%主屏右侧副屏，副屏内部分区域被识别为主屏，数值位移较显示位移偏小
        double GetScreenLeftScaled(Screen target) => ScalePixel(ScaledLeft - target.Bounds.X);
        double GetScreenRightScaled(Screen target) => ScalePixel(target.Bounds.Width + target.Bounds.X - ScaledLeft - ScaledWidth);
        double GetScreenUpScaled(Screen target) => ScalePixel(ScaledTop - target.Bounds.Y);
        double GetScreenDownScaled(Screen target) => ScalePixel(target.Bounds.Height + target.Bounds.Y - ScaledTop - ScaledHeight);

        double GetScreenMinDist(Screen target)
        {
            return Math.Min(
                Math.Min(
                    GetScreenLeftScaled(target),
                    GetScreenRightScaled(target)
                ),
                Math.Min(
                    GetScreenUpScaled(target),
                    GetScreenDownScaled(target)
                )
            );
        }
        Screen GetNearestScreen(Screen except = null, double minDistTolerance = double.NegativeInfinity)
        {
            // TODO 还有bug，待试验
            Screen ret = null;
            var minDist = minDistTolerance;
            foreach (var s in Screen.AllScreens)
            {
                if (s == except) continue;
                var dist = GetScreenMinDist(s);
                if (dist > minDist)
                {
                    minDist = dist;
                    ret = s;
                }
            }
            return ret;
        }
        #endregion

        #endregion
    }
}
