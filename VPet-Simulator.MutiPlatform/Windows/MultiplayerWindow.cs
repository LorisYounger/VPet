using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 多人联机
/// </summary>
/// 对应 Windows 版的 winMutiPlayer。线上格式与那边逐字节相同(MPProtocol),
/// 所以两个平台的玩家能连到同一个房间里。
///
/// 但**跨平台版现在连不上**: Facepunch 的 Posix 绑定要额外引一个包, 往仓库里加
/// 依赖该由项目所有者定(见 PetWindow.HostSteam)。在那之前这个窗口只会告诉玩家
/// 为什么用不了 —— 摆一堆点了没反应的按钮比直说更糟。
internal sealed class MultiplayerWindow : VPetWindow
{
    private readonly PetWindow host;
    private readonly StackPanel members = new StackPanel { Spacing = 4 };
    private readonly StackPanel chat = new StackPanel { Spacing = 2 };
    private readonly TextBox input = new TextBox
    {
        Watermark = LocalizeCore.Translate("说点什么"),
        MinWidth = 260,
    };
    private readonly TextBox lobbyId = new TextBox
    {
        Watermark = LocalizeCore.Translate("房间号"),
        MinWidth = 180,
    };
    private readonly DispatcherTimer pump = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
    private MultiplayerSession? session;

    internal MultiplayerWindow(PetWindow host)
    {
        this.host = host;
        Title = LocalizeCore.Translate("多人联机");
        CanResize = true;
        SizeToContent = SizeToContent.Manual;
        Width = 640;
        Height = 520;
        Body = BuildRoot();
        Closed += (_, _) =>
        {
            pump.Stop();
            session?.Leave();
        };
    }

    private Control BuildRoot()
    {
        session = new MultiplayerSession(host.HostMultiplayer);
        if (!session.IsAvailable)
            return BuildUnavailable(session.UnavailableReason);

        session.MemberJoined += _ => Dispatcher.UIThread.Post(RefreshMembers);
        session.MemberLeft += _ => Dispatcher.UIThread.Post(RefreshMembers);
        session.ChatReceived += (from, text) => Dispatcher.UIThread.Post(() => AddChat(NameOf(from), text));
        session.InteractReceived += (from, kind) => Dispatcher.UIThread.Post(() => OnInteract(from, kind));

        //消息在自己的心跳里取, 不在 Steam 的回调线程上碰界面
        pump.Tick += (_, _) => session.Pump(host.HostSteam.UserId);
        pump.Start();

        var root = new DockPanel();

        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        top.Children.Add(PrimaryButton(LocalizeCore.Translate("开房间"), Host));
        top.Children.Add(lobbyId);
        top.Children.Add(SecondaryButton(LocalizeCore.Translate("加入"), Join));
        top.Children.Add(SecondaryButton(LocalizeCore.Translate("离开"), Leave));
        DockPanel.SetDock(top, Dock.Top);
        root.Children.Add(top);

        var bottom = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };
        bottom.Children.Add(input);
        bottom.Children.Add(PrimaryButton(LocalizeCore.Translate("发送"), SendChat));
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var side = new Border { Width = 180, Margin = new Thickness(8, 8, 0, 0) };
        side.Classes.Add("vpet-card");
        side.Child = new ScrollViewer { Content = members };
        DockPanel.SetDock(side, Dock.Right);
        root.Children.Add(side);

        root.Children.Add(new ScrollViewer { Content = chat, Margin = new Thickness(0, 8, 0, 0) });
        RefreshMembers();
        return root;
    }

    /// <summary>
    /// 联机用不了时给玩家看的
    /// </summary>
    /// 说清楚为什么, 而不是一句"不可用"
    private Control BuildUnavailable(string? reason)
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = LocalizeCore.Translate("联机暂时用不了"),
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        panel.Children.Add(BodyText(reason ?? LocalizeCore.Translate("未知原因")));
        var hint = new TextBlock
        {
            Text = LocalizeCore.Translate(
                "联机的消息格式与 Windows 版是同一份, 接上 Steam 之后两个平台的玩家能进同一个房间。"),
            TextWrapping = TextWrapping.Wrap,
        };
        hint.Classes.Add("vpet-hint");
        panel.Children.Add(hint);
        return panel;
    }

    private string NameOf(ulong id)
    {
        var member = session?.Members.FirstOrDefault(x => x.Id == id);
        return member?.Name is { Length: > 0 } name ? name : id.ToString();
    }

    private void RefreshMembers()
    {
        members.Children.Clear();
        if (session == null)
            return;
        members.Children.Add(new TextBlock
        {
            Text = LocalizeCore.Translate("房间里的人"),
            FontSize = 15,
        });
        foreach (var member in session.Members)
        {
            var row = new StackPanel { Spacing = 2 };
            row.Children.Add(new TextBlock { Text = member.Name, TextWrapping = TextWrapping.Wrap });
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            var captured = member.Id;
            actions.Children.Add(SecondaryButton(LocalizeCore.Translate("摸头"),
                () => session.SendInteract(captured, MPProtocol.InteractKind.TouchHead)));
            row.Children.Add(actions);
            members.Children.Add(row);
        }
        if (session.Lobby != null)
        {
            members.Children.Add(new TextBlock
            {
                Text = LocalizeCore.Translate("房间号") + $"\n{session.Lobby.Id}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
            });
        }
    }

    private void AddChat(string who, string text)
    {
        chat.Children.Add(new TextBlock
        {
            Text = $"[{DateTime.Now:HH:mm}] {who}: {text}",
            TextWrapping = TextWrapping.Wrap,
        });
        //只留最近一百条: 挂一整天下来能攒出几千条, 全留着白占内存
        while (chat.Children.Count > 100)
            chat.Children.RemoveAt(0);
    }

    private void OnInteract(ulong from, MPProtocol.InteractKind kind)
    {
        if (host.HostPet == null)
            return;
        var who = NameOf(from);
        host.HostPet.LabelDisplayShow(kind switch
        {
            MPProtocol.InteractKind.TouchHead => LocalizeCore.Translate("{0}在摸{1}的头", who, host.HostGameSave.GameSave.Name),
            MPProtocol.InteractKind.TouchBody => LocalizeCore.Translate("{0}在摸{1}", who, host.HostGameSave.GameSave.Name),
            _ => LocalizeCore.Translate("{0}捏了捏{1}的脸", who, host.HostGameSave.GameSave.Name),
        }, 3000);
    }

    private async void Host()
    {
        if (session == null)
            return;
        if (!await session.HostAsync())
        {
            await DialogService.ShowAsync(LocalizeCore.Translate("开房间失败"), Title, this);
            return;
        }
        RefreshMembers();
    }

    private async void Join()
    {
        if (session == null)
            return;
        if (!ulong.TryParse(lobbyId.Text, out var id))
        {
            await DialogService.ShowAsync(LocalizeCore.Translate("房间号不对"), Title, this);
            return;
        }
        if (!await session.JoinAsync(id))
        {
            await DialogService.ShowAsync(LocalizeCore.Translate("加入失败"), Title, this);
            return;
        }
        RefreshMembers();
    }

    private void Leave()
    {
        session?.Leave();
        RefreshMembers();
    }

    private void SendChat()
    {
        var text = input.Text;
        if (session == null || string.IsNullOrWhiteSpace(text))
            return;
        session.SendChat(text, host.HostSteam.UserName);
        AddChat(host.HostSteam.UserName, text);
        input.Text = string.Empty;
    }
}
