using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class MessageBar : UserControl
    {
        public MessageBar()
        {
            InitializeComponent();
            ShowTimer.Elapsed += ShowTimer_Elapsed;
        }

        private void ShowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (--timeleft <= 0)
                Dispatcher.Invoke(() => this.Visibility = Visibility.Collapsed);
        }

        public Timer ShowTimer = new Timer() { Interval = 100 };
        int timeleft;
        public void Show(string name, string text)
        {
            TText.Text = text;
            LName.Content = name;
            timeleft = text.Length * 2 + 10;
            ShowTimer.Start();
            this.Visibility = Visibility.Visible;
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowTimer.Stop();
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowTimer.Start();
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowTimer.Stop();
            this.Visibility = Visibility.Collapsed;
        }
    }
}
