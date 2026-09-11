using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 一次联机
/// </summary>
/// 把"消息怎么编解码、收到之后分给谁"这一层从传输里分出来: 传输(Steam 的大厅和
/// P2P)在两个平台上是同一份托管 API, 但要不要引那个包是宿主的事; 而这一层不碰
/// Steam, 拿一个 ISteamMultiplayer 就能跑, 也就能拿假的传输验。
///
/// 线上格式走共享后端的 MPProtocol, 与 Windows 版逐字节相同 —— 两个平台的玩家
/// 能连到同一个房间里。
public class MultiplayerSession
{
    private readonly ISteamMultiplayer transport;

    public MultiplayerSession(ISteamMultiplayer transport)
    {
        this.transport = transport;
        transport.MemberJoined += x => MemberJoined?.Invoke(x);
        transport.MemberLeft += x => MemberLeft?.Invoke(x);
    }

    /// <summary>联机能用吗</summary>
    public bool IsAvailable => transport.IsAvailable;

    /// <summary>不可用的原因</summary>
    public string? UnavailableReason => transport.UnavailableReason;

    /// <summary>现在在哪个房间</summary>
    public ISteamLobby? Lobby => transport.CurrentLobby;

    /// <summary>房间里现在有谁</summary>
    public IReadOnlyList<LobbyMember> Members => Lobby?.Members ?? Array.Empty<LobbyMember>();

    /// <summary>有人进来</summary>
    public event Action<LobbyMember>? MemberJoined;
    /// <summary>有人离开</summary>
    public event Action<LobbyMember>? MemberLeft;

    /// <summary>收到一条聊天</summary>
    public event Action<ulong, string>? ChatReceived;
    /// <summary>收到一次互动</summary>
    public event Action<ulong, MPProtocol.InteractKind>? InteractReceived;
    /// <summary>收到一条别的消息 (MOD 自己定义的类型)</summary>
    public event Action<ulong, int, ILPS>? OtherReceived;

    /// <summary>
    /// 开一个房间
    /// </summary>
    public async System.Threading.Tasks.Task<bool> HostAsync(int maxMembers = 8)
    {
        var lobby = await transport.CreateLobbyAsync(maxMembers);
        if (lobby == null)
            return false;
        //打上标记, 别的桌宠才认得出这是个桌宠房间
        lobby.SetData(MPProtocol.LobbyMarkKey, MPProtocol.LobbyMarkValue);
        lobby.SetJoinable(true);
        transport.StartNetworking();
        return true;
    }

    /// <summary>
    /// 进一个房间
    /// </summary>
    public async System.Threading.Tasks.Task<bool> JoinAsync(ulong lobbyId)
    {
        var lobby = await transport.JoinLobbyAsync(lobbyId);
        if (lobby == null)
            return false;
        transport.StartNetworking();
        return true;
    }

    /// <summary>离开房间</summary>
    public void Leave() => Lobby?.Leave();

    /// <summary>还收不收新人</summary>
    public void SetJoinable(bool value)
    {
        Lobby?.SetData(MPProtocol.LobbyNoJoinKey, value ? "false" : "true");
        Lobby?.SetJoinable(value);
    }

    /// <summary>房主把某人请出去</summary>
    /// 与 Windows 版一样是"软踢": 房主写一个键, 被点名的那位自己退出。
    /// Steam 没有真正的踢人接口。
    public void Kick(ulong memberId)
        => Lobby?.SetData(MPProtocol.LobbyKickKey, memberId.ToString());

    /// <summary>我是不是被请出去了</summary>
    public bool IsKicked(ulong myId)
        => Lobby?.GetData(MPProtocol.LobbyKickKey) == myId.ToString();

    // ---- 发消息 ----

    /// <summary>说一句话</summary>
    /// 内容用与 Windows 版同一个方法序列化 —— 字段名和格式对不上, 对面就读不出来
    public void SendChat(string text, string senderName, MPProtocol.ChatKind kind = MPProtocol.ChatKind.Public)
    {
        var content = new MPProtocol.Chat
        {
            Content = text,
            ChatType = kind,
            SendName = senderName,
            ToName = string.Empty,
        };
        Broadcast(MPProtocol.BuildMessage(MPProtocol.MessageKind.Chat, 0, MPProtocol.EncodeContent(content)));
    }


    /// <summary>摸一下别人的桌宠</summary>
    public void SendInteract(ulong targetId, MPProtocol.InteractKind kind)
        => Send(targetId, MPProtocol.BuildMessage(MPProtocol.MessageKind.Interact, targetId,
            MPProtocol.EncodeContent(kind)));

    /// <summary>把一条消息发给房间里所有人</summary>
    public void Broadcast(ILPS message)
    {
        var data = MPProtocol.EncodeMessage(message);
        foreach (var member in Members)
            transport.Send(member.Id, data);
    }

    /// <summary>把一条消息发给某个人</summary>
    public void Send(ulong targetId, ILPS message) => transport.Send(targetId, MPProtocol.EncodeMessage(message));


    // ---- 收消息 ----

    /// <summary>
    /// 把攒下的消息都处理掉
    /// </summary>
    /// <param name="myId">我自己的 SteamId</param>
    /// <returns>这一轮处理了几条</returns>
    /// 由宿主在自己的心跳里调 —— 从 Steam 的回调线程上直接改界面是要出事的
    public int Pump(ulong myId)
    {
        int count = 0;
        while (true)
        {
            var packet = transport.Receive();
            if (packet == null)
                break;
            count++;
            try
            {
                Dispatch(packet.Value.From, packet.Value.Data, myId);
            }
            catch (Exception)
            {
                // 一条坏包不该把整条连接带崩: 对面可能是别的版本, 也可能是有人乱发
            }
        }
        return count;
    }

    private void Dispatch(ulong from, byte[] data, ulong myId)
    {
        var document = MPProtocol.Decode(data);
        var type = MPProtocol.ReadKind(document);
        var to = MPProtocol.ReadTo(document);
        var content = MPProtocol.ReadContent(document);

        switch ((MPProtocol.MessageKind)type)
        {
            case MPProtocol.MessageKind.Chat:
                ChatReceived?.Invoke(from, ReadChatContent(content));
                break;
            case MPProtocol.MessageKind.Interact:
                // 只处理发给我的那些
                if (to == myId)
                    InteractReceived?.Invoke(from, MPProtocol.DecodeContent<MPProtocol.InteractKind>(content));
                break;
            default:
                OtherReceived?.Invoke(from, type, document);
                break;
        }
    }

    /// <summary>
    /// 从聊天消息的内容里取出正文
    /// </summary>
    /// 与 Windows 版 MPMessage.GetContent&lt;Chat&gt;() 是同一个方法
    private static string ReadChatContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;
        try
        {
            return MPProtocol.DecodeContent<MPProtocol.Chat>(content).Content ?? string.Empty;
        }
        catch (Exception)
        {
            return content;
        }
    }
}
