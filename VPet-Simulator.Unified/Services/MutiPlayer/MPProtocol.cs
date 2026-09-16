using LinePutScript;
using System.Text;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 多人联机的线上格式
/// </summary>
/// 两端要能互相联机, 发出去的字节必须一模一样. 消息类型的数值、字段名、封装方式
/// 这几样只要有一处对不上, 表现就是"能连上但对方看不到动作", 极难查 —— 所以全部
/// 集中在这里, 由门禁盯着。
///
/// 这里只管线上格式, 不管 Steam 怎么收发: 那部分是宿主自己的事。
public static class MPProtocol
{
    // ---- 字段名 ----

    /// <summary>消息类型的行名</summary>
    public const string TypeLineName = "Type";

    /// <summary>消息内容的行名</summary>
    public const string ContentLineName = "Content";

    /// <summary>被操作者的行名</summary>
    public const string ToLineName = "To";

    // ---- 大厅数据的键 ----

    /// <summary>标记这是个桌宠联机大厅</summary>
    public const string LobbyMarkKey = "isvpets";

    /// <summary>大厅标记的值</summary>
    public const string LobbyMarkValue = "true";

    /// <summary>被踢出者的 SteamId</summary>
    public const string LobbyKickKey = "kick";

    /// <summary>是否不再接受新人</summary>
    public const string LobbyNoJoinKey = "nojoin";

    // ---- 消息类型 ----

    /// <summary>
    /// 消息类型
    /// </summary>
    /// 数值与 Windows 版 MPMessage.MSGType 逐项一致。
    /// MOD 可以随便抽一个不在这里的数当自己的类型, 负数也行。
    public enum MessageKind
    {
        /// <summary>一般是出错或者空消息</summary>
        Empty,
        /// <summary>聊天消息</summary>
        Chat,
        /// <summary>显示动画</summary>
        DispayGraph,
        /// <summary>交互</summary>
        Interact,
        /// <summary>喂食</summary>
        Feed,
    }

    /// <summary>
    /// 交互类型
    /// </summary>
    /// 数值与 Windows 版 MPMessage.Interact 逐项一致
    public enum InteractKind
    {
        /// <summary>摸身体</summary>
        TouchHead,
        /// <summary>摸头</summary>
        TouchBody,
        /// <summary>捏脸</summary>
        TouchPinch,
    }

    /// <summary>
    /// 聊天可见范围
    /// </summary>
    /// 数值与 Windows 版 MPMessage.Chat.Type 逐项一致
    public enum ChatKind
    {
        /// <summary>私有</summary>
        Private,
        /// <summary>半公开</summary>
        Internal,
        /// <summary>公开</summary>
        Public,
    }

    // ---- 消息内容 ----

    /// <summary>
    /// 聊天消息的内容
    /// </summary>
    /// 字段名和顺序必须与 Windows 版 MPMessage.Chat 逐字一致: 内容是用 LPSConvert
    /// 序列化成字符串塞进 Content 子项的, 字段名对不上对面就读不出来。
    public struct Chat
    {
        /// <summary>聊天内容</summary>
        public string Content { get; set; }
        /// <summary>可见范围</summary>
        public ChatKind ChatType { get; set; }
        /// <summary>发送者名字</summary>
        public string SendName { get; set; }
        /// <summary>接受者名字</summary>
        public string ToName { get; set; }
    }

    /// <summary>
    /// 把消息内容序列化成字符串
    /// </summary>
    /// 与 Windows 版 MPMessage.SetContent 调的是同一个方法和同一组参数
    public static string EncodeContent(object content)
        => LinePutScript.Converter.LPSConvert.GetObjectString(content, convertNoneLineAttribute: true);

    /// <summary>
    /// 把消息内容反序列化回来
    /// </summary>
    public static T? DecodeContent<T>(string content)
        => (T?)LinePutScript.Converter.LPSConvert.GetStringObject(content, typeof(T), convertNoneLineAttribute: true);

    // ---- 封装 ----

    /// <summary>
    /// 造一条消息
    /// </summary>
    /// <param name="kind">消息类型</param>
    /// <param name="to">被操作者, 广播传 0</param>
    /// <param name="content">已经序列化好的内容 (见 EncodeContent)</param>
    /// **三项是三个顶层的行, 不是一行上的三个子项**. Windows 版的 MPMessage 是靠
    /// LPSConvert.SerializeObject 序列化的, 它把每个 [Line] 属性摊成文档里的一行;
    /// 包成一行三子项的话对面读出来全是默认值, 而且不报错 —— 表现是"能连上但
    /// 什么都收不到"。
    public static ILPS BuildMessage(MessageKind kind, ulong to, string content)
    {
        var document = new LPS();
        document.Add(new Line(TypeLineName, ((int)kind).ToString()));
        document.Add(new Line(ContentLineName, content));
        document.Add(new Line(ToLineName, to.ToString()));
        return document;
    }

    /// <summary>
    /// 读一条消息的类型
    /// </summary>
    public static int ReadKind(ILPS message) => message.FindLine(TypeLineName)?.InfoToInt ?? 0;

    /// <summary>
    /// 读一条消息的被操作者
    /// </summary>
    public static ulong ReadTo(ILPS message)
        => ulong.TryParse(message.FindLine(ToLineName)?.Info, out var value) ? value : 0;

    /// <summary>
    /// 读一条消息的内容
    /// </summary>
    public static string ReadContent(ILPS message) => message.FindLine(ContentLineName)?.Info ?? string.Empty;

    /// <summary>
    /// 把一条消息封成要发出去的字节
    /// </summary>
    public static byte[] EncodeMessage(ILPS message) => Encode(message);

    /// <summary>
    /// 把一条消息封成要发出去的字节
    /// </summary>
    /// 就是 LPS 文本的 UTF-8 编码, 没有长度前缀也没有校验位 —— Steam 的 P2P 包
    /// 自己带边界。
    public static byte[] Encode(ILPS message)
        //LPS 文档理论上可以给出 null 文本, 那时发一个空包比抛异常好 ——
        //联机里一条消息发不出去不该把整条连接带崩
        => Encoding.UTF8.GetBytes(message.ToString() ?? string.Empty);

    /// <summary>
    /// 把收到的字节拆回一条消息
    /// </summary>
    public static LPS Decode(byte[] data) => new LPS(Encoding.UTF8.GetString(data));
}
