using Avalonia.Controls;
using Avalonia.Interactivity;
using LinePutScript.Localization;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// winReport.xaml 的交互逻辑
    /// </summary>
    /// 跨平台: 原文复制自 VPet-Simulator.Windows/WinDesign/winReport.xaml.cs; WindowX→VPetWindow, Visibility→IsVisible,
    /// Dispatcher→Dispatcher.UIThread, ComboBox.Text→选中项的 Content, 其余逐行相同
    public partial class winReport : VPetWindow
    {
        MainWindow mw;
        string save;
        public bool IsUniformSizeChanged => false;
        public bool StoreSize => false;
        public winReport(MainWindow mainw, string? errmsg = null)
        {
            mainw.Windows.Add(this);
            InitializeComponent();
            mw = mainw;
            Title = "反馈中心".Translate() + ' ' + mw.PrefixSave;
            save = mw!.Core?.Save?.ToLine().ToString() + mw!.Set?.ToString();
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

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void tUpload_Click(object? sender, RoutedEventArgs e)
        {//游戏设置比存档更重要,桌宠大部分内容存设置里了,所以一起上传
            if (tUpload.IsChecked == true)
                save = mw.Core.Save!.ToLine().ToString() + mw.Set.ToString();
            else
                save = "玩家取消上传存档".Translate();
        }

        private void btn_upload(object? sender, RoutedEventArgs e)
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
            MainGrid.IsEnabled = false;
            pgload.Value = 0;
            gridLoading.IsVisible = true;

#if DEBUG
            string _url = "http://localhost:5079/VPET/Report";
#else
                string _url = "https://report.exlb.net/VPET/Report";
#endif
            //参数
            StringBuilder sb = new StringBuilder();
            sb.Append("action=error");
            sb.Append("&type=" + HttpUtility.UrlEncode((tType.SelectedItem as ComboBoxItem)?.Content?.ToString()));
            sb.Append("&description=" + HttpUtility.UrlEncode(tDescription.Text));
            sb.Append("&content=" + HttpUtility.UrlEncode(tContent.Text));
            sb.Append("&contact=" + HttpUtility.UrlEncode(tContact.Text));
            sb.Append($"&steamid={Steamworks.SteamClient.SteamId.Value}");
            sb.Append($"&ver={mw.version}&repver=3&lang={LocalizeCore.CurrentCulture}");
            sb.Append("&save=");
            sb.Append(HttpUtility.UrlEncode(save));

            Task.Run(async () =>
            {
                try
                {
                    ShowTimeLoading();
                    using System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(120);
                    byte[] byteData = Encoding.UTF8.GetBytes(sb.ToString());
                    using System.Net.Http.ByteArrayContent content = new System.Net.Http.ByteArrayContent(byteData);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    System.Net.Http.HttpResponseMessage httpResponse = await client.PostAsync(_url, content);
                    string responseString = await httpResponse.Content.ReadAsStringAsync();

                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        gridLoading.IsVisible = false;
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
                            Close();
                        }
                        else
                        {
                            MessageBoxX.Show("反馈上传失败\n欢迎加入虚拟主播模拟器群430081239手动反馈问题\n服务器消息:".Translate() + responseString, "反馈提交失败,遇到错误".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error);
                        }
                        MainGrid.IsEnabled = true;
                    });
                }
                catch (Exception exp)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() => MessageBoxX.Show("反馈上传失败,可能是网络或其他问题导致无法上传\n欢迎加入虚拟主播模拟器群430081239手动反馈问题\n".Translate() + exp.ToString(), "反馈提交失败,遇到错误".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error));
                }
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() => MainGrid.IsEnabled = true);
            });

        }
        public void ShowTimeLoading()
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
             {
                 if (gridLoading.IsVisible)
                 {
                     if (pgload.Value >= 117)
                     {
                         pgload.Value = 120;
                     }
                     else
                     {
                         pgload.Value += Function.Rnd.Next(5, 15) * 0.1;
                         Task.Run(() =>
                         {
                             Thread.Sleep(Function.Rnd.Next(500, 1500));
                             ShowTimeLoading();
                         });
                     }
                 }
             });

        }
        private void MainGrid_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            Height = MainGrid.Bounds.Height + 50;
        }

        private void tType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            //跨平台: Avalonia 在 XAML 加载期间 (字段还没赋值) 就会触发一次 SelectionChanged
            if (tType == null)
                return;
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

        private void WindowX_Closed(object? sender, EventArgs e)
        {
            mw.Windows.Remove(this);
        }
    }
}
