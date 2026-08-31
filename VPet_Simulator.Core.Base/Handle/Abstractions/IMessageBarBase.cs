using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 平台无关的消息栏抽象
    /// </summary>
    public interface IMessageBarBase : IDisposable
    {
        /// <summary>
        /// 显示消息
        /// </summary>
        void Show(string name, string text, string? graphName = null, object? msgContent = null);

        /// <summary>
        /// 显示流式消息
        /// </summary>
        void Show(string name, SayInfoWithStreamBase sayInfoWithStream);

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
        /// 平台具体视图对象
        /// </summary>
        object View { get; }

        /// <summary>
        /// 被关闭时事件
        /// </summary>
        event Action EndAction;
    }
}
