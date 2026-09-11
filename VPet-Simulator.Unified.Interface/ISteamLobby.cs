using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 联机大厅里的一个人
/// </summary>
public readonly struct LobbyMember
{
    /// <summary>SteamId</summary>
    public ulong Id { get; }
    /// <summary>昵称</summary>
    public string Name { get; }

    public LobbyMember(ulong id, string name)
    {
        Id = id;
        Name = name;
    }
}

/// <summary>
/// 一个联机大厅
/// </summary>
/// 只包住 VPet 真正用到的那几件事. 刻意不做成"Steam 大厅的完整封装" ——
/// 用不到的东西包进来只会让另一端多实现一堆空方法。
public interface ISteamLobby
{
    /// <summary>大厅号</summary>
    ulong Id { get; }
    /// <summary>房主的 SteamId</summary>
    ulong OwnerId { get; }
    /// <summary>现在有哪些人</summary>
    IReadOnlyList<LobbyMember> Members { get; }

    /// <summary>读大厅数据</summary>
    string? GetData(string key);
    /// <summary>写大厅数据 (只有房主写得动)</summary>
    void SetData(string key, string value);
    /// <summary>读某个人的数据</summary>
    string? GetMemberData(ulong memberId, string key);
    /// <summary>写自己的数据</summary>
    void SetMemberData(string key, string value);

    /// <summary>能不能被搜到</summary>
    void SetPublic(bool value);
    /// <summary>还收不收新人</summary>
    void SetJoinable(bool value);
    /// <summary>离开</summary>
    void Leave();
}

/// <summary>
/// 联机
/// </summary>
/// 与 ISteamServices 分开是有意的: 联机是一整块可选功能, 没有它游戏照样完整,
/// 而它要的 Steam 能力(大厅、P2P)比别处多得多. 分开之后, 不打算做联机的宿主
/// 实现一个 ISteamServices 就够了。
public interface ISteamMultiplayer
{
    /// <summary>联机能用吗</summary>
    bool IsAvailable { get; }

    /// <summary>不可用的原因, 可用时为 null</summary>
    string? UnavailableReason { get; }

    /// <summary>现在在哪个大厅里, 没有则为 null</summary>
    ISteamLobby? CurrentLobby { get; }

    /// <summary>开一个大厅</summary>
    /// <param name="maxMembers">最多几个人</param>
    Task<ISteamLobby?> CreateLobbyAsync(int maxMembers);

    /// <summary>加入一个大厅</summary>
    Task<ISteamLobby?> JoinLobbyAsync(ulong lobbyId);

    /// <summary>有人从 Steam 好友列表点了"加入游戏"</summary>
    event Action<ulong>? JoinRequested;

    /// <summary>有人进来了</summary>
    event Action<LobbyMember>? MemberJoined;
    /// <summary>有人走了</summary>
    event Action<LobbyMember>? MemberLeft;
    /// <summary>大厅数据变了</summary>
    event Action? LobbyDataChanged;

    // ---- 点对点消息 ----

    /// <summary>
    /// 开始收发消息
    /// </summary>
    /// 里面会打开 P2P 中继并接受会话请求
    void StartNetworking();

    /// <summary>给某个人发一包</summary>
    bool Send(ulong targetId, byte[] data);

    /// <summary>
    /// 取一包收到的消息
    /// </summary>
    /// <returns>没有就返回 null</returns>
    /// 做成"轮询取一包"而不是事件: 宿主自己的心跳更清楚什么时候该处理消息,
    /// 从 Steam 的回调线程上直接改界面是要出事的
    (ulong From, byte[] Data)? Receive();
}
