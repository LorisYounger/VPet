using LinePutScript;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet_Simulator.Core;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class TalkBoxAPI : UserControl, ITalkBox
    {
        Main m;
        MainWindow mw;

        public UIElement This => this;

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
                m.Say(mw.CGPTClient.Ask("vpet", content).GetMessageContent(), GraphCore.Helper.SayType.Serious);
            }
            catch (Exception exp)
            {
                m.Say("API调用失败,请检查设置和网络连接\n" + exp.ToString(), GraphCore.Helper.SayType.Serious);
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);
        }
    }
}
