using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class TalkBoxAPI : UserControl
    {
        Main m;
        MainWindow mw;
        public TalkBoxAPI(MainWindow mw)
        {
            InitializeComponent();
            this.m = mw.Main;
            this.mw = mw;
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbTalk.Text))
            {
                return;
            }
            var cont = tbTalk.Text;
            tbTalk.Text = "";
            Task.Run(() => OPENAI(cont));
        }
        /// <summary>
        /// 使用OPENAI API进行回复
        /// </summary>
        /// <param name="content">内容 说话内容</param>
        public void OPENAI(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            if (mw.CGPTClient == null)
            {
                m.Say("请先前往设置中设置 ChatGPT API", GraphCore.Helper.SayType.Serious);
                return;
            }
            Dispatcher.Invoke(() => this.IsEnabled = false);
            try
            {
                m.SayRnd(mw.CGPTClient.Ask("vpet", content).GetMessageContent());
            }
            catch (Exception exp)
            {
                var e = exp.ToString();
                string str = "请检查设置和网络连接";
                if (e.Contains("401"))
                {
                    str = "请检查API token设置";
                }
                m.Say($"API调用失败,{str}\n{e}", GraphCore.Helper.SayType.Serious);
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);
        }
        private void tbTalk_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SendMessage_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
