using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.MPMessage;
using static VPet_Simulator.Windows.Win32;

namespace VPet_Simulator.Windows;
/// <summary>
/// winMutiPlayer.xaml 的交互逻辑
/// </summary>
public partial class winMutiPlayer : WindowX, IMPWindows
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
        if (mw.Core.Save.Mode == IGameSave.ModeType.Ill)
        {
            MessageBoxX.Show("{0}生病了,无法创建或者加入访客表".Translate());
            Close();
            return;
        }

        swAllowTouch.IsChecked = !mw.Set.MPNOTouch;
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
        lb.SetData("isvpets", "true");
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
        giveprice = -1000 - (100 * (mw.GameSavesData.GameSave.LevelMax + 1) + mw.GameSavesData.GameSave.Level) * 100;
        _ = Task.Run(async () =>
        {
            lb.SetMemberData("save", mw.GameSavesData.GameSave.ToLine().ToString());
            lb.SetMemberData("onmod", mw.Set.FindLine("onmod")?.ToString() ?? "onmod");
            lb.SetMemberData("petgraph", mw.Set.PetGraph);
            lb.SetMemberData("notouch", mw.Set.MPNOTouch.ToString());
            lb.SetMemberData("hash", mw.HashCheck.ToString());

            SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;

            Steamworks.Data.Image? img = await lb.Owner.GetMediumAvatarAsync();

            Dispatcher.Invoke(() =>
            {
                hostName.Text = lb.Owner.Name;
                HostID = lb.Owner.Id.Value;
                lbLid.Text = lb.Id.Value.ToString("x");
                HostHead.ImageSource = ConvertToImageSource(img);
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
        //if (mw.GameSavesData.GameSave.Mode == IGameSave.ModeType.Ill)
        //{//生病自动退出访客表
        //    ClosingMutiPlayer?.Invoke();
        //    isOPEN = false;
        //    lb.Leave();
        //    lb = default;
        //    MessageBoxX.Show("{0}生病了,已自动退出访客表".Translate(obj.Core.Save.Name));
        //    Close();
        //    return;
        //}
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
        if (info.Type == GraphType.Shutdown || info.Type == GraphType.Common
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
    public bool SendMessage(ulong friendid, MPMessage msg)
    {
        byte[] data = ConverTo(msg);
        return SteamNetworking.SendP2PPacket(friendid, data);
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
        if (lobby.Id == lb.Id && MPFriends.Find(x => x.friend.Id == friend.Id) == null)
        { //如果有未处理的退出,不管
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
                                To?.DisplayGraph(MSG.GetContent<GraphInfo>());
                                break;
                            case (int)MSGType.Chat:
                                To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                To?.DisplayMessage(MSG.GetContent<Chat>());
                                break;
                            case (int)MSGType.Interact:
                                var byname = lb.Members.First(x => x.Id == From).Name;
                                var interact = MSG.GetContent<Interact>();
                                if (MSG.To == SteamClient.SteamId.Value)
                                {
                                    if (mw.Set.MPNOTouch) return;
                                    bool isok = !IMPFriend.InConvenience(mw.Main);
                                    switch (interact)
                                    {
                                        case Interact.TouchHead:
                                            mw.Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, mw.Core.Save.Name), 3000);
                                            if (isok)
                                                DisplayNOCALTouchHead();
                                            break;
                                        case Interact.TouchBody:
                                            mw.Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, mw.Core.Save.Name), 3000);
                                            if (isok)
                                                DisplayNOCALTouchBody();
                                            break;
                                        case Interact.TouchPinch:
                                            mw.Main.LabelDisplayShow("{0}在捏{1}的脸".Translate(byname, mw.Core.Save.Name), 3000);
                                            if (isok)
                                                DisplayNOCALTouchPinch();
                                            break;
                                    }
                                }
                                else
                                {
                                    To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                    To?.ActiveInteract(byname, interact);
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
                                        mw.Main.LabelDisplayShow("{0}花费${3}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName, feed.Item.Price), 6000);
                                        Log("{0}花费${3}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName, feed.Item.Price));
                                        //对于要修改数据的物品一定要再次检查,避免联机开挂毁存档
                                        if (item.Price >= 10 && item.Price <= (100 * (mw.GameSavesData.GameSave.LevelMax + 1) + mw.GameSavesData.GameSave.Level) * 10 && item.Health >= 0 && item.Exp >= 0 && item.Likability >= 0 && giveprice < 0
                                           && item.Strength >= 0 && item.StrengthDrink >= 0 && item.StrengthFood >= 0 && item.Feeling >= 0)
                                        {//单次联机收礼物上限 (100 * (精英化次数+1) + 等级)*10
                                            giveprice += item.Price;
                                            mw.TakeItemHandle(item, 1, "friend");
                                            mw.TakeItem(feed.Item);
                                        }
                                    }
                                    else
                                    {
                                        mw.Main.LabelDisplayShow("{0}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName), 6000);
                                        Log("{0}给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName));
                                    }
                                }
                                else
                                {
                                    To = MPFriends.Find(x => x.friend.Id == MSG.To);
                                    To?.Feed(byname, feed);
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
            if (MessageBoxX.Show("确定要关闭访客表吗?".Translate(), "离开访客表".Translate(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
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
    /// 显示本体摸头情况
    /// </summary>
    public void DisplayNOCALTouchHead()
    {
        if (mw.Main.DisplayType.Type == GraphType.Touch_Head)
        {
            if (mw.Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (mw.Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => mw.Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (Dispatcher.Invoke(() => mw.Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        mw.Main.Display(GraphType.Touch_Head, AnimatType.A_Start, (graphname) =>
           mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           mw.Main.DisplayCEndtoNomal(graphname))));
    }
    /// <summary>
    /// 显示摸身体情况
    /// </summary>
    public void DisplayNOCALTouchBody()
    {
        if (mw.Main.DisplayType.Type == GraphType.Touch_Body)
        {
            if (mw.Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (mw.Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => mw.Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (Dispatcher.Invoke(() => mw.Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        mw.Main.Display(GraphType.Touch_Body, AnimatType.A_Start, (graphname) =>
         mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         mw.Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         mw.Main.DisplayCEndtoNomal(graphname))));
    }
    /// <summary>
    /// 显示本体捏脸情况
    /// </summary>
    public void DisplayNOCALTouchPinch()
    {
        if (mw.Main.DisplayType.Name == "pinch")
        {
            if (mw.Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (mw.Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => mw.Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (Dispatcher.Invoke(() => mw.Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
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
        lb.SetMemberData("notouch", "false");
        mw.Set.MPNOTouch = false;
    }

    private void swAllowTouch_Unchecked(object sender, RoutedEventArgs e)
    {
        if (mw == null) return;
        lb.SetMemberData("notouch", "true");
        mw.Set.MPNOTouch = true;
    }

    public IMPFriend SelftoIMPFriend()
    {
        return new SelfFriends(mw, lb);
    }
    /// <summary>
    /// 把自己伪装好友,方便处理数据
    /// </summary>
    public class SelfFriends : IMPFriend
    {
        MainWindow mw;
        Lobby lb;
        Friend friend;
        winMutiPlayer wmp => mw.winMutiPlayer;

        public SelfFriends(MainWindow mw, Lobby lb)
        {
            this.mw = mw;
            this.lb = lb;
            friend = lb.Members.First(x => x.Id == SteamClient.SteamId);
        }

        public ulong LobbyID => lb.Id;

        public ulong FriendID => friend.Id;


        // Stream Data
        public bool IsMe => friend.IsMe;
        public bool IsFriend => friend.IsFriend;
        public bool IsBlocked => friend.IsBlocked;
        public bool IsPlayingThisGame => friend.IsPlayingThisGame;
        public bool IsOnline => friend.IsOnline;
        public bool IsAway => friend.IsAway;
        public bool IsBusy => friend.IsBusy;
        public bool IsSnoozing => friend.IsSnoozing;
        public int SteamLevel => friend.SteamLevel;


        public string Name => friend.Name;



        public ImageSource Avatar
        {
            get
            {
                var avatarTask = friend.GetMediumAvatarAsync();
                avatarTask.Wait();
                var img = avatarTask.Result;
                return ConvertToImageSource(img);
            }
        }

        public GameCore Core => mw.Core;

        public ImageResources ImageSources => mw.ImageSources;

        public string SetPetGraph => mw.Set.PetGraph;

        public Main Main => mw.Main;

        public bool NOTouch => mw.Set.MPNOTouch;




        /// <summary>
        /// 智能化显示后续过度动画
        /// </summary>
        public void DisplayAuto(GraphInfo gi)
        {
            switch (gi.Animat)
            {
                case AnimatType.A_Start:
                    gi.Animat = AnimatType.B_Loop;
                    var img = Core.Graph.FindGraphs(gi.Name, gi.Animat, Core.Save.Mode).FindAll(x => x.GraphInfo.Type == gi.Type);
                    if (img.Count != 0)
                    {
                        Main.Display(img[Function.Rnd.Next(img.Count)], () => DisplayAuto(gi));
                    }
                    else
                    {
                        Main.DisplayToNomal();
                    }
                    break;
                case AnimatType.B_Loop:
                    img = Core.Graph.FindGraphs(gi.Name, gi.Animat, Core.Save.Mode).FindAll(x => x.GraphInfo.Type == gi.Type);
                    if (img.Count != 0)
                    {
                        Main.Display(img[Function.Rnd.Next(img.Count)], () => DisplayAuto(gi));
                    }
                    else
                    {
                        Main.DisplayToNomal();
                    }
                    break;
                case AnimatType.C_End:
                case AnimatType.Single:
                    Main.DisplayToNomal();
                    break;
            }
        }
        /// <summary>
        /// 根据好友数据显示动画
        /// </summary>
        public bool DisplayGraph(GraphInfo gi)
        {
            if (InConvenience())
                return false;
            if (gi.Type == Main.DisplayType.Type && gi.Animat == Main.DisplayType.Animat)
            {
                if (gi.Type != GraphType.Common)
                    return false;
            }
            var img = Core.Graph.FindGraphs(gi.Name, gi.Animat, Core.Save.Mode).FindAll(x => x.GraphInfo.Type == gi.Type);
            if (img.Count != 0)
            {
                Main.Display(img[Function.Rnd.Next(img.Count)], () => DisplayAuto(gi));
                return true;
            }
            return false;
        }

        public void DisplayMessage(Chat msg)
        {
            switch (msg.ChatType)
            {
                case Chat.Type.Private:
                    Main.Say("{0} 悄悄地对你说: {1}".Translate(msg.SendName, msg.Content));
                    wmp.Log("{0} 悄悄地对你说: {1}".Translate(msg.SendName, msg.Content));
                    break;
                case Chat.Type.Internal:
                    Main.Say("{0} 对 {2} 说: {1}".Translate(msg.SendName, msg.Content, msg.ToName));
                    wmp.Log("{0} 对 {2} 说: {1}".Translate(msg.SendName, msg.Content, msg.ToName));
                    break;
                case Chat.Type.Public:
                    Main.Say("{0} 对大家说: {1}".Translate(msg.SendName, msg.Content));
                    wmp.Log("{0} 对大家说: {1}".Translate(msg.SendName, msg.Content));
                    break;
            }
        }

        /// <summary>
        /// 显示吃东西(夹层)动画
        /// </summary>
        /// <param name="graphName">夹层动画名</param>
        /// <param name="imageSource">被夹在中间的图片</param>
        public void DisplayFoodAnimation(string graphName, ImageSource imageSource)
        {
            mw.DisplayFoodAnimation(graphName, imageSource);
        }
        public bool InConvenience() => IMPFriend.InConvenience(Main);
    }
}
