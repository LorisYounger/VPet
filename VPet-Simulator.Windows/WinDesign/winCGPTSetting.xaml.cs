using ChatGPT.API.Framework;
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
            mw.CGPTClient.Completions["vpet"].messages.AddRange(JsonConvert.DeserializeObject<List<ChatGPT.API.Framework.Message>>(tbHistory.Text));
            mw.Save();
            this.Close();
        }
    }
}
