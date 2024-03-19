using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.MPMessage;

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
    public List<MPFriends> MPFriends = new List<MPFriends>();
    public List<MPUserControl> MPUserControls = new List<MPUserControl>();
    public winMutiPlayer(MainWindow mw, ulong? lobbyid = null)
    {
        InitializeComponent();
        this.mw = mw;
        if (lobbyid == null)
            CreateLobby();
        else
            JoinLobby(lobbyid.Value);
    }
    public async void JoinLobby(ulong lobbyid)
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
    public static ImageSource ConvertToImageSource(Steamworks.Data.Image? img)
    {
        if (img == null)
        {
            return new BitmapImage(new Uri("pack://application:,,,/Res/vpeticon.png"));
        }
        var image = img.Value;
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
    ulong owner;
    public void ShowLobbyInfo()
    {
        _ = Task.Run(async () =>
        {
            lb.SetMemberData("save", mw.GameSavesData.GameSave.ToLine().ToString());
            lb.SetMemberData("onmod", mw.Set.FindLine("onmod")?.ToString() ?? "onmod");
            lb.SetMemberData("petgraph", mw.Set.PetGraph);

            SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;
            SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
            Steamworks.Data.Image? img = await lb.Owner.GetMediumAvatarAsync();

            Dispatcher.Invoke(() =>
            {
                hostName.Text = lb.Owner.Name;
                owner = lb.Owner.Id.Value;
                lbLid.Text = lb.Id.Value.ToString("x");
                HostHead.Source = ConvertToImageSource(img.Value);
            });

            SteamNetworking.AllowP2PPacketRelay(true);
            SteamNetworking.OnP2PSessionRequest = (steamid) =>
            {
                SteamNetworking.AcceptP2PSessionWithUser(steamid);
            };

            //给自己动画添加绑定
            mw.Main.GraphDisplayHandler += Main_GraphDisplayHandler;
            if (lb.Owner.IsMe)
            {
                Dispatcher.Invoke(() =>
                {
                    hostPet.Text = mw.GameSavesData.GameSave.Name;
                    Title = "{0}的访客表".Translate(mw.GameSavesData.GameSave.Name);
                });
            }
            //获取成员列表
            foreach (var v in lb.Members)
            {
                if (v.Id == SteamClient.SteamId) continue;
                var mpf = Dispatcher.Invoke(() =>
                  {
                      var mpf = new MPFriends(this, mw, lb, v);
                      MPFriends.Add(mpf);
                      mpf.Show();
                      var mpuc = new MPUserControl(this, mpf);
                      MUUCList.Children.Add(mpuc);
                      MPUserControls.Add(mpuc);
                      return mpf;
                  });
                if (v.Id == lb.Owner.Id)
                    _ = Task.Run(() =>
                    {
                        //加载lobby传过来的数据       
                        while (!mpf.Loaded)
                        {
                            Thread.Sleep(500);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            Title = "{0}的访客表".Translate(mpf.Core.Save.Name);
                            hostPet.Text = mpf.Core.Save.Name;
                        });
                    });
            }
            LoopP2PPacket();
        });
    }

    private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        if (friend.Id == owner)
        {
            Task.Run(() => MessageBox.Show("访客表已被房主{0}关闭".Translate(friend.Name)));
            lb = default(Lobby);
            Close();
        }
        else
        {
            var mpuc = MPUserControls.Find(x => x.mpf.friend.Id == friend.Id);
            if (mpuc != null)
            {
                MPUserControls.Remove(mpuc);
                MUUCList.Children.Remove(mpuc);
                MPFriends.Remove(mpuc.mpf);
                mpuc.mpf.Quit();
            }
        }
    }
    GraphInfo lastgraph = new GraphInfo() { Type = GraphType.Common };
    private void Main_GraphDisplayHandler(GraphInfo info)
    {
        if (info.Type == GraphType.Shutdown || info.Type == GraphType.Common || info.Type == GraphType.Move
            || info.Type == GraphType.Raised_Dynamic || info.Type == GraphType.Raised_Static || info.Type == GraphType.Say)
        {
            return;
        }
        //如果是同一个动画就不发送
        if (lastgraph.Type == info.Type && lastgraph.Animat == info.Animat && info.Name == lastgraph.Name)
            return;
        lastgraph = info;
        MPMessage msg = new MPMessage();
        msg.Type = MSGType.DispayGraph;
        msg.SetContent(info);
        msg.To = SteamClient.SteamId.Value;
        SendMessageALL(msg);
    }
    /// <summary>
    /// 给指定好友发送消息
    /// </summary>
    /// <param name="mpf"></param>
    /// <param name="msg"></param>
    public void SendMessage(ulong friendid, MPMessage msg)
    {
        byte[] data = ConverTo(msg);
        SteamNetworking.SendP2PPacket(friendid, data);
    }
    /// <summary>
    /// 给所有人发送消息
    /// </summary>
    public void SendMessageALL(MPMessage msg)
    {
        byte[] data = ConverTo(msg);
        for (int i = 0; i < MPFriends.Count; i++)
        {
            MPFriends v = MPFriends[i];
            SteamNetworking.SendP2PPacket(v.friend.Id, data);
        }
    }


    private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id)
        {
            var mpf = new MPFriends(this, mw, lb, friend);
            MPFriends.Add(mpf);
            mpf.Show();
            var mpuc = new MPUserControl(this, mpf);
            MUUCList.Children.Add(mpuc);
            MPUserControls.Add(mpuc);
        }
    }


    private void SteamMatchmaking_OnLobbyDataChanged(Lobby lobby)
    {
        if (lobby.Id == lb.Id)
        {

        }
    }
    private void LoopP2PPacket()
    {
        while (isOPEN)
            try
            {
                while (SteamNetworking.IsP2PPacketAvailable())
                {
                    var packet = SteamNetworking.ReadP2PPacket();
                    if (packet.HasValue)
                    {
                        var From = packet.Value.SteamId;
                        var MSG = ConverTo(packet.Value.Data);
                        var To = MPFriends.Find(x => x.friend.Id == MSG.To);
                        switch (MSG.Type)
                        {
                            case MSGType.DispayGraph:
                                To.DisplayGraph(MSG.GetContent<GraphInfo>());
                                break;
                            case MSGType.Chat:
                                To.DisplayMessage(MSG.GetContent<Chat>());
                                break;
                        }
                    }
                    Thread.Sleep(100);
                }
                Thread.Sleep(1000);
            }
            catch
            {

            }
    }
    private void Window_Closed(object sender, EventArgs e)
    {
        SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
        mw.Main.GraphDisplayHandler -= Main_GraphDisplayHandler;
        SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
        lb.Leave();
        for (int i = 0; i < MPFriends.Count; i++)
        {
            MPFriends[i].Quit();
        }
        mw.winMutiPlayer = null;
    }
    bool isOPEN = true;
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!lb.Equals(default(Lobby)))
            if (MessageBoxX.Show("确定要关闭访客表吗?".Translate(), "离开游戏", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        isOPEN = false;
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
