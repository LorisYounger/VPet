using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Timer = System.Timers.Timer;
using static VPet_Simulator.Core.Main;

namespace VPet_Simulator.Core
{
    public interface IMassageBar : IDisposable
    {
        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="text">内容</param>
        /// <param name="graphName">图像名</param>
        /// <param name="msgContent">消息框内容</param>
        void Show(string name, string text, string graphName = null, UIElement msgContent = null);


        /// <summary>
        /// 显示流式消息
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="sayInfoWithStream">内容</param>
        void Show(string name, SayInfoWithStream sayInfoWithStream);
        /// <summary>
        /// 强制关闭
        /// </summary>
        void ForceClose();
        /// <summary>
        /// 设置位置在桌宠内
        /// </summary>
        void SetPlaceIN();
        /// <summary>
        /// 设置位置在桌宠外
        /// </summary>
        void SetPlaceOUT();
        /// <summary>
        /// 显示状态
        /// </summary>
        Visibility Visibility { get; set; }
        /// <summary>
        /// 该消息框的Control
        /// </summary>
        Control This { get; }
        /// <summary>
        /// 被关闭时事件
        /// </summary>
        event Action EndAction;
    }
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class MessageBar : UserControl, IDisposable, IMassageBar
    {
        public Control This => this;
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
                CloseTimer.Stop();
                Dispatcher.Invoke(() =>
                {
                    Opacity = 1;
                    this.Visibility = Visibility.Collapsed;
                    MessageBoxContent.Children.Clear();
                });
                EndAction?.Invoke();
            }
            else
            {
                Dispatcher.Invoke(() => Opacity -= 0.02);
            }
        }

        List<char> outputtext;
        StringBuilder outputtextsample = new StringBuilder();
        private void ShowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (outputtext.Count > 0)
            {
                // 处理2-3个字符，平衡效果和性能
                int batchSize = Math.Min(2, outputtext.Count);
                string textToAdd = string.Empty;

                for (int i = 0; i < batchSize; i++)
                {
                    textToAdd += outputtext[0];
                    outputtext.RemoveAt(0);
                }
                outputtextsample.Append(textToAdd);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TText.Text = outputtextsample.ToString();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                // 其余代码保持不变
                if (m.PlayingVoice)
                {
                    if (m.windowMediaPlayerAvailable)
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
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (m.soundPlayer.IsLoadCompleted)
                            {
                                m.PlayingVoice = false;
                                m.soundPlayer.PlaySync();
                            }
                        });
                    }
                }
                ShowTimer.Stop();
                EndTimer.Start();
                if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                    m.DisplayCEndtoNomal(m.DisplayType.Name);
            }
        }
        /// <summary>
        /// 被关闭时事件
        /// </summary>
        public event Action EndAction;
        private void EndTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (--timeleft <= 0)
            {
                EndTimer.Stop();
                CloseTimer.Start();
            }
        }

        public Timer EndTimer = new Timer() { Interval = 200 };
        public Timer ShowTimer = new Timer() { Interval = 100 };
        public Timer CloseTimer = new Timer() { Interval = 20 };
        int timeleft;
        string graphName;
        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="text">内容</param>
        public void Show(string name, string text, string graphName = null, UIElement msgContent = null)
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                Panel.SetZIndex(this, m.UIGrid.Children.Count - 1);
            }
            MessageBoxContent.Children.Clear();
            TText.Text = "";
            outputtext = text.ToList();
            outputtextsample.Clear();
            LName.Content = name;
            timeleft = text.Length + 5;
            ShowTimer.Start(); EndTimer.Stop(); CloseTimer.Stop();
            this.Visibility = Visibility.Visible;
            Opacity = .8;
            this.graphName = graphName;
            if (msgContent != null)
            {
                MessageBoxContent.Children.Add(msgContent);
            }
        }

        /// <summary>
        /// 确认是否完成 防止多次调用
        /// </summary>
        private bool finished;
        /// <summary>
        /// 流式传输模式 显示文字
        /// </summary>
        public void Show(string name, SayInfoWithStream sayInfoWithStream)
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                Panel.SetZIndex(this, m.UIGrid.Children.Count - 1);
            }

            MessageBoxContent.Children.Clear();
            TText.Text = "";
            LName.Content = name;
            ShowTimer.Stop();
            EndTimer.Stop();
            CloseTimer.Stop();
            this.Visibility = Visibility.Visible;
            Opacity = .8;
            graphName = sayInfoWithStream.GraphName;
            finished = false;

            var msgcontent = sayInfoWithStream.MsgContent ?? (string.IsNullOrWhiteSpace(sayInfoWithStream.Desc)
                ? null
                : new TextBlock()
                {
                    Text = sayInfoWithStream.Desc,
                    FontSize = 20,
                    ToolTip = sayInfoWithStream.Desc,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
            if (msgcontent != null)
            {
                MessageBoxContent.Children.Add(msgcontent);
            }

            Dispatcher.Invoke(() => { TText.Text = sayInfoWithStream.CurrentText.ToString(); });

            sayInfoWithStream.Event_Update += DealWithUpdate;
            sayInfoWithStream.Event_Finish += (_) => DealWithStreamFinish();
            if (sayInfoWithStream.IsFinishGen)
            {
                DealWithStreamFinish();
            }
        }

        /// <summary>
        /// 增加显示新词
        /// </summary>
        /// <param name="data">更新内容</param>
        public void DealWithUpdate((string fullText, string changedText) data)
        {
            timeleft = data.fullText.Length;
            Dispatcher.Invoke(() => { TText.Text = data.fullText; });
        }
        /// <summary>
        /// 处理流式传输结束
        /// </summary>
        public void DealWithStreamFinish()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            if (m.PlayingVoice)
            {
                if (m.windowMediaPlayerAvailable)
                {
                    TimeSpan ts = Dispatcher.Invoke(() => m.VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true ? (m.VoicePlayer.Clock.NaturalDuration.TimeSpan - m.VoicePlayer.Clock.CurrentTime.Value) : TimeSpan.Zero);
                    if (ts.TotalSeconds > 2)
                    {
                        return;
                    }
                }
                else
                {
                    if (m.soundPlayer.IsLoadCompleted)
                    {
                        m.PlayingVoice = false;
                        m.soundPlayer.PlaySync();
                    }
                }
            }
            EndTimer.Start();
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
            MessageBoxContent.Children.Clear();
            if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                m.DisplayCEndtoNomal(m.DisplayType.Name);
            EndAction?.Invoke();
        }
        public void Dispose()
        {
            EndTimer.Dispose();
            ShowTimer.Dispose();
            CloseTimer.Dispose();
        }
        public void SetPlaceIN()
        {
            this.Height = 500;
            BorderMain.VerticalAlignment = VerticalAlignment.Bottom;
            Margin = new Thickness(0);
        }
        public void SetPlaceOUT()
        {
            this.Height = double.NaN;
            BorderMain.VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(0, 500, 0, 0);
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TText.Text);
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            ForceClose();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => Border_MouseEnter(null, null);

        private void ContextMenu_Closed(object sender, RoutedEventArgs e) => Border_MouseLeave(null, null);

        private void TText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            sv.ScrollToEnd();
        }
    }
}
