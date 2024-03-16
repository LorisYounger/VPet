using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// 聊天消息 (string)
        /// </summary>
        Message,
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
    public MSGType Type;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content;
    /// <summary>
    /// 操作来自者 (也可能是自己)
    /// </summary>
    public ulong From;
    /// <summary>
    /// 被操作者 (显示动画用)
    /// </summary>
    public ulong To;

    public static string ConverTo(MPMessage data) => LPSConvert.SerializeObject(data).ToString();
    public static MPMessage ConverTo(string data) => LPSConvert.DeserializeObject<MPMessage>(new LPS(data));
}
