using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
                var actualLeft = Convert.ToInt32(winGameSetting.leftGetter.GetValue(this));
                var actualTop = Convert.ToInt32(winGameSetting.topGetter.GetValue(this));
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
            mwCtrl.ScreenBorder = bounds;
            mw.winSetting.UpdateMoveAreaText();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
