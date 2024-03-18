using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 多人模式传输的消息
/// </summary>
public struct MPMessage
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MSGType
    {
        /// <summary>
        /// 一般是出错或者空消息
        /// </summary>
        Empty,
        /// <summary>
        /// 聊天消息 (chat)
        /// </summary>
        Chat,
        /// <summary>
        /// 显示动画 (graphinfo)
        /// </summary>
        DispayGraph,
        /// <summary>
        /// 摸身体 
        /// </summary>
        TouchHead,
        /// <summary>
        /// 摸头
        /// </summary>
        TouchBody,
        /// <summary>
        /// 喂食
        /// </summary>
        Feed,
    }
    /// <summary>
    /// 消息类型
    /// </summary>
    [Line] public MSGType Type { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [Line] private string Content { get; set; }
    /// <summary>
    /// 被操作者 (显示动画用)
    /// </summary>
    [Line] public ulong To { get; set; }

    public static byte[] ConverTo(MPMessage data) => Encoding.UTF8.GetBytes(LPSConvert.SerializeObject(data).ToString());
    public static MPMessage ConverTo(byte[] data) => LPSConvert.DeserializeObject<MPMessage>(new LPS(Encoding.UTF8.GetString(data)));

    public void SetContent(object content)
    {
        Content = LPSConvert.GetObjectString(content, convertNoneLineAttribute: true);
    }
    public T GetContent<T>()
    {
        return (T)LPSConvert.GetStringObject(Content, typeof(T), convertNoneLineAttribute: true);
    }
    /// <summary>
    /// 聊天结构
    /// </summary>
    public struct Chat
    {
        /// <summary>
        /// 聊天内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 消息类型
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// 私有
            /// </summary>
            Private,
            /// <summary>
            /// 半公开
            /// </summary>
            Internal,
            /// <summary>
            /// 公开
            /// </summary>
            Public
        }
        /// <summary>
        /// 聊天类型
        /// </summary>
        public Type ChatType { get; set; }
        /// <summary>
        /// 发送者名字
        /// </summary>
        public string SendName { get; set; }
    }
}
