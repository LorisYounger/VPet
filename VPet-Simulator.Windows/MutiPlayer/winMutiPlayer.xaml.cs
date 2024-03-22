using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Microsoft.VisualBasic.Logging;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Xml.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.MPMessage;

namespace VPet_Simulator.Windows;
/// <summary>
/// winMutiPlayer.xaml 的交互逻辑
/// </summary>
public partial class winMutiPlayer : Window, IMPWindows
{
    public Lobby lb;
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
        IsHost = true;
        swAllowJoin.IsEnabled = true;
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

    public ulong HostID { get; set; }
    public bool IsHost { get; set; } = false;

    public ulong LobbyID => lb.Id.Value;

    public bool Joinable { get; set; } = true;

    public IEnumerable<IMPFriend> Friends => MPFriends;

    public bool IsGameRunning { get; set; }

    public TabControl TabControl => tabControl;

    public void ShowLobbyInfo()
    {
        _ = Task.Run(async () =>
        {
            lb.SetMemberData("save", mw.GameSavesData.GameSave.ToLine().ToString());
            lb.SetMemberData("onmod", mw.Set.FindLine("onmod")?.ToString() ?? "onmod");
            lb.SetMemberData("petgraph", mw.Set.PetGraph);
            lb.SetMemberData("notouch", mw.Set.MPNOTouch.ToString());

            SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;

            Steamworks.Data.Image? img = await lb.Owner.GetMediumAvatarAsync();

            Dispatcher.Invoke(() =>
            {
                hostName.Text = lb.Owner.Name;
                HostID = lb.Owner.Id.Value;
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
            mw.Main.TimeHandle += Main_TimeHandle;
            if (IsHost)
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
            mw.MutiPlayerStart(this);
            Log("已成功连接到访客表".Translate());
            LoopP2PPacket();
        });
    }

    private void Main_TimeHandle(Main obj)
    {
        lb.SetMemberData("save", mw.GameSavesData.GameSave.ToLine().ToString());
    }

    private void SteamMatchmaking_OnLobbyDataChanged(Lobby lobby)
    {
        if (lb.Id == lobby.Id)
        {
            if (lb.GetData("kick") == SteamClient.SteamId.Value.ToString())
            {
                Task.Run(() => MessageBox.Show("访客表已被房主{0}关闭".Translate(lb.Owner.Name)));//温柔的谎言
                lb.Leave();
                lb = default(Lobby);
                Close();
            }

            if (lb.GetData("nojoin") == "true")
            {
                Joinable = false;
                Dispatcher.Invoke(() => swAllowJoin.IsChecked = false);
            }
            else
            {
                Joinable = true;
                Dispatcher.Invoke(() => swAllowJoin.IsChecked = true);
            }
        }
    }

    public event Action<ulong> OnMemberLeave;
    private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        if (lobby.Id != lb.Id) return;
        OnMemberLeave?.Invoke(friend.Id);
        if (friend.Id == HostID)
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
            Log("好友{0}已退出访客表".Translate(friend.Name));
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
        msg.Type = (int)MSGType.DispayGraph;
        msg.SetContent(info);
        msg.To = SteamClient.SteamId.Value;
        SendMessageALL(msg);
    }
    /// <summary>
    /// 给指定好友发送消息
    /// </summary>
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

    /// <summary>
    /// 发送日志消息
    /// </summary>
    /// <param name="message">日志</param>
    public void Log(string message)
    {
        Dispatcher.Invoke(() => tbLog.AppendText($"[{DateTime.Now.ToShortTimeString()}]{message}\n"));
    }

    /// <summary>
    /// 事件:成员加入
    /// </summary>
    public event Action<ulong> OnMemberJoined;
    private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id)
        {
            Log("好友{0}已加入访客表".Translate(friend.Name));
            var mpf = new MPFriends(this, mw, lb, friend);
            MPFriends.Add(mpf);
            mpf.Show();
            var mpuc = new MPUserControl(this, mpf);
            MUUCList.Children.Add(mpuc);
            MPUserControls.Add(mpuc);
            OnMemberJoined?.Invoke(friend.Id);
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
                        SteamId From = packet.Value.SteamId;
                        var MSG = ConverTo(packet.Value.Data);
                        ReceivedMessage?.Invoke(From.Value, MSG);
                        switch (MSG.Type)
                        {
                            case (int)MSGType.DispayGraph:
                                var To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                To.DisplayGraph(MSG.GetContent<GraphInfo>());
                                break;
                            case (int)MSGType.Chat:
                                To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                To.DisplayMessage(MSG.GetContent<Chat>());
                                break;
                            case (int)MSGType.Interact:
                                var byname = lb.Members.First(x => x.Id == From).Name;
                                var interact = MSG.GetContent<Interact>();
                                if (MSG.To == SteamClient.SteamId.Value)
                                {
                                    bool isok = !IMPFriend.InConvenience(mw.Main);
                                    switch (interact)
                                    {
                                        case Interact.TouchHead:
                                            mw.Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, mw.Core.Save.Name), 5000);
                                            if (isok)
                                                DisplayNOCALTouchHead();
                                            break;
                                        case Interact.TouchBody:
                                            mw.Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, mw.Core.Save.Name), 5000);
                                            if (isok)
                                                DisplayNOCALTouchBody();
                                            break;
                                        case Interact.TouchPinch:
                                            mw.Main.LabelDisplayShow("{0}在捏{1}的脸".Translate(byname, mw.Core.Save.Name), 5000);
                                            if (isok)
                                                DisplayNOCALTouchPinch();
                                            break;
                                    }
                                }
                                else
                                {
                                    To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                    To.ActiveInteract(byname, interact);
                                }
                                break;
                            case (int)MSGType.Feed:
                                byname = lb.Members.First(x => x.Id == From).Name;
                                var feed = MSG.GetContent<Feed>();
                                if (MSG.To == SteamClient.SteamId.Value)
                                {
                                    var item = feed.Item;
                                    feed.Item.ImageSource = Dispatcher.Invoke(() => mw.ImageSources.FindImage("food_" + (item.Image ?? item.Name), "food"));
                                    mw.DisplayFoodAnimation(feed.Item.GetGraph(), feed.Item.ImageSource);
                                    if (feed.EnableFunction)
                                    {
                                        mw.Main.LabelDisplayShow("{0}花费${3}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName, feed.Item.Price), 10000);
                                        Log("{0}花费${3}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName, feed.Item.Price));
                                        //对于要修改数据的物品一定要再次检查,避免联机开挂毁存档
                                        if (item.Price >= 10 && item.Price <= 1000 && item.Health >= 0 && item.Exp >= 0 && item.Likability >= 0 && giveprice < 1000
                                           && item.Strength >= 0 && item.StrengthDrink >= 0 && item.StrengthFood >= 0 && item.Feeling >= 0)
                                        {//单次联机收礼物上限1000
                                            giveprice += item.Price;
                                            mw.TakeItem(feed.Item);
                                        }
                                    }
                                    else
                                    {
                                        mw.Main.LabelDisplayShow("{0}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName), 10000);
                                        Log("{0}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName));
                                    }
                                }
                                else
                                {
                                    To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                    To.Feed(byname, feed);
                                }
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
    private double giveprice = 0;
    public event Action<ulong, MPMessage> ReceivedMessage;
    private void Window_Closed(object sender, EventArgs e)
    {
        mw.Main.TimeHandle -= Main_TimeHandle;
        mw.Main.GraphDisplayHandler -= Main_GraphDisplayHandler;
        SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
        lb.Leave();
        for (int i = 0; i < MPFriends.Count; i++)
        {
            MPFriends[i].Quit();
        }
        mw.winMutiPlayer = null;
    }
    bool isOPEN = true;
    /// <summary>
    /// 事件: 结束访客表, 窗口关闭
    /// </summary>
    public event Action ClosingMutiPlayer;
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!lb.Equals(default(Lobby)))
            if (MessageBoxX.Show("确定要关闭访客表吗?".Translate(), "离开游戏", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        ClosingMutiPlayer?.Invoke();
        isOPEN = false;
    }

    private void swAllowJoin_Checked(object sender, RoutedEventArgs e)
    {
        lb.SetData("nojoin", "false");
        lb.SetJoinable(true);
    }

    private void swAllowJoin_Unchecked(object sender, RoutedEventArgs e)
    {
        lb.SetData("nojoin", "true");
        lb.SetJoinable(false);
    }

    /// <summary>
    /// 显示本体摸头情况 (会无损加心情)
    /// </summary>
    public void DisplayNOCALTouchHead()
    {
        mw.Main.Core.Save.FeelingChange(1);
        if (mw.Main.DisplayType.Type == GraphType.Touch_Head)
        {
            if (mw.Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (mw.Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => mw.Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.IsContinue = true;
                    return;
                }
                else if (Dispatcher.Invoke(() => mw.Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.IsContinue = true;
                    return;
                }
        }
        mw.Main.Display(GraphType.Touch_Head, AnimatType.A_Start, (graphname) =>
           mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           mw.Main.DisplayCEndtoNomal(graphname))));
    }
    /// <summary>
    /// 显示摸身体情况 (会无损加心情)
    /// </summary>
    public void DisplayNOCALTouchBody()
    {
        mw.Main.Core.Save.FeelingChange(1);
        if (mw.Main.DisplayType.Type == GraphType.Touch_Body)
        {
            if (mw.Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (mw.Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => mw.Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.IsContinue = true;
                    return;
                }
                else if (Dispatcher.Invoke(() => mw.Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.IsContinue = true;
                    return;
                }
        }
        mw.Main.Display(GraphType.Touch_Body, AnimatType.A_Start, (graphname) =>
         mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         mw.Main.DisplayCEndtoNomal(graphname))));
    }
    /// <summary>
    /// 显示本体捏脸情况 (会无损加心情)
    /// </summary>
    public void DisplayNOCALTouchPinch()
    {
        mw.Main.Core.Save.FeelingChange(1);
        if (mw.Main.DisplayType.Name == "pinch")
        {
            if (mw.Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (mw.Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => mw.Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.IsContinue = true;
                    return;
                }
                else if (Dispatcher.Invoke(() => mw.Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.IsContinue = true;
                    return;
                }
        }
        mw.Main.Display("pinch", AnimatType.A_Start, (graphname) =>
           mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           mw.Main.DisplayCEndtoNomal(graphname))));
    }

    private void swAllowTouch_Checked(object sender, RoutedEventArgs e)
    {
        if (mw == null) return;
        lb.SetMemberData("notouch", "true");
        mw.Set.MPNOTouch = false;
    }

    private void swAllowTouch_Unchecked(object sender, RoutedEventArgs e)
    {
        lb.SetMemberData("notouch", "false");
        mw.Set.MPNOTouch = false;
    }
}
