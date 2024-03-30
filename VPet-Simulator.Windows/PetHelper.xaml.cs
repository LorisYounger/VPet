using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// PetHelper.xaml 的交互逻辑
    /// </summary>
    public partial class PetHelper : WindowX
    {
        MainWindow mw;
        public void SetOpacity(bool isOn)
        {
            if (isOn)
                Dispatcher.Invoke(() => { Opacity = 0.8; });
            else
                Dispatcher.Invoke(() => { Opacity = 0.4; });
        }
        public PetHelper(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            //set = mf.Set["pethelp"];
            x = mw.Set.PetHelpLeft * mw.Width;
            y = mw.Set.PetHelpTop * mw.Width;
            Width = 50 * mw.Set.ZoomLevel;
            Height = 50 * mw.Set.ZoomLevel;
            SetLocation();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                Win32.User32.SetWindowLongPtr(new WindowInteropHelper(this).Handle,
                    Win32.GetWindowLongFields.GWL_HWNDPARENT, new WindowInteropHelper(mw).Handle);
            });
            ToolTip = "点击此处开关鼠标穿透\n右键开关置于顶层\n长按挪动位置\n可在设置中关闭".Translate();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isclick = true;
            Task.Run(() =>
            {
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                    {
                        if (isclick)
                        {
                            try
                            {
                                DragMove();
                                isdragmove = Opacity == 0.8;
                                Cursor = Cursors.Hand;
                                Opacity = 1;
                            }
                            catch
                            {
                            }

                        }
                    });
            });

        }
        bool? isdragmove = null;
        bool isclick = false;
        double x;
        double y;
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isclick = false;
            if (isdragmove.HasValue)
            {
                Cursor = Cursors.Arrow;
                SetOpacity(isdragmove.Value);
                isdragmove = null;
                x = Left - mw.Left;
                y = Top - mw.Top;
                mw.Set.PetHelpLeft = Math.Max(Math.Min(x / mw.Width, 1.1), -.1);
                mw.Set.PetHelpTop = Math.Max(Math.Min(y / mw.Width, 1.1), -.1);

                ReloadLocation();
                return;
            }
            mw.SetTransparentHitThrough();
        }
        public void ReloadLocation()
        {
            x = mw.Set.PetHelpLeft * mw.Width;
            y = mw.Set.PetHelpTop * mw.Width;
            SetLocation();
        }
        public void SetLocation()
        {
            this.Left = mw.Left + x;
            this.Top = mw.Top + y;
        }

        private void WindowX_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            mw.Topmost = !mw.Topmost;

            if (mw.Topmost == true && mw.HitThrough == true)
                mw.SetTransparentHitThrough();
            else
                SetOpacity(mw.Topmost);
        }
    }
}
