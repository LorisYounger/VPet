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

        private readonly struct BoundarySnapshot
        {
            public BoundarySnapshot(double left, double right, double up, double down, double width, double height)
            {
                Left = left;
                Right = right;
                Up = up;
                Down = down;
                Width = width;
                Height = height;
            }

            public double Left { get; }
            public double Right { get; }
            public double Up { get; }
            public double Down { get; }
            public double Width { get; }
            public double Height { get; }
        }

        private BoundarySnapshot GetBoundarySnapshot()
        {
            if (AutoChangeWindow && TryGetCurrentMonitorSnapshot(out var monitorSnapshot))
                return monitorSnapshot;

            if (IsPrimaryScreen)
            {
                return new BoundarySnapshot(
                    mw.Left,
                    SystemParameters.PrimaryScreenWidth - mw.Left - mw.ActualWidth,
                    mw.Top,
                    SystemParameters.PrimaryScreenHeight - mw.Top - mw.ActualHeight,
                    SystemParameters.PrimaryScreenWidth,
                    SystemParameters.PrimaryScreenHeight);
            }

            return new BoundarySnapshot(
                mw.Left - ScreenBorder.X,
                ScreenBorder.Right - mw.Left - mw.ActualWidth,
                mw.Top - ScreenBorder.Y,
                ScreenBorder.Bottom - mw.Top - mw.ActualHeight,
                ScreenBorder.Width,
                ScreenBorder.Height);
        }

        private bool TryGetCurrentMonitorSnapshot(out BoundarySnapshot snapshot)
        {
            snapshot = default;
            var handle = GetWindowHandle();
            if (!ScreenNative.TryGetMonitorBounds(handle, out var monitorBounds, out var windowBounds))
                return false;

            var source = HwndSource.FromHwnd(handle);
            var transform = source?.CompositionTarget?.TransformToDevice;
            var scaleX = transform?.M11 ?? 1;
            var scaleY = transform?.M22 ?? 1;
            if (scaleX <= 0 || scaleY <= 0)
                return false;

            // Convert only local physical distances. Global virtual-desktop coordinates do not share one DPI scale.
            snapshot = new BoundarySnapshot(
                (windowBounds.Left - monitorBounds.Left) / scaleX,
                (monitorBounds.Right - windowBounds.Right) / scaleX,
                (windowBounds.Top - monitorBounds.Top) / scaleY,
                (monitorBounds.Bottom - windowBounds.Bottom) / scaleY,
                monitorBounds.Width / scaleX,
                monitorBounds.Height / scaleY);
            return true;
        }

        public double GetWindowsDistanceLeft()
        {
            return mw.Dispatcher.Invoke(() => GetBoundarySnapshot().Left);
        }

        public double GetWindowsDistanceUp()
        {
            return mw.Dispatcher.Invoke(() => GetBoundarySnapshot().Up);
        }

        public double GetWindowsDistanceRight()
        {
            return mw.Dispatcher.Invoke(() => GetBoundarySnapshot().Right);
        }

        public double GetWindowsDistanceDown()
        {
            return mw.Dispatcher.Invoke(() => GetBoundarySnapshot().Down);
        }

        public void MoveWindows(double X, double Y)
        {
            if (X == 0 && Y == 0)
                return;

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
                if (mw.Dispatcher.HasShutdownStarted || mw.Dispatcher.HasShutdownFinished) return true;
                if (mw.winSetting != null && mw.winSetting.Visibility == Visibility.Visible) return true;
                if (mw.winBetterBuy != null && mw.winBetterBuy.Visibility == Visibility.Visible) return true;
                if (mw.winWorkMenu != null && mw.winWorkMenu.Visibility == Visibility.Visible) return true;
                if (mw.winMutiPlayer != null && mw.winMutiPlayer.Visibility == Visibility.Visible) return true;
                for (int i = 0; i < mw.Windows.Count; i++)
                {
                    if (mw.Windows[i] != null && mw.Windows[i].Visibility == Visibility.Visible) return true;
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
                var screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    if (screens[i].DeviceName == currentScreen.DeviceName)
                    {
                        mw.Set.GameScreenIndex = i;
                        break;
                    }
                }

                // Automatic screen selection is runtime-only and must not overwrite a saved MoveArea.
                if (AutoChangeWindow)
                    return;

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
                var bounds = GetBoundarySnapshot();
                if (bounds.Up < -0.25 * mw.ActualHeight && bounds.Down < bounds.Height)
                {
                    MoveWindows(0, -bounds.Up / ZoomRatio);
                }
                else if (bounds.Down < -0.25 * mw.ActualHeight && bounds.Up < bounds.Height)
                {
                    MoveWindows(0, bounds.Down / ZoomRatio);
                }
                if (bounds.Left < -0.25 * mw.ActualWidth && bounds.Right < bounds.Width)
                {
                    MoveWindows(-bounds.Left / ZoomRatio, 0);
                }
                else if (bounds.Right < -0.25 * mw.ActualWidth && bounds.Left < bounds.Width)
                {
                    MoveWindows(bounds.Right / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mw.Dispatcher.Invoke(() =>
        {
            var bounds = GetBoundarySnapshot();
            return bounds.Up < -0.25 * mw.ActualHeight && bounds.Down < bounds.Height
                || bounds.Down < -0.25 * mw.ActualHeight && bounds.Up < bounds.Height
                || bounds.Left < -0.25 * mw.ActualWidth && bounds.Right < bounds.Width
                || bounds.Right < -0.25 * mw.ActualWidth && bounds.Left < bounds.Width;
        });

        public bool RePositionActive { get; set; } = true;

        public IntPtr GetWindowHandle()
        {
            if (mw.Dispatcher.HasShutdownStarted || mw.Dispatcher.HasShutdownFinished)
                return IntPtr.Zero;

            try
            {
                return mw.Dispatcher.Invoke(() =>
                {
                    var helper = new WindowInteropHelper(mw);
                    return helper.Handle == IntPtr.Zero ? helper.EnsureHandle() : helper.Handle;
                });
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public double ZoomRatio => mw.Set.ZoomLevel;

        public int PressLength => mw.Set.PressLength;

        public bool EnableFunction => mw.Set.EnableFunction;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;
    }
}
