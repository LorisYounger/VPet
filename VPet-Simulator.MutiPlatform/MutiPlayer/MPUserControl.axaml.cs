using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Steamworks;
using Steamworks.Data;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.MutiPlatform;
/// <summary>
/// MPUserControl.xaml 的交互逻辑
/// </summary>
/// 跨平台: 原文复制自 VPet-Simulator.Windows/MutiPlayer/MPUserControl.xaml.cs; Dispatcher.Invoke→Dispatcher.UIThread.Invoke,
/// Visibility→IsVisible, 其余逐行相同
public partial class MPUserControl : Border
{
    public Friend friend => mpf.friend;
    winMutiPlayer wmp;
    public MPFriends mpf;
    Lobby lb => mpf.lb;
    public MPUserControl(winMutiPlayer wmp, MPFriends mpf)
    {
        InitializeComponent();
        this.wmp = wmp;
        this.mpf = mpf;
        Task.Run(LoadInfo);
    }
    public void LoadInfo()
    {
        //加载lobby传过来的数据
        while (!mpf.Loaded)
        {
            Thread.Sleep(500);
        }
        Dispatcher.UIThread.Invoke(async () =>
        {
            rPetName.Text = mpf.Core.Save!.Name;
            hostName.Text = friend.Name;
            var img = await friend.GetMediumAvatarAsync();
            uimg.Source = winMutiPlayer.ConvertToImageSource(img);
            info.Text = "Lv " + mpf.Core.Save!.Level;
            if (lb.Owner.IsMe)
                Kick.IsVisible = true;
        });
    }

    private void btn_ReSetLocal(object? sender, RoutedEventArgs e)
    {
        mpf.ReSetLocal();
    }


    private void Kick_Click(object? sender, RoutedEventArgs e)
    {
        lb.SetData(MPProtocol.LobbyKickKey, friend.Id.Value.ToString());
    }
}
