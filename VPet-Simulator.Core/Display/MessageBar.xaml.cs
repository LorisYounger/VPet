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
            EndTimer.Elapsed += EndTimer_Elapsed;
            ShowTimer.Elapsed += ShowTimer_Elapsed;
        }
        List<char> outputtext;
        private void ShowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (outputtext.Count > 0)
            {
                Dispatcher.Invoke(() => { TText.Text += outputtext[0]; outputtext.RemoveAt(0); });
            }
            else
            {
                ShowTimer.Stop();
                EndTimer.Start();
            }
        }

        private void EndTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (--timeleft <= 0)
                Dispatcher.Invoke(() => this.Visibility = Visibility.Collapsed);
        }

        public Timer EndTimer = new Timer() { Interval = 100 };
        public Timer ShowTimer = new Timer() { Interval = 20 };
        int timeleft;
        public void Show(string name, string text)
        {
            TText.Text = "";
            outputtext = text.ToList();
            LName.Content = name;
            timeleft = text.Length + 5;
            ShowTimer.Start(); EndTimer.Stop();
            this.Visibility = Visibility.Visible;
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            EndTimer.Stop();
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!ShowTimer.Enabled)
                EndTimer.Start();
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EndTimer.Stop(); ShowTimer.Stop();
            this.Visibility = Visibility.Collapsed;
        }
    }
}
