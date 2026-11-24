using Panuon.WPF.UI;
using System;
using System.Windows;
using System.Windows.Input;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winMoveArea.xaml 的交互逻辑
    /// </summary>
    public partial class winMoveArea : WindowX
    {
        MainWindow mw;
        public winMoveArea(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
        }

        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var mwCtrl = mw.Core.Controller as MWController;
            System.Drawing.Rectangle bounds;
            if (WindowState == WindowState.Maximized)
            {
                // 反射捞一下左上角
                if (winGameSetting.leftGetter == null) winGameSetting.leftGetter = typeof(Window).GetField("_actualLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (winGameSetting.topGetter == null) winGameSetting.topGetter = typeof(Window).GetField("_actualTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var actualLeft = Convert.ToInt32(winGameSetting.leftGetter?.GetValue(this));
                var actualTop = Convert.ToInt32(winGameSetting.topGetter?.GetValue(this));
                bounds = new System.Drawing.Rectangle(
                    actualLeft, actualTop,
                    (int)ActualWidth, (int)ActualHeight
                );
            }
            else
            {
                bounds = new System.Drawing.Rectangle(
                    (int)Left, (int)Top,
                    (int)Width, (int)Height
                );
            }
            if (mwCtrl == null)
            {
                Close();
                return;
            }
            var screens = ScreenTopology.GetScreens(mw);
            var rect = ScreenTopology.FromDrawingRectangle(bounds);
            if (ScreenTopology.TryRepairMoveArea(screens, rect, out var repaired))
            {
                if (repaired.IsEmpty)
                {
                    repaired = ScreenTopology.GetPrimaryScreenRect(screens);
                }
                bounds = ScreenTopology.ToDrawingRectangle(repaired);
            }
            mw.Set.AutoChangeWindow = false;
            mwCtrl.ScreenBorder = bounds;
            mwCtrl.EnsureWindowInBounds();
            mw.winSetting?.UpdateMoveAreaText();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
