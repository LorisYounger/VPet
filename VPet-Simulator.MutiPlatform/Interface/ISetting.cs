//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/ISetting.cs; System.Windows.Point 换成 Avalonia.Point
using Avalonia;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 设置方法接口的 Windows 半
    /// </summary>
    /// 只剩这两项: System.Windows.Point 是 WPF 的类型, 没法共享.
    /// 跨平台侧的设置实现不需要它们 —— 那边的窗口位置是 Avalonia 的 PixelPoint.
    public partial interface ISetting
    {

        /// <summary>
        /// 获取上次退出位置
        /// </summary>
        Point StartRecordLastPoint { get; }

        /// <summary>
        /// 获取或设置桌宠启动的位置
        /// </summary>
        Point StartRecordPoint { get; set; }
    }
}
