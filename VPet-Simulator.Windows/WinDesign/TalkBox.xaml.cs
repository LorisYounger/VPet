using LinePutScript;
using LinePutScript.Localization.WPF;
using Steamworks;
using System;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            lastopeningtime = DateTime.Now;
            Task.Run(TalkChatInfoDisplay);
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbTalk.Text))
            {
                return;
            }
            var sd = new say_data()
            {
                content = tbTalk.Text,
                likability = (int)m.Core.Save.Likability,
                mode = (int)m.Core.Save.Mode,
            };
            tbTalk.Text = "";
            Task.Run(() =>
            {
                var ret = ConnectAIOpen("VPet/Say", "POST", JsonConvert.SerializeObject(sd));
                switch (ret.Name)
                {
                    case "Success":
                        var showtxt = "当前Token使用".Translate() + ": " + ret.Info;
                        Dispatcher.Invoke(() =>
                        {
                            m.MsgBar.MessageBoxContent.Children.Add(new TextBlock() { Text = showtxt, FontSize = 20, ToolTip = showtxt, HorizontalAlignment = HorizontalAlignment.Right });
                            TalkChatInfoDisplay(ret);
                        });
                        m.SayRnd(ret.Text);
                        break;
                    default:
                    case "Error":
                        if (ret.Info == "Connect")
                            m.SayRnd(ret.Text);
                        else
                            m.SayRnd(ret.Info.Translate());
                        break;
                    case "Fail":
                        switch (ret.Info)
                        {
                            case "ServerFull":
                                m.SayRnd("ServerFull".Translate(ret[(gint)"serverused"], ret[(gint)"servertotal"]));
                                break;
                            case "NoUser":
                                Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Visible);
                                m.SayRnd("NoUser".Translate());
                                break;
                            case "NoTokenUser":
                            case "NoTokenVIP":
                                Dispatcher.Invoke(() => TalkChatInfoDisplay(ret));
                                m.SayRnd(ret.Info.Translate(ret[(gint)"TokenUsed"], ret[(gint)"TokenFreeLimit"], ret[(gdbe)"relshour"]));
                                break;
                            default:
                                m.SayRnd(ret.Info.Translate());
                                break;
                        }
                        break;
                }
            });

        }
        private class say_data
        {
            /// <summary>
            /// 说话内容
            /// </summary>
            public string content { get; set; } = "";
            /// <summary>
            /// 好感度
            /// </summary>
            public int likability { get; set; } = 0;
            /// <summary>
            /// 当前状态
            /// </summary>
            public int mode { get; set; } = 1;
            public string lang { get; set; } = LocalizeCore.CurrentCulture.ToLower();
        }
        /// <summary>
        /// 重置ChatGPT
        /// </summary>
        /// <returns></returns>
        public bool ChatGPT_Reset()
        {
            var rd = new rest_data()
            {
                petname = m.Core.Save.Name,
            };
            var ret = ConnectAIOpen("VPet/Rest", "POST", JsonConvert.SerializeObject(rd));
            switch (ret.Name)
            {
                case "Success":
                    Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Collapsed);
                    return true;
                default:
                case "Error":
                    if (ret.Info == "Connect")
                        m.SayRnd(ret.Text);
                    else
                        m.SayRnd(ret.Info.Translate());
                    return false;
                case "Fail":
                    m.SayRnd(ret.Info.Translate());
                    return false;
            }
        }
        /// <summary>
        /// 根据宠物剩余寿命显示相关UI
        /// </summary>
        public void TalkChatInfoDisplay()
        {
            //Dispatcher.Invoke(() => this.IsEnabled = false);
            var l = ConnectAIOpen("VPet/Data", "GET");
            if (l.Name == "Info")
            {
                Dispatcher.Invoke(() =>
                {
                    if (l[(gdbe)"PetLife"] == 0)
                    {
                        btn_startup.Visibility = Visibility.Visible;
                        return;
                    }
                    else
                    {
                        btn_startup.Visibility = Visibility.Collapsed;
                    }
                    PrograssUsed.Tag = "服务器消耗倍率".Translate() + $":\t{l[(gdbe)"servermuti"]:P0}";
                    TalkChatInfoDisplay(l);
                });
            }
            else
            {
                Dispatcher.Invoke(() => btn_startup.Visibility = Visibility.Collapsed);
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);
        }
        /// <summary>
        /// 根据宠物剩余寿命显示相关UI (读用户信息)
        /// </summary>
        /// <param name="steamid">steamid,用于记录历史</param>
        public void TalkChatInfoDisplay(Line Infos)
        {
            if (Infos.Find("TokenUsed") == null)
            {
                return;
            }
            PrograssUsed.Visibility = Visibility.Visible;
            double used = Math.Min(Infos[(gdbe)"TokenUsed"] / Infos[(gdbe)"TokenFreeLimit"], 1);
            PrograssUsed.Value = used;
            var txt = "当前使用字数".Translate() + $":\t{Infos[(gdbe)"TokenUsed"]}/{Infos[(gdbe)"TokenFreeLimit"]} ({used:P1})";
            txt += "\n" + "下次刷新时间".Translate() + $":\t{Infos[(gdat)"LastUsed"].AddDays(1)}";
            //未实装的付费功能
            if (Infos[(gint)"Vip"] == 0)
            {
                txt = "由桌宠开发者提供的免费API".Translate() + "\n" + txt;
                txt += "\n" + "生成最大字数".Translate() + $":\t{150 + Math.Min((int)Math.Sqrt(m.Core.Save.Likability) * 5, 450)}";
            }
            else
            {//未来如果整应用内购买再在这里实装
                txt = "由桌宠开发者提供的API".Translate() + "\n" + txt;
                txt += "\n" + "生成最大字数".Translate() + $":\t4000";
                txt += "\n" + "VIP用户".Translate() + $"({Infos[(gint)"Vip"] - 9})";
                txt += "\n" + "按量付费字数".Translate() + $":\t{Infos[(gint)"TokenPaid"]}";
            }
            PrograssUsed.ToolTip = txt + "\n" + PrograssUsed.Tag;
        }
        private class rest_data
        {
            public string lang { get; set; } = LocalizeCore.CurrentCulture.ToLower();
            public string petname { get; set; }
            public string diycreate { get; set; }
        }
        private void StartUP_Click(object sender, RoutedEventArgs e)
        {
            set["aiopen"][(gbol)"startup"] = true;
            mw.Save();
            btn_startup.Content = "初始化桌宠聊天程序中...".Translate();
            Task.Run(ChatGPT_Reset);
        }

        private void tbTalk_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SendMessage_Click(sender, e);
                e.Handled = true;
            }
        }
        /// <summary>
        /// 连接 由作者提供的聊天功能
        /// </summary>
        /// <returns></returns>
        public static Line ConnectAIOpen(string action, string Method = "POST", string content = null)
        {
            // const string url = "https://localhost:7166/";
            const string url = "https://aiopen.exlb.net:5810/";
            try
            {
                //请不要使用该API作为其他用途,如有其他需要请联系我(QQ群:430081239)
                //该API可能会因为其他原因更改
                string _url = url + action + "?steamid=" + SteamClient.SteamId.Value;
                //参数
                var request = (HttpWebRequest)WebRequest.Create(_url);
                request.Method = Method;
                request.ContentType = "application/json";//ContentType
                if (content != null)
                {
                    byte[] byteData = Encoding.UTF8.GetBytes(content);
                    int length = byteData.Length;
                    request.ContentLength = length;
                    using (Stream writer = request.GetRequestStream())
                    {
                        writer.Write(byteData, 0, length);
                        writer.Close();
                        writer.Dispose();
                    }
                }
                string responseString;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    responseString = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    response.Dispose();
                }
                return new Line(responseString);
            }
            catch (Exception ex)
            {
                return new Line("Error", "Connect", ex.ToString());
            }
        }
        private DateTime lastopeningtime;
        private void PrograssUsed_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if ((DateTime.Now - lastopeningtime).TotalMinutes < 5)
            {
                return;
            }
            lastopeningtime = DateTime.Now;
            Task.Run(TalkChatInfoDisplay);
        }
    }
}
