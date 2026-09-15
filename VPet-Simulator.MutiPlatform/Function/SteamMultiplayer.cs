//跨平台: 原文复制自 VPet-Simulator.Windows/Function/SteamMultiplayer.cs, 只换了命名空间. Facepunch.Steamworks 的托管 API 三个平台一样,
//这边按目标 RID 引用官方 Facepunch.Steamworks 2.5.2: Windows x64 用 Win64, Linux/macOS 用 Posix.
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPet_Simulator.Unified.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 联机 (Windows 侧)
/// </summary>
/// 与 SteamCapability 一样, 先在 Windows 上照着 Facepunch 的真 API 写一遍,
/// 是为了把接口验对 —— 接口设计错了, 在没有 Steam 的机器上是看不出来的。
/// 托管 API 三个平台一样, 这份代码换个包引用就能搬到跨平台侧。
internal sealed class SteamMultiplayer : ISteamMultiplayer
{
    private readonly Func<bool> isSteamUser;
    private SteamLobby? current;
    private bool networkingStarted;

    internal SteamMultiplayer(Func<bool> isSteamUser)
    {
        this.isSteamUser = isSteamUser;
        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend)
            => MemberJoined?.Invoke(new LobbyMember(friend.Id.Value, friend.Name));
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend)
            => MemberLeft?.Invoke(new LobbyMember(friend.Id.Value, friend.Name));
        SteamMatchmaking.OnLobbyDataChanged += _ => LobbyDataChanged?.Invoke();
        SteamFriends.OnGameLobbyJoinRequested += (lobby, id) => JoinRequested?.Invoke(lobby.Id.Value);
    }

    public bool IsAvailable => isSteamUser();

    public string? UnavailableReason => IsAvailable ? null : "Steam 未连接";

    public ISteamLobby? CurrentLobby => current;

    public async Task<ISteamLobby?> CreateLobbyAsync(int maxMembers)
    {
        if (!IsAvailable)
            return null;
        try
        {
            var lobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
            if (lobby == null)
                return null;
            current = new SteamLobby(lobby.Value);
            return current;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<ISteamLobby?> JoinLobbyAsync(ulong lobbyId)
    {
        if (!IsAvailable)
            return null;
        try
        {
            var result = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
            if (result == null)
                return null;
            current = new SteamLobby(result.Value);
            return current;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public event Action<ulong>? JoinRequested;
    public event Action<LobbyMember>? MemberJoined;
    public event Action<LobbyMember>? MemberLeft;
    public event Action? LobbyDataChanged;

    public void StartNetworking()
    {
        if (!IsAvailable || networkingStarted)
            return;
        try
        {
            //中继是必须的: 两边都在 NAT 后面时直连打不通, 而家用网络基本都是
            SteamNetworking.AllowP2PPacketRelay(true);
            SteamNetworking.OnP2PSessionRequest = id => SteamNetworking.AcceptP2PSessionWithUser(id);
            networkingStarted = true;
        }
        catch (Exception)
        {
        }
    }

    public bool Send(ulong targetId, byte[] data)
    {
        if (!IsAvailable)
            return false;
        try { return SteamNetworking.SendP2PPacket(targetId, data); }
        catch (Exception) { return false; }
    }

    public (ulong From, byte[] Data)? Receive()
    {
        if (!IsAvailable)
            return null;
        try
        {
            if (!SteamNetworking.IsP2PPacketAvailable())
                return null;
            var packet = SteamNetworking.ReadP2PPacket();
            if (packet == null)
                return null;
            return (packet.Value.SteamId.Value, packet.Value.Data);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 一个大厅
    /// </summary>
    private sealed class SteamLobby : ISteamLobby
    {
        private readonly Lobby lobby;

        internal SteamLobby(Lobby lobby)
        {
            this.lobby = lobby;
        }

        public ulong Id => lobby.Id.Value;
        public ulong OwnerId => lobby.Owner.Id.Value;

        public IReadOnlyList<LobbyMember> Members
        {
            get
            {
                try
                {
                    return lobby.Members.Select(x => new LobbyMember(x.Id.Value, x.Name)).ToList();
                }
                catch (Exception)
                {
                    return Array.Empty<LobbyMember>();
                }
            }
        }

        public string? GetData(string key)
        {
            try
            {
                var value = lobby.GetData(key);
                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch (Exception) { return null; }
        }

        public void SetData(string key, string value)
        {
            try { lobby.SetData(key, value); }
            catch (Exception) { }
        }

        public string? GetMemberData(ulong memberId, string key)
        {
            try
            {
                var value = lobby.GetMemberData(new Friend(memberId), key);
                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch (Exception) { return null; }
        }

        public void SetMemberData(string key, string value)
        {
            try { lobby.SetMemberData(key, value); }
            catch (Exception) { }
        }

        public void SetPublic(bool value)
        {
            try
            {
                if (value)
                    lobby.SetPublic();
                else
                    lobby.SetPrivate();
            }
            catch (Exception) { }
        }

        public void SetJoinable(bool value)
        {
            try { lobby.SetJoinable(value); }
            catch (Exception) { }
        }

        public void Leave()
        {
            try { lobby.Leave(); }
            catch (Exception) { }
        }
    }
}
