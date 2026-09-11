using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 没有联机时的那一份实现
/// </summary>
/// 与 NullSteamServices 同一个道理: 判空写在十几个调用点上迟早会漏一个。
public sealed class NullSteamMultiplayer : ISteamMultiplayer
{
    /// <summary>
    /// 现成的一个
    /// </summary>
    public static readonly NullSteamMultiplayer Instance = new NullSteamMultiplayer();

    public string? UnavailableReason { get; }

    public NullSteamMultiplayer(string? reason = null)
    {
        UnavailableReason = reason ?? "联机不可用";
    }

    public bool IsAvailable => false;
    public ISteamLobby? CurrentLobby => null;

    public Task<ISteamLobby?> CreateLobbyAsync(int maxMembers) => Task.FromResult<ISteamLobby?>(null);
    public Task<ISteamLobby?> JoinLobbyAsync(ulong lobbyId) => Task.FromResult<ISteamLobby?>(null);

    // 事件永远不触发, 但 add/remove 要能调 —— 别让挂事件的地方也得判空
    public event Action<ulong>? JoinRequested { add { } remove { } }
    public event Action<LobbyMember>? MemberJoined { add { } remove { } }
    public event Action<LobbyMember>? MemberLeft { add { } remove { } }
    public event Action? LobbyDataChanged { add { } remove { } }

    public void StartNetworking() { }
    public bool Send(ulong targetId, byte[] data) => false;
    public (ulong From, byte[] Data)? Receive() => null;
}
