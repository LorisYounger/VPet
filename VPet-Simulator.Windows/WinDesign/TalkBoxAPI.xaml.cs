using LinePutScript.Localization.WPF;
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
                m.SayRnd("请先前往设置中设置 ChatGPT API".Translate());
                return;
            }
            Dispatcher.Invoke(() => this.IsEnabled = false);
            try
            {
                var resp = mw.CGPTClient.Ask("vpet", content);
                var reply = resp.GetMessageContent();
                if (resp.choices[0].finish_reason == "length")
                {
                    reply += " ...";
                }
                var showtxt = "当前Token使用".Translate() + ": " + resp.usage.total_tokens;
                Dispatcher.Invoke(() =>
                {
                    m.MsgBar.MessageBoxContent.Children.Add(new TextBlock() { Text = showtxt, FontSize = 20, ToolTip = showtxt, HorizontalAlignment = HorizontalAlignment.Right });
                });
                m.SayRnd(reply);
            }
            catch (Exception exp)
            {
                var e = exp.ToString();
                string str = "请检查设置和网络连接".Translate();
                if (e.Contains("401"))
                {
                    str = "请检查API token设置".Translate();
                }
                m.SayRnd("API调用失败".Translate() + $",{str}\n{e}");//, GraphCore.Helper.SayType.Serious);
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
