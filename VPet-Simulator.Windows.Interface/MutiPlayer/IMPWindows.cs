using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 多人联机窗口接口 (访客表)
/// </summary>
public interface IMPWindows
{
    /// <summary>
    /// 访客表id
    /// </summary>
    ulong LobbyID { get; }
    /// <summary>
    /// 所有好友(不包括自己)
    /// </summary>
    IEnumerable<IMPFriend> Friends { get; }
    /// <summary>
    /// 主持人SteamID
    /// </summary>
    ulong OwnerID { get; }

    /// <summary>
    /// 事件:成员退出
    /// </summary>
    event Action<ulong> OnMemberLeave;
    /// <summary>
    /// 事件:成员加入
    /// </summary>
    event Action<ulong> OnMemberJoined;
    /// <summary>
    /// 给指定好友发送消息(数据包)
    /// </summary>
    /// <param name="friendid">好友id</param>
    /// <param name="msg">消息内容(数据包)</param>
    void SendMessage(ulong friendid, MPMessage msg);

    /// <summary>
    /// 给所有人发送消息
    /// </summary>
    void SendMessageALL(MPMessage msg);

    /// <summary>
    /// 发送日志消息
    /// </summary>
    /// <param name="message">日志</param>
    void Log(string message);

    /// <summary>
    /// 收到消息日志 发送人id, 消息内容
    /// </summary>
    event Action<ulong, MPMessage> ReceivedMessage;
    /// <summary>
    /// 事件: 结束访客表, 窗口关闭
    /// </summary>
    event Action ClosingMutiPlayer;
}
