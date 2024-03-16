using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VPet_Simulator.Windows;
/// <summary>
/// winMutiPlayer.xaml 的交互逻辑
/// </summary>
public partial class winMutiPlayer : Window
{
    Steamworks.Data.Lobby lb;
    MainWindow mw;
    /// <summary>
    /// 好友宠物模块
    /// </summary>
    List<MPFriends> mFriends = new List<MPFriends>();
    public winMutiPlayer(MainWindow mw, ulong? lobbyid = null)
    {
        InitializeComponent();
        this.mw = mw;
        if (lobbyid == null)
            CreateLobby();
        else
            JoinLobby(lobbyid);
    }
    public async void JoinLobby(ulong? lobbyid)
    {
        var lbt = (await SteamMatchmaking.JoinLobbyAsync((SteamId)lobbyid));
        if (!lbt.HasValue || lbt.Value.Owner.Id.Value == 0)
        {
            MessageBoxX.Show("加入/创建访客表失败，请检查网络连接或重启游戏".Translate());
            Close();
            return;
        }
        lb = lbt.Value;
        ShowLobbyInfo();
    }
    public async void CreateLobby()
    {
        var lbt = (await SteamMatchmaking.CreateLobbyAsync());
        if (!lbt.HasValue)
        {
            MessageBoxX.Show("加入/创建访客表失败，请检查网络连接或重启游戏".Translate());
            Close();
            return;
        }
        lb = lbt.Value;
        lb.SetJoinable(true);
        lb.SetPublic();
        swAllowJoin.Visibility = Visibility.Visible;
        ShowLobbyInfo();
    }
    public static BitmapFrame ConvertToImageSource(Steamworks.Data.Image image)
    {
        int stride = (int)((image.Width * 32 + 7) / 8); // 32 bits per pixel
                                                        // Convert RGBA to BGRA
        for (int i = 0; i < image.Data.Length; i += 4)
        {
            byte r = image.Data[i];
            image.Data[i] = image.Data[i + 2];
            image.Data[i + 2] = r;
        }
        var bitmap = BitmapSource.Create(
            (int)image.Width,
            (int)image.Height,
            96, 96, // dpi x, dpi y
            PixelFormats.Bgra32, // Pixel format
            null, // Bitmap palette
            image.Data, // Pixel data
            stride // Stride
        );

        // Convert to ImageSource
        var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder(); // or use another encoder if you want
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);

        BitmapFrame result = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return result;
    }
    public async void ShowLobbyInfo()
    {
        lb.SetMemberData("save", mw.GameSavesData.GameSave.ToLine().ToString());
        lb.SetMemberData("onmod", mw.Set.FindLine("onmod")?.ToString() ?? "onmod");
        lb.SetMemberData("petgraph", mw.Set.PetGraph);

        SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;
        SteamMatchmaking.OnLobbyMemberDataChanged += SteamMatchmaking_OnLobbyMemberDataChanged;
        SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
        hostName.Text = lb.Owner.Name;
        lbLid.Text = lb.Id.Value.ToString("x");
        Steamworks.Data.Image? img = await lb.Owner.GetMediumAvatarAsync();
        if (img.HasValue)
        {
            HostHead.Source = ConvertToImageSource(img.Value);
        }
        else
        {
            HostHead.Source = new BitmapImage(new Uri("pack://application:,,,/Res/vpeticon.png"));
        }
    }

    private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id)
        {

        }
    }

    private void SteamMatchmaking_OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id)
        {

        }
    }

    private void SteamMatchmaking_OnLobbyDataChanged(Lobby lobby)
    {
        if (lobby.Id == lb.Id)
        {

        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
        SteamMatchmaking.OnLobbyMemberDataChanged -= SteamMatchmaking_OnLobbyMemberDataChanged;
        lb.SetMemberData("leave", DateTime.Now.ToString());
        lb.Leave();
        foreach (var item in mFriends)
        {
            item.Close();
        }
        mw.winMutiPlayer = null;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!lb.Equals(default(Lobby)))
            if (MessageBoxX.Show("确定要关闭访客表吗?".Translate(), "离开游戏", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
    }

    private void swAllowJoin_Checked(object sender, RoutedEventArgs e)
    {
        lb.SetJoinable(true);
    }

    private void swAllowJoin_Unchecked(object sender, RoutedEventArgs e)
    {
        lb.SetJoinable(false);
    }
}
