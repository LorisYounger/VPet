using System;
using System.Diagnostics;
using System.IO.Packaging;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.Main;

namespace VPet_Simulator.Windows.Interface
{
    
    /// <summary>
    /// 聊天API接口/显示类
    /// </summary>
    public abstract partial class TalkBox : UserControl, ITalkAPI
    {
        /// <summary>
        /// 插件主体
        /// </summary>
        protected MainPlugin MainPlugin;
        public TalkBox(MainPlugin mainPlugin)
        {
            var baseUri = "/VPet-Simulator.Windows.Interface;component/talkbox.xaml";
            var resourceLocater = new Uri(baseUri, UriKind.Relative);
            var exprCa = (PackagePart)typeof(Application).GetMethod("GetResourceOrContentPart", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { resourceLocater });
            var stream = exprCa.GetStream();
            var uri = new Uri((Uri)typeof(BaseUriHelper).GetProperty("PackAppBaseUri", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), resourceLocater);
            var parserContext = new ParserContext
            {
                BaseUri = uri
            };
            typeof(XamlReader).GetMethod("LoadBaml", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { stream, parserContext, this, true });

            InitializeComponent();
            MainPlugin = mainPlugin;
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

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbTalk.Text))
            {
                return;
            }
            var cont = tbTalk.Text;
            tbTalk.Text = "";
            MainPlugin.MW.Main.ToolBar.Visibility = Visibility.Collapsed;

            Task.Run(() => Responded(cont));
        }
        /// <summary>
        /// 显示思考动画
        /// </summary>
        public void DisplayThink()
        {
            if (MainPlugin.MW.Main.DisplayType.Name == "think")
                return;

            var think = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.B_Loop, MainPlugin.MW.Core.Save.Mode);
            var think2 = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.A_Start, MainPlugin.MW.Core.Save.Mode);
            if (think.Count > 0 && think2.Count > 0)
            {
                MainPlugin.MW.Main.Display("think", AnimatType.A_Start, MainPlugin.MW.Main.DisplayBLoopingForce);
            }
        }
        /// <summary>
        /// 显示思考结束并说话
        /// </summary>
        public void DisplayThinkToSayRnd(string text, string desc = null)
        {
            var think = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.C_End, MainPlugin.MW.Core.Save.Mode);
            Action Next = () => { MainPlugin.MW.Main.SayRnd(text, true, desc); };
            if (think.Count > 0)
            {
                MainPlugin.MW.Main.Display(think[Function.Rnd.Next(think.Count)], Next);
            }
            else
            {
                Next();
            }
        }
        
        /// <summary>
        /// 带有流式传输的对话
        /// </summary>
        /// <param name="sayInfoWithStream"></param>
        /// <param name="desc"></param>
        public void DisplayThinkToSayRnd(SayInfoWithStream sayInfoWithStream,string desc = null)
        {
            
            var think = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.C_End, MainPlugin.MW.Core.Save.Mode);
            Action Next = () => {MainPlugin.MW.Main.SayRnd(sayInfoWithStream,true, desc); };
            if (think.Count > 0)
            {
                MainPlugin.MW.Main.Display(think[Function.Rnd.Next(think.Count)], Next);
            }
            else
            {
                Next();
            }
        }
        
        /// <summary>
        /// 聊天设置
        /// </summary>
        public abstract void Setting();

        private void tbTalk_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Send_Click(sender, e);
                e.Handled = true;
                MainPlugin.MW.Main.ToolBar.Visibility = Visibility.Collapsed;
                return;
            }
            if (tbTalk.Text.Length > 0)
            {
                MainPlugin.MW.Main.ToolBar.CloseTimer.Stop();
            }
            else
            {
                MainPlugin.MW.Main.ToolBar.CloseTimer.Start();
            }
        }
        public UIElement This => this;
    }
    public interface ITalkAPI
    {
        /// <summary>
        /// 显示的窗口
        /// </summary>
        UIElement This { get; }

        /// <summary>
        /// 该聊天接口名字
        /// </summary>
        string APIName { get; }
        /// <summary>
        /// 聊天设置
        /// </summary>
        void Setting();
    }
}
