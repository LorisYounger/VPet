using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;

#if VPET_AVALONIA
namespace VPet_Simulator.MutiPlatform;
#else
namespace VPet_Simulator.Windows;
#endif

internal sealed partial class UnifiedPluginHost
{
    public bool IsSteamUser => mw.IsSteamUser;
    public event Action<IMPHost>? MutiPlayerHandle;

    internal static void MutiPlayerStart(IEnumerable<UnifiedPluginHost> hosts, IMPWindows window)
    {
        UnifiedMPHost? room = null;
        foreach (var host in hosts)
            host.MutiPlayerHandle?.Invoke(room ??= new UnifiedMPHost(window));
    }
}

internal sealed class UnifiedMPHost : IMPHost
{
    private readonly IMPWindows window;

    internal UnifiedMPHost(IMPWindows window)
    {
        this.window = window;
        window.ReceivedMessage += OnMessage;
        window.ClosingMutiPlayer += OnClosing;
    }

    public ulong LobbyID => window.LobbyID;
    public ulong HostID => window.HostID;
    public bool IsHost => window.IsHost;
    public bool Joinable => window.Joinable;
    public bool IsGameRunning { get => window.IsGameRunning; set => window.IsGameRunning = value; }
    public object TabControl => window.TabControl;
    public void Log(string message) => window.Log(message);

    public (ulong FriendID, string Name, int? Level) Self => Member(window.SelftoIMPFriend());
    public IEnumerable<(ulong FriendID, string Name, int? Level)> Friends => window.Friends.Select(Member);
    private static (ulong, string, int?) Member(IMPFriend friend)
        => (friend.FriendID, friend.Name, friend.Core.Save?.Level);

    public bool SendMessage(ulong friendID, UnifiedMPMessage message) => window.SendMessage(friendID, ToMessage(message));
    public void SendMessageALL(UnifiedMPMessage message) => window.SendMessageALL(ToMessage(message));
    private static MPMessage ToMessage(UnifiedMPMessage message)
    {
        var packet = new MPMessage { Type = message.Type, To = message.To };
        if (message.Content != null)
            packet.SetContent(message.Content);
        return packet;
    }

    public event Action<ulong, UnifiedMPMessage>? ReceivedMessage;
    private void OnMessage(ulong from, MPMessage packet) => ReceivedMessage?.Invoke(from,
        new UnifiedMPMessage { Type = packet.Type, To = packet.To, Content = packet.GetContent() });

    public event Action<ulong>? OnMemberJoined
    {
        add => window.OnMemberJoined += value;
        remove => window.OnMemberJoined -= value;
    }
    public event Action<ulong>? OnMemberLeave
    {
        add => window.OnMemberLeave += value;
        remove => window.OnMemberLeave -= value;
    }
    public event Action? ClosingMutiPlayer;
    private void OnClosing()
    {
        window.ReceivedMessage -= OnMessage;
        window.ClosingMutiPlayer -= OnClosing;
        ClosingMutiPlayer?.Invoke();
    }
}
