using LinePutScript;
using LinePutScript.Converter;
using System.Text;

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
        /// 交互 (Interact)
        /// </summary>
        Interact,
        /// <summary>
        /// 喂食 (Feed)
        /// </summary>
        Feed,
    }
    /// <summary>
    /// 消息类型 MOD作者可以随便抽个不是MSGTYPE的数避免冲突,支持负数
    /// </summary>
    [Line] public int Type { get; set; }

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
    /// <summary>
    /// 设置消息内容(类)
    /// </summary>
    public void SetContent(object content)
    {
        Content = LPSConvert.GetObjectString(content, convertNoneLineAttribute: true);
    }
    /// <summary>
    /// 获取消息内容(类)
    /// </summary>
    /// <typeparam name="T">类类型</typeparam>
    public T GetContent<T>()
    {
        return (T)LPSConvert.GetStringObject(Content, typeof(T), convertNoneLineAttribute: true);
    }
    /// <summary>
    /// 设置消息内容(字符串)
    /// </summary>
    public void SetContent(string content)
    {
        Content = content;
    }
    /// <summary>
    /// 获取消息内容(字符串)
    /// </summary>
    public string GetContent()
    {
        return Content;
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
    /// <summary>
    /// 交互结构
    /// </summary>
    public struct Feed
    {
        /// <summary>
        /// 对方是否启用了数据计算 (并且未丢失小标)
        /// </summary>
        public bool EnableFunction { get; set; }
        /// <summary>
        /// 食物/物品
        /// </summary>
        [Line()]
        public Food Item { get; set; }
    }
    /// <summary>
    /// 交互类型
    /// </summary>
    public enum Interact
    {
        /// <summary>
        /// 摸身体 
        /// </summary>
        TouchHead,
        /// <summary>
        /// 摸头
        /// </summary>
        TouchBody,
        /// <summary>
        /// 捏脸
        /// </summary>
        TouchPinch,
    }
}
