using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class MessageBar : UserControl, IDisposable
    {
        Main m;
        public MessageBar(Main m)
        {
            InitializeComponent();
            EndTimer.Elapsed += EndTimer_Elapsed;
            ShowTimer.Elapsed += ShowTimer_Elapsed;
            CloseTimer.Elapsed += CloseTimer_Elapsed;
            this.m = m;
        }

        private void CloseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Invoke(() => Opacity) <= 0.05)
            {
                Dispatcher.Invoke(() => this.Visibility = Visibility.Collapsed);
                EndAction?.Invoke();
            }
            else
            {
                Dispatcher.Invoke(() => Opacity -= 0.02);
            }
        }

        List<char> outputtext;
        private void ShowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (outputtext.Count > 0)
            {
                var str = outputtext[0];
                outputtext.RemoveAt(0);
                Dispatcher.Invoke(() => { TText.Text += str; });
            }
            else
            {
                if (m.PlayingVoice)
                {
                    TimeSpan ts = Dispatcher.Invoke(() => m.VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true ? (m.VoicePlayer.Clock.NaturalDuration.TimeSpan - m.VoicePlayer.Clock.CurrentTime.Value) : TimeSpan.Zero);
                    if (ts.TotalSeconds > 2)
                    {
                        return;
                    }
                    else
                    {
                        Console.WriteLine(1);
                    }
                }
                Task.Run(() =>
                {
                    Thread.Sleep(timeleft * 50);
                    if (sayType != GraphCore.Helper.SayType.None && m.DisplayType.ToString().StartsWith("Say"))
                        m.Display(GraphCore.Helper.Convert(sayType, GraphCore.Helper.AnimatType.C_End), m.DisplayToNomal);
                });
                ShowTimer.Stop();
                EndTimer.Start();
            }
        }
        public Action EndAction;
        private void EndTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (--timeleft <= 0)
            {
                EndTimer.Stop();
                CloseTimer.Start();
            }
        }

        public Timer EndTimer = new Timer() { Interval = 200 };
        public Timer ShowTimer = new Timer() { Interval = 50 };
        public Timer CloseTimer = new Timer() { Interval = 20 };
        int timeleft;
        GraphCore.Helper.SayType sayType;
        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="text">内容</param>
        public void Show(string name, string text, GraphCore.Helper.SayType sayType)
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                Panel.SetZIndex(this, m.UIGrid.Children.Count);
            }
            TText.Text = "";
            outputtext = text.ToList();
            LName.Content = name;
            timeleft = text.Length + 5;
            ShowTimer.Start(); EndTimer.Stop(); CloseTimer.Stop();
            this.Visibility = Visibility.Visible;
            Opacity = .8;
            this.sayType = sayType;
        }

        public void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            EndTimer.Stop();
            CloseTimer.Stop();
            this.Opacity = .8;
        }

        public void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!ShowTimer.Enabled)
                EndTimer.Start();
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ForceClose();
        }
        /// <summary>
        /// 强制关闭
        /// </summary>
        public void ForceClose()
        {
            EndTimer.Stop(); ShowTimer.Stop(); CloseTimer.Close();
            this.Visibility = Visibility.Collapsed;
            EndAction?.Invoke();
        }
        public void Dispose()
        {
            EndTimer.Dispose();
            ShowTimer.Dispose();
            CloseTimer.Dispose();
        }
    }
}
