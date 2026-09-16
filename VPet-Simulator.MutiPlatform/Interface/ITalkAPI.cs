//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/TalkBox.xaml.cs 里的 ITalkAPI; UIElement→Control
using Avalonia.Controls;

namespace VPet_Simulator.Windows.Interface
{
    public interface ITalkAPI
    {
        /// <summary>
        /// 显示的窗口
        /// </summary>
        Control This { get; }

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
