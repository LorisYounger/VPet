using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 聊天API接口/显示类
    /// </summary>
    public abstract partial class TalkBox : UserControl
    {
        MainPlugin MainPlugin;
        public TalkBox(MainPlugin mainPlugin)
        {
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
            Task.Run(() => Responded(cont));
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
    }
}
