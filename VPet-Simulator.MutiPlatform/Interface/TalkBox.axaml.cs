//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/TalkBox.xaml.cs. 那边靠 MainPlugin 拿主窗口, 这边没有老式插件,
//构造函数直接收 IMainWindow (统一契约的宿主把它交给插件); 反射加载 BAML 那段是 WPF 独有的, Avalonia 的 InitializeComponent 自己会读 AXAML
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows.Interface
{

    /// <summary>
    /// 聊天API接口/显示类
    /// </summary>
    public abstract partial class TalkBox : UserControl, ITalkAPI
    {
        /// <summary>
        /// 主窗口
        /// </summary>
        protected IMainWindow MW;
        public TalkBox(IMainWindow mw)
        {
            InitializeComponent();
            //跨平台: Avalonia 没有 PreviewKeyDown, 用隧道路由挂
            tbTalk.AddHandler(KeyDownEvent, tbTalk_KeyDown, RoutingStrategies.Tunnel);
            MW = mw;
        }
        /// <summary>
        /// 根据内容进行回应 (异步)
        /// </summary>
        /// <param name="text">内容</param>
        public abstract void Responded(string text);
        /// <summary>
        /// 该聊天接口名字
        /// </summary>
        public abstract string APIName { get; }

        private void Send_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbTalk.Text))
            {
                return;
            }
            var cont = tbTalk.Text;
            tbTalk.Text = "";
            if (MW.Main.ToolBar != null)
                MW.Main.ToolBar!.Hide();

            Task.Run(() => Responded(cont));
        }
        /// <summary>
        /// 显示思考动画
        /// </summary>
        public void DisplayThink()
        {
            if (MW.Main.DisplayType.Name == "think")
                return;

            var think = MW.Core.Graph!.FindGraphs("think", AnimatType.B_Loop, MW.Core.Save!.Mode);
            var think2 = MW.Core.Graph!.FindGraphs("think", AnimatType.A_Start, MW.Core.Save!.Mode);
            if (think?.Count > 0 && think2?.Count > 0)
            {
                MW.Main.Display("think", AnimatType.A_Start, MW.Main.DisplayBLoopingForce);
            }
        }
        /// <summary>
        /// 显示思考结束并说话
        /// </summary>
        public void DisplayThinkToSayRnd(string text, string? desc = null)
        {
            var think = MW.Core.Graph!.FindGraphs("think", AnimatType.C_End, MW.Core.Save!.Mode) ?? [];
            Action Next = () => { MW.Main.SayRnd(text, true, desc); };
            if (think.Count > 0)
            {
                MW.Main.Display(think[Function.Rnd.Next(think.Count)], Next);
            }
            else
            {
                Next();
            }
        }

        /// <summary>
        /// 显示思考结束并说话 流式说话版本
        /// </summary>
        /// <param name="sayInfostream">说话信息</param>
        public void DisplayThinkToSayRnd(SayInfoWithStream sayInfostream)
        {
            var think = MW.Core.Graph!.FindGraphs("think", AnimatType.C_End, MW.Core.Save!.Mode);
            sayInfostream.Force = true;
            if (think?.Count > 0)
            {
                Task.Run(() =>
                {
                    while (!sayInfostream.IsFinishGen && Function.ComCheck(sayInfostream.CurrentText.ToString()) < 4 && sayInfostream.CurrentText.Length < 80)
                    {
                        Thread.Sleep(50);
                    }
                    int a = Function.ComCheck(sayInfostream.CurrentText.ToString());
                    int b = sayInfostream.CurrentText.Length;
                    MW.Main.Display(think[Function.Rnd.Next(think.Count)], () => MW.Main.SayRnd(sayInfostream));
                });
            }
            else
            {
                MW.Main.SayRnd(sayInfostream);
            }
        }

        /// <summary>
        /// 聊天设置
        /// </summary>
        public abstract void Setting();

        private void tbTalk_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                Send_Click(sender, e);
                e.Handled = true;
                MW.Main.ToolBar!.Hide();
                return;
            }
            if ((tbTalk.Text?.Length ?? 0) > 0)
            {
                MW.Main.ToolBar!.CloseTimer.Stop();
            }
            else
            {
                MW.Main.ToolBar!.CloseTimer.Start();
            }
        }
        public Control This => this;
    }
}
