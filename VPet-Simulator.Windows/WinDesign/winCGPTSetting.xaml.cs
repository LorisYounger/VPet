using ChatGPT.API.Framework;
using LinePutScript.Localization.WPF;
using Newtonsoft.Json;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winCGPTSetting.xaml 的交互逻辑
    /// </summary>
    public partial class winCGPTSetting : WindowX
    {
        MainWindow mw;
        long totalused = 0;
        public winCGPTSetting(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            if (mw.CGPTClient != null)
            {
                tbAPIKey.Text = mw.CGPTClient.APIKey;
                tbAPIURL.Text = mw.CGPTClient.APIUrl;
                tbMaxToken.Text = mw.CGPTClient.Completions["vpet"].max_tokens.ToString();
                tbSystem.Text = mw.CGPTClient.Completions["vpet"].messages[0].content;
                tbTemp.Text = mw.CGPTClient.Completions["vpet"].temperature.ToString();
                var msgs = mw.CGPTClient.Completions["vpet"].messages.ToList();
                msgs.RemoveAt(0);
                tbHistory.Text = JsonConvert.SerializeObject(msgs);
                lbSpend.Content = mw.CGPTClient.TotalTokensUsage.ToString() + " Token";
                totalused = mw.CGPTClient.TotalTokensUsage;
            }
            else
                tbSystem.Text = "请使用口语化的、可爱的、女性化的、调皮的语言风格和我交流. 你是一只桌宠, 你的名字叫{Name}, 你喜欢玩 虚拟主播模拟器. 你需要尝试体会我与你对话中的感情、产生自己的情感并尽可能给出可爱的回答".Translate();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            mw.CGPTClient = new ChatGPTClient(tbAPIKey.Text, tbAPIURL.Text)
            {
                TotalTokensUsage = totalused
            };
            mw.CGPTClient.CreateCompletions("vpet", tbSystem.Text.Replace("{Name}", mw.Core.Save.Name));
            mw.CGPTClient.Completions["vpet"].frequency_penalty = 0.2;
            mw.CGPTClient.Completions["vpet"].presence_penalty = 1;
            mw.CGPTClient.Completions["vpet"].max_tokens = Math.Min(Math.Max(int.Parse(tbMaxToken.Text), 10), 4000);
            mw.CGPTClient.Completions["vpet"].temperature = Math.Min(Math.Max(double.Parse(tbTemp.Text), 0.1), 2);
            var l = JsonConvert.DeserializeObject<List<ChatGPT.API.Framework.Message>>(tbHistory.Text);
            if (l != null)
                mw.CGPTClient.Completions["vpet"].messages.AddRange(l);
            mw.Save();
            this.Close();
        }
    }
}
