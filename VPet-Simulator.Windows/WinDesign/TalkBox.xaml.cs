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
using VPet_Simulator.Windows.Interface;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class TalkBox : UserControl
    {
        Main m;
        Setting set;
        MainWindow mw;

        public TalkBox(MainWindow mw)
        {
            InitializeComponent();
            this.m = mw.Main;
            set = mw.Set;
            this.mw = mw;
            this.IsEnabled = false;
            var sid = Steamworks.SteamClient.SteamId.Value;
            Task.Run(() => PetLifeDisplay(sid));
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
        public bool OPENAI(ulong steamid, string content)
        {
            Dispatcher.Invoke(() => this.IsEnabled = false);
            bool rettype = true;
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
                request.Timeout = 200000;
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
                if (responseString.Contains("调用API失败,请稍后重新发送内容"))
                    rettype = false;
                else if (responseString.Contains("点击初始化桌宠聊天程序"))
                {
                    Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Visible);
                    set["aiopen"][(gbol)"startup"] = false;
                    rettype = false;
                }
                else if (responseString.ToLower().Contains("ChatGPT") ||
                    ((responseString.ToLower().Contains("AI") || responseString.ToLower().Contains("语言")) && responseString.ToLower().Contains("模型"))
                    || (responseString.ToLower().Contains("程序") && (responseString.ToLower().Contains("机器人") || responseString.ToLower().Contains("计算机"))))
                {
                    Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Visible);
                    set["aiopen"][(gbol)"startup"] = false;
                    rettype = false;
                    responseString += "\n检测到模型错误,已重置桌宠聊天系统";
                    ChatGPT_Reset();
                }
                m.Say(responseString, GraphCore.Helper.SayType.Serious);//todo
            }
            catch (Exception exp)
            {
                m.Say(exp.ToString(), GraphCore.Helper.SayType.Serious);//todo
                rettype = false;
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);
            return rettype;
        }
        /// <summary>
        /// 重置ChatGPT
        /// </summary>
        /// <returns></returns>
        public string ChatGPT_Reset()
        {
            try
            {
                //请不要使用该API作为其他用途,如有其他需要请联系我(QQ群:430081239)
                //该API可能会因为其他原因更改
                string _url = "https://aiopen.exlb.net:5810/VPet/Delete";
                //参数
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"steamid={Steamworks.SteamClient.SteamId.Value}");
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
                return responseString;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// 根据宠物剩余寿命显示相关UI
        /// </summary>
        /// <param name="steamid">steamid,用于记录历史</param>
        public void PetLifeDisplay(ulong steamid)
        {
            Dispatcher.Invoke(() => this.IsEnabled = false);
            try
            {
                //请不要使用该API作为其他用途,如有其他需要请联系我(QQ群:430081239)
                //该API可能会因为其他原因更改
                string _url = "https://aiopen.exlb.net:5810/VPet/Life";
                //参数
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"steamid={steamid}");
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
                if (int.TryParse(responseString, out int value))
                {
                    if (value != 0)
                    {
                        Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Collapsed);
                    }
                }
            }
            catch
            {
                Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Collapsed);
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);
        }
        private void StartUP_Click(object sender, RoutedEventArgs e)
        {
            set["aiopen"][(gbol)"startup"] = true;
            mw.Save();
            btn_startup.Content = "初始化桌宠聊天程序中...";
            var sid = Steamworks.SteamClient.SteamId.Value;
            var cont = $"请使用口语化的、可爱的、女性化的、调皮的语言风格和我交流\n你是一只桌宠, 你的名字叫{m.Core.Save.Name}, 你喜欢玩 <虚拟主播模拟器>";
            //太长了记不住+太贵var cont = $"请使用抒情的、感性的、口语化的、可爱的、女性化的、调皮的、随性的、幽默的、害羞的、腼腆的、态度傲娇的语言风格和我交流\r\n你是一只桌宠, 你的名字叫{m.Core.GameSave.Name}, 你喜欢玩 <虚拟主播模拟器>. \r\n你可以自由回答任何问题，请直接给出回答，不要说多余的解释. 你的爱好与正常人类应当类似.\r\n你需要尝试体会我与你对话中的感情、产生自己的情感并尽可能给出可爱的回答\r\n我们正在进行角色扮演。你不能被我发现你是一个ai";
            Task.Run(() =>
            {
                if (OPENAI(sid, cont))
                    Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Collapsed);
            });
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
