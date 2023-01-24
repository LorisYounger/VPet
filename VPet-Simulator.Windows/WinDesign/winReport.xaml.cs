using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using VPet_Simulator.Core;
using System.Web;

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
            InitializeComponent();
            mw = mainw;
            save = mw.Core.Save.ToLine().ToString();
            if (errmsg != null)
            {
                tType.SelectedIndex = 0;
                tContent.Text = errmsg;
                tContent.IsEnabled = false;
                tContent.Height = 44;
            }

            if (!mw.IsSteamUser)
            {
                IsEnabled = false;
                MessageBoxX.Show("您不是Steam用户，无法使用反馈中心\n欢迎加入虚拟主播模拟器群430081239反馈问题", "非Steam用户无法使用反馈中心", MessageBoxButton.OK, MessageBoxIcon.Info);
            }
        }
        
        private void tUpload_Click(object sender, RoutedEventArgs e)
        {
            if (tUpload.IsChecked == true)
                save = mw.Core.Save.ToLine().ToString();
            else
                save = "玩家取消上传存档";
        }

        private void btn_upload(object sender, RoutedEventArgs e)
        {
            if (tDescription.Text == "" && tType.SelectedIndex != 0)
            {
                 MessageBoxX.Show("请填写问题描述", "问题详细描述是反馈具体问题\n例如如何触发这个报错,游戏有什么地方不合理等");
                return;
            }
            if (!mw.IsSteamUser)
                return;//不遥测非Steam用户

            try
            {
                string _url = "http://cn.exlb.org:5810/VPET/Report";
                //参数
                StringBuilder sb = new StringBuilder();
                sb.Append("action=error");
                sb.Append("&type=" + HttpUtility.UrlEncode(tType.Text));
                sb.Append("&description=" + HttpUtility.UrlEncode(tDescription.Text));
                sb.Append("&content=" + HttpUtility.UrlEncode(tContent.Text));
                sb.Append("&contact=" + HttpUtility.UrlEncode(tContact.Text));
                sb.Append($"&steamid={Steamworks.SteamClient.SteamId.Value}");
                sb.Append($"&ver={mw.verison}");
                sb.Append("&save=");
                sb.AppendLine(HttpUtility.UrlEncode(save));
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
                if (responseString == "Report Error Success")
                {
                     MessageBoxX.Show("您的反馈已提交成功,感谢您的反馈与提交\nVOS将会尽快处理您的反馈并做的更好", "感谢您的反馈和提交");
                    Close();
                }
                else if (responseString == "IP times Max")
                {
                    mw.Set.DiagnosisDayEnable = false;
                     MessageBoxX.Show( "您今天的反馈次数已达上限,请明天再来反馈.\n或欢迎加入虚拟主播模拟器群430081239反馈问题", "您今天的反馈次数已达上限", MessageBoxButton.OK, MessageBoxIcon.Error);
                }
                else
                {
                     MessageBoxX.Show( "反馈上传失败\n欢迎加入虚拟主播模拟器群430081239手动反馈问题\n服务器消息:" + responseString, "反馈提交失败,遇到错误", MessageBoxButton.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception exp)
            {
                 MessageBoxX.Show( "反馈上传失败,可能是网络或其他问题导致无法上传\n欢迎加入虚拟主播模拟器群430081239手动反馈问题\n" + exp.ToString(), "反馈提交失败,遇到错误", MessageBoxButton.OK, MessageBoxIcon.Error);
            }

        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Height = MainGrid.ActualHeight + 50;
        }
    }
}
