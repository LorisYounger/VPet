using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// winMoveArea.xaml 的交互逻辑
    /// </summary>
    /// 跨平台: 原文复制自 VPet-Simulator.Windows/WinDesign/winMoveArea.xaml.cs; WindowX→VPetWindow,
    /// DragMove→BeginMoveDrag, 最大化时的左上角不用反射捞 (Avalonia 的 Position 在最大化时也是对的)
    public partial class winMoveArea : VPetWindow
    {
        MainWindow mw;
        public winMoveArea(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            //跨平台: Avalonia 没有 PreviewMouseDown, 用隧道路由挂
            BorderDrag.AddHandler(PointerPressedEvent, Grid_PreviewMouseDown, RoutingStrategies.Tunnel);
            ViewboxDrag1.AddHandler(PointerPressedEvent, Grid_PreviewMouseDown, RoutingStrategies.Tunnel);
            ViewboxDrag2.AddHandler(PointerPressedEvent, Grid_PreviewMouseDown, RoutingStrategies.Tunnel);
        }

        private void Grid_PreviewMouseDown(object? sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            var mwCtrl = mw.Core.Controller as MWController;
            System.Drawing.Rectangle bounds;
            if (WindowState == WindowState.Maximized)
            {
                bounds = new System.Drawing.Rectangle(
                    (int)Left, (int)Top,
                    (int)Bounds.Width, (int)Bounds.Height
                );
            }
            else
            {
                bounds = new System.Drawing.Rectangle(
                    (int)Left, (int)Top,
                    (int)Width, (int)Height
                );
            }
            mwCtrl!.ScreenBorder = bounds;
            mw.winSetting!.UpdateMoveAreaText();
            Close();
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
