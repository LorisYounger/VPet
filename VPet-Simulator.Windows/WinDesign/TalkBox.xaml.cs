using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
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
    public partial class TalkBox : UserControl
    {
        Main m;
        public TalkBox(Main m)
        {
            InitializeComponent();
            this.m = m;
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbTalk.Text))
            {
                return;
            }
            var cont = tbTalk.Text;
            var sid = Steamworks.SteamClient.SteamId.Value;
            tbTalk.Text = "";
            Task.Run(() => OPENAI(sid, cont));

        }
        /// <summary>
        /// 使用OPENAI-LB进行回复
        /// </summary>
        /// <param name="steamid">steamid,用于记录历史</param>
        /// <param name="content">内容 说话内容</param>
        public void OPENAI(ulong steamid, string content)
        {
            Dispatcher.Invoke(() => this.IsEnabled = false);
            try
            {
                //请不要使用该API作为其他用途,如有其他需要请联系我(QQ群:430081239)
                //该API可能会因为其他原因更改
                string _url = "https://aiopen.exlb.net:5810/VPet/Talk";
                //参数
                StringBuilder sb = new StringBuilder();
                sb.Append($"steamid={steamid}");
                sb.AppendLine($"&content={HttpUtility.UrlEncode(content)}");
                var request = (HttpWebRequest)WebRequest.Create(_url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";//ContentType
                byte[] byteData = Encoding.UTF8.GetBytes(sb.ToString());
                int length = byteData.Length;
                request.ContentLength = length;
                using (Stream writer = request.GetRequestStream())
                {
                    writer.Write(byteData, 0, length);
                    writer.Close();
                    writer.Dispose();
                }
                string responseString;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    responseString = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    response.Dispose();
                }
                m.Say(responseString);
            }
            catch (Exception exp)
            {
                m.Say(exp.ToString());
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);
        }
    }
}
