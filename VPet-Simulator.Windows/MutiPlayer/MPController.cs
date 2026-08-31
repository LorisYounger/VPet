using System;
using System.Windows.Interop;
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

            if (mw.MWController.IsPrimaryScreen)
            {
                return new BoundarySnapshot(
                    mp.Left,
                    System.Windows.SystemParameters.PrimaryScreenWidth - mp.Left - mp.ActualWidth,
                    mp.Top,
                    System.Windows.SystemParameters.PrimaryScreenHeight - mp.Top - mp.ActualHeight,
                    System.Windows.SystemParameters.PrimaryScreenWidth,
                    System.Windows.SystemParameters.PrimaryScreenHeight);
            }

            var moveArea = mw.MWController.ScreenBorder;
            return new BoundarySnapshot(
                mp.Left - moveArea.X,
                moveArea.Right - mp.Left - mp.ActualWidth,
                mp.Top - moveArea.Y,
                moveArea.Bottom - mp.Top - mp.ActualHeight,
                moveArea.Width,
                moveArea.Height);
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
            return mp.Dispatcher.Invoke(() => GetBoundarySnapshot().Left);
        }

        public double GetWindowsDistanceUp()
        {
            return mp.Dispatcher.Invoke(() => GetBoundarySnapshot().Up);
        }

        public double GetWindowsDistanceRight()
        {
            return mp.Dispatcher.Invoke(() => GetBoundarySnapshot().Right);
        }

        public double GetWindowsDistanceDown()
        {
            return mp.Dispatcher.Invoke(() => GetBoundarySnapshot().Down);
        }

        public void MoveWindows(double X, double Y)
        {
            if (X == 0 && Y == 0)
                return;

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
                var bounds = GetBoundarySnapshot();
                if (bounds.Up < -0.25 * mp.ActualHeight && bounds.Down < bounds.Height)
                {
                    MoveWindows(0, -bounds.Up / ZoomRatio);
                }
                else if (bounds.Down < -0.25 * mp.ActualHeight && bounds.Up < bounds.Height)
                {
                    MoveWindows(0, bounds.Down / ZoomRatio);
                }
                if (bounds.Left < -0.25 * mp.ActualWidth && bounds.Right < bounds.Width)
                {
                    MoveWindows(-bounds.Left / ZoomRatio, 0);
                }
                else if (bounds.Right < -0.25 * mp.ActualWidth && bounds.Left < bounds.Width)
                {
                    MoveWindows(bounds.Right / ZoomRatio, 0);
                }
            });
        }
        public bool CheckPosition() => mp.Dispatcher.Invoke(() =>
        {
            var bounds = GetBoundarySnapshot();
            return bounds.Up < -0.25 * mp.ActualHeight && bounds.Down < bounds.Height
                || bounds.Down < -0.25 * mp.ActualHeight && bounds.Up < bounds.Height
                || bounds.Left < -0.25 * mp.ActualWidth && bounds.Right < bounds.Width
                || bounds.Right < -0.25 * mp.ActualWidth && bounds.Left < bounds.Width;
        });

        public bool RePositionActive { get; set; } = true;

        public IntPtr GetWindowHandle()
        {
            if (mp.Dispatcher.HasShutdownStarted || mp.Dispatcher.HasShutdownFinished)
                return IntPtr.Zero;

            try
            {
                return mp.Dispatcher.Invoke(() =>
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(mp);
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

        public bool EnableFunction => false;

        public int InteractionCycle => mw.Set.InteractionCycle;

        public bool AutoChangeWindow => mw.Set.AutoChangeWindow;

        public bool IfInActivateScreen()
        {
            try
            {
                if (mp.Dispatcher.HasShutdownStarted || mp.Dispatcher.HasShutdownFinished) return false;
            }
            catch { }
            return mp.Dispatcher.Invoke(() =>
            {

                try
                {
                    var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(mp).Handle);
                    var screens = System.Windows.Forms.Screen.AllScreens;
                    for (int i = 0; i < screens.Length; i++)
                    {
                        if (screens[i].DeviceName == screen.DeviceName)
                        {
                            if (i == mw.Set.GameScreenIndex)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            });
        }

        public void SetNowScreenActivate()
        {
            
        }
    }
}
