using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Timers;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// ToolBar.xaml 的交互逻辑
    /// </summary>
    public partial class ToolBar : UserControl
    {
        Main m;
        Timer closetimer;
        bool onFocus = false;
        public ToolBar(Main m)
        {
            InitializeComponent();
            this.m = m;
            closetimer = new Timer()
            {
                Interval = 4000,
                AutoReset = false,
                Enabled = false
            };
            closetimer.Elapsed += Closetimer_Elapsed;
        }

        private void Closetimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (onFocus)
            {
                onFocus = false;
                closetimer.Start();
            }
            else
                Dispatcher.Invoke(() => this.Visibility = Visibility.Collapsed);
        }

        public void Show()
        {
            Visibility = Visibility.Visible;
            if (closetimer.Enabled)
                onFocus = true;
            else
                closetimer.Start();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            closetimer.Stop();
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            closetimer.Start();
        }
    }
}
