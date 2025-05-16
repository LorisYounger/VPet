using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winReport.xaml 的交互逻辑
    /// </summary>
    public partial class winReport : WindowX
    {
        MainWindow mw;
        string save;
        public bool IsUniformSizeChanged => false;
        public bool StoreSize => false;
        public winReport(MainWindow mainw, string errmsg = null)
        {
            mainw.Windows.Add(this);
            InitializeComponent();
            mw = mainw;
            Title = "反馈中心".Translate() + ' ' + mw.PrefixSave;
            save = mw?.Core?.Save?.ToLine().ToString() + mw.Set?.ToString();
            if (errmsg != null)
            {
                tType.SelectedIndex = 0;
                tContent.Text = errmsg;
                tContent.IsReadOnly = true;
            }

            if (!mw.IsSteamUser)
            {
                MessageBoxX.Show("您不是Steam用户，无法使用反馈中心\n欢迎加入虚拟主播模拟器群430081239反馈问题".Translate(),
                    "非Steam用户无法使用反馈中心".Translate(), MessageBoxButton.OK, MessageBoxIcon.Info);
                btn_Report.IsEnabled = false;
            }
        }

        private void tUpload_Click(object sender, RoutedEventArgs e)
        {//游戏设置比存档更重要,桌宠大部分内容存设置里了,所以一起上传
            if (tUpload.IsChecked == true)
                save = mw.Core.Save.ToLine().ToString() + mw.Set.ToString();
            else
                save = "玩家取消上传存档".Translate();
        }

        private void btn_upload(object sender, RoutedEventArgs e)
        {
            if (tDescription.Text == "" && tType.SelectedIndex != 0)
            {
                MessageBoxX.Show("问题详细描述是反馈具体问题\n例如如何触发这个报错,游戏有什么地方不合理等".Translate(), "请填写问题描述".Translate());
                return;
            }
            if (!mw.IsSteamUser)
            {
                MessageBoxX.Show("您不是Steam用户，无法使用反馈中心\n欢迎加入虚拟主播模拟器群430081239反馈问题".Translate(), "非Steam用户无法使用反馈中心".Translate(), MessageBoxButton.OK, MessageBoxIcon.Info);
                return;//不遥测非Steam用户
            }
            try
            {
                string _url = "https://report.exlb.net/VPET/Report";
                //参数
                StringBuilder sb = new StringBuilder();
                sb.Append("action=error");
                sb.Append("&type=" + HttpUtility.UrlEncode(tType.Text));
                sb.Append("&description=" + HttpUtility.UrlEncode(tDescription.Text));
                sb.Append("&content=" + HttpUtility.UrlEncode(tContent.Text));
                sb.Append("&contact=" + HttpUtility.UrlEncode(tContact.Text));
                sb.Append($"&steamid={Steamworks.SteamClient.SteamId.Value}");
                sb.Append($"&ver={mw.version}&repver=2&lang={LocalizeCore.CurrentCulture}");
                sb.Append("&save=");
                sb.AppendLine(HttpUtility.UrlEncode(save));
#pragma warning disable SYSLIB0014 // 类型或成员已过时
                var request = (HttpWebRequest)WebRequest.Create(_url);
#pragma warning restore SYSLIB0014 // 类型或成员已过时
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
                if (responseString == "Report Error Success")
                {
                    MessageBoxX.Show("您的反馈已提交成功,感谢您的反馈与提交\nVOS将会尽快处理您的反馈并做的更好".Translate(), "感谢您的反馈和提交".Translate());
                    Close();
                }
                else if (responseString == "IP times Max")
                {
                    mw.Set.DiagnosisDayEnable = false;
                    MessageBoxX.Show("您今天的反馈次数已达上限,请明天再来反馈.\n或欢迎加入虚拟主播模拟器群430081239反馈问题".Translate(), "您今天的反馈次数已达上限".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error);
                }
                else if (responseString.StartsWith("ReportMessage:"))
                {
                    MessageBoxX.Show(responseString.Substring(14), "感谢您的反馈和提交".Translate());
                }
                else
                {
                    MessageBoxX.Show("反馈上传失败\n欢迎加入虚拟主播模拟器群430081239手动反馈问题\n服务器消息:".Translate() + responseString, "反馈提交失败,遇到错误".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception exp)
            {
                MessageBoxX.Show("反馈上传失败,可能是网络或其他问题导致无法上传\n欢迎加入虚拟主播模拟器群430081239手动反馈问题\n".Translate() + exp.ToString(), "反馈提交失败,遇到错误".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error);
            }

        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Height = MainGrid.ActualHeight + 50;
        }

        private void tType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tType.SelectedIndex == 5)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var v in LocalizeCore.StoreTranslationList)
                    {
                        sb.AppendLine(v.Replace("\n", @"\n").Replace("\r", @"\r"));
                    }
                    tContent.Text = sb.ToString();
                    if (string.IsNullOrEmpty(tContent.Text))
                    {
                        tContent.Text = "没有需要提交的翻译的内容".Translate();
                    }
                    tUpload.IsChecked = false;
                }
                catch
                {

                }
            }
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            mw.Windows.Remove(this);
        }
    }
}
