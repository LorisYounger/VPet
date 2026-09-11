using Avalonia.Controls;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 消息栏
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Core/Display/MessageBar.xaml.cs 里的同名接口.
/// 存在的理由是"消息栏可以被换掉": 多人联机时访客桌宠要挂一个不一样的消息栏
/// (Windows 版 MPFriends 就是直接给 Main.MsgBar 赋一个新实例).
///
/// 与 Windows 版的唯一差异是 Show 的最后一个参数: 那边传的是现成的 UIElement,
/// 这边传字符串 desc, 由消息栏自己在 UI 线程上把控件 new 出来. 原因写在
/// ABI-Compatibility.md 的"跨平台侧的线程约定"那一节 —— Avalonia 的控件在构造时
/// 就绑定了创建它的 Dispatcher, 在后台线程 new 好再传进来会在下一次排版时炸.
public interface IMassageBar : IDisposable
{
    /// <summary>
    /// 显示消息
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="text">内容</param>
    /// <param name="graphName">图像名</param>
    /// <param name="msgContent">消息框内容</param>
    /// <param name="desc">描述, 没有 msgContent 时用它生成一段右对齐的小字</param>
    void Show(string name, string text, string? graphName = null, Control? msgContent = null, string? desc = null);

    /// <summary>
    /// 显示流式消息
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="sayInfoWithStream">内容</param>
    void Show(string name, SayInfoWithStream sayInfoWithStream);

    /// <summary>
    /// 强制关闭
    /// </summary>
    void ForceClose();

    /// <summary>
    /// 设置位置在桌宠内
    /// </summary>
    void SetPlaceIN();

    /// <summary>
    /// 设置位置在桌宠外
    /// </summary>
    void SetPlaceOUT();

    /// <summary>
    /// 显示状态
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// 该消息框的Control
    /// </summary>
    Control This { get; }

    /// <summary>
    /// 被关闭时事件
    /// </summary>
    event Action? EndAction;
}
