using LinePutScript;
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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.MPMessage;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using ToolBar = VPet_Simulator.Core.ToolBar;

namespace VPet_Simulator.Windows;
/// <summary>
/// MPFriends.xaml 的交互逻辑
/// </summary>
public partial class MPFriends : WindowX, IMPFriend
{
    public Lobby lb;
    internal MainWindow mw;
    public Friend friend;
    public winMutiPlayer wmp;
    public GameCore Core { get; set; } = new GameCore();
    public List<Food> Foods { get; } = new List<Food>();
    public ImageResources ImageSources { get; } = new ImageResources();
    public List<PetLoader> Pets { get; set; } = new List<PetLoader>();
    public ILine OnMod { get; set; }

    public string SetPetGraph { get; set; }
    public bool IsOnMod(string ModName)
    {
        if (CoreMOD.OnModDefList.Contains(ModName))
            return true;
        return OnMod.Find(ModName.ToLowerInvariant()) != null;
    }
    public bool NOTouch { get; set; } = false;


    public MPFriends(winMutiPlayer wmp, MainWindow mw, Lobby lb, Friend friend)
    {
        this.wmp = wmp;
        this.mw = mw;
        this.lb = lb;
        this.friend = friend;

        mw.Windows.Add(this);
        try
        {
            InitializeComponent();

            MGrid.Height = 500 * mw.Set.ZoomLevel;
            MGrid.Width = 500 * mw.Set.ZoomLevel;
            double L = 0, T = 0;
            if (mw.Set.StartRecordLast)
            {
                var point = mw.Set.StartRecordLastPoint;
                if (point.X != 0 || point.Y != 0)
                {
                    L = point.X;
                    T = point.Y;
                }
            }
            else
            {
                var point = mw.Set.StartRecordPoint;
                L = point.X; T = point.Y;
            }

            Left = L;
            Top = T;

            // control position inside bounds
            Core.Controller = new MPController(this, mw);
            Task.Run(() =>
            {
                double dist;
                if ((dist = Core.Controller.GetWindowsDistanceLeft()) < 0)
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => Left -= dist);
                }
                if ((dist = Core.Controller.GetWindowsDistanceRight()) < 0)
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => Left += dist);
                }
                if ((dist = Core.Controller.GetWindowsDistanceUp()) < 0)
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => Top -= dist);
                }
                if ((dist = Core.Controller.GetWindowsDistanceDown()) < 0)
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => Top += dist);
                }
            });
            if (mw.Set.TopMost)
            {
                Topmost = true;
            }

        }
        catch
        {
            Close();
            return;
        }


        Task.Run(async () =>
        {
            ImageSources.AddRange(mw.ImageSources);


            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(mw.ModPath).EnumerateDirectories());

            var workshop = mw.Set["workshop"];
            foreach (Sub ws in workshop)
            {
                Path.Add(new DirectoryInfo(ws.Name));
            }

            //加载lobby传过来的数据
            string tmp = lb.GetMemberData(friend, "save");
            while (string.IsNullOrEmpty(tmp))
            {
                Thread.Sleep(500);
                tmp = lb.GetMemberData(friend, "save");
            }
            Core.Save = GameSave_VPet.Load(new Line(tmp));
            tmp = lb.GetMemberData(friend, "onmod");
            while (string.IsNullOrEmpty(tmp))
            {
                Thread.Sleep(100);
                tmp = lb.GetMemberData(friend, "onmod");
            }
            OnMod = new Line(tmp);

            tmp = lb.GetMemberData(friend, "petgraph");
            while (string.IsNullOrEmpty(tmp))
            {
                Thread.Sleep(100);
                tmp = lb.GetMemberData(friend, "onmod");
            }
            SetPetGraph = tmp;

            tmp = lb.GetMemberData(friend, "hash");

            await GameLoad(Path);
            int skipwait = 0;
            while (string.IsNullOrEmpty(tmp) && skipwait >= 10)
            {
                Thread.Sleep(100);
                tmp = lb.GetMemberData(friend, "hash");
            }
            Dispatcher.Invoke(() =>
            {
                if (tmp == "True")
                {
                    if (hashcheckimg == null)
                    {
                        hashcheckimg = new Image();
                        hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash.png");
                        hashcheckimg.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        hashcheckimg.ToolTip = "是没有修改过存档/使用超模MOD的玩家专属标志".Translate();
                        Grid.SetColumn(hashcheckimg, 4);
                        Grid.SetRowSpan(hashcheckimg, 2);
                        Main.ToolBar.gdPanel.Children.Add(hashcheckimg);
                    }
                }
                else
                {
                    if (hashcheckimg != null)
                    {
                        Main.ToolBar.gdPanel.Children.Remove(hashcheckimg);
                        hashcheckimg = null;
                    }
                }
            });

            Main.Event_TouchHead += Main_Event_TouchHead;
            Main.Event_TouchBody += Main_Event_TouchBody;
            SteamMatchmaking.OnLobbyMemberDataChanged += SteamMatchmaking_OnLobbyMemberDataChanged;

        });

    }

    private void SteamMatchmaking_OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id && friend.Id == this.friend.Id)
        {
            string tmp = lb.GetMemberData(friend, "save");
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    Core.Save = GameSave_VPet.Load(new Line(tmp));
                    Main.ToolBar.M_TimeUIHandle(Main);
                    Main.ToolBar.tfun.Visibility = Visibility.Collapsed;
                }
                if (lb.GetMemberData(friend, "notouch") == "true")
                {
                    NoTouchTrue();
                }
                else
                {
                    NoTouchFalse();
                }
            });
        }
    }
    private void NoTouchTrue()
    {
        NOTouch = true;
        Main.ToolBar.MenuInteract.IsEnabled = false;
    }
    private void NoTouchFalse()
    {
        NOTouch = false;
        Main.ToolBar.MenuInteract.IsEnabled = true;
    }

    private void Main_Event_TouchHead()
    {
        Main.LabelDisplayShow("{0}在摸{1}的头".Translate(SteamClient.Name, Core.Save.Name), 3000);
        var msg = new MPMessage() { Type = (int)MSGType.Interact, To = friend.Id };
        msg.SetContent(Interact.TouchHead);
        wmp.SendMessageALL(msg);
    }
    private void Main_Event_TouchBody()
    {
        Main.LabelDisplayShow("{0}在摸{1}的头".Translate(SteamClient.Name, Core.Save.Name), 3000);
        var msg = new MPMessage() { Type = (int)MSGType.Interact, To = friend.Id };
        msg.SetContent(Interact.TouchBody);
        wmp.SendMessageALL(msg);
    }

    internal List<MPMOD> MPMODs = new List<MPMOD>();
    public Main Main { get; set; }

    public ulong LobbyID => lb.Id;
    /// <summary>
    /// 是否显示吃东西动画
    /// </summary>
    bool showeatanm = true;
    /// <summary>
    /// 显示吃东西(夹层)动画
    /// </summary>
    /// <param name="graphName">夹层动画名</param>
    /// <param name="imageSource">被夹在中间的图片</param>
    public void DisplayFoodAnimation(string graphName, ImageSource imageSource)
    {
        if (showeatanm)
        {//显示动画
            showeatanm = false;
            Main.Display(graphName, imageSource, () =>
            {
                showeatanm = true;
                Main.DisplayToNomal();
                Main.EventTimer_Elapsed();
            });
        }
    }
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
    public ImageSource Avatar
    {
        get
        {
            // 这里假设 winMutiPlayer 有一个静态方法用于转换头像
            // 如果有缓存可用则直接返回，否则同步获取
            var avatarTask = friend.GetMediumAvatarAsync();
            avatarTask.Wait();
            var img = avatarTask.Result;
            return winMutiPlayer.ConvertToImageSource(img);
        }
    }
    /// <summary>
    /// 喂食显示动画
    /// </summary>
    /// <param name="byname"></param>
    /// <param name="feed"></param>
    public void Feed(string byname, Feed feed)
    {
        DisplayFoodAnimation(feed.Item.GetGraph(), Dispatcher.Invoke(() => ImageSources.FindImage("food_" + (feed.Item.Image ?? feed.Item.Name), "food")));
        if (feed.EnableFunction)
        {
            mw.Main.LabelDisplayShow("{0}花费${3}给{4}的{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name,
                feed.Item.TranslateName, feed.Item.Price, friend.Name));
            wmp.Log("{0}花费${3}{4}的给{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName,
                feed.Item.Price, friend.Name));
            mw.TakeItem(feed.Item);
        }
        else
        {
            mw.Main.LabelDisplayShow("{0}给{3}的{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName, friend.Name));
            wmp.Log("{0}给{3}的{1}买了{2}".Translate(byname, mw.GameSavesData.GameSave.Name, feed.Item.TranslateName, friend.Name));
        }
    }
    /// <summary>
    /// 加载游戏
    /// </summary>
    /// <param name="Path">MOD地址</param>
    public async Task GameLoad(List<DirectoryInfo> Path)
    {
        Path = Path.Distinct().ToList();
        await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading MOD"));
        //加载mod
        foreach (DirectoryInfo di in Path)
        {
            if (!File.Exists(di.FullName + @"\info.lps"))
                continue;
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = $"Loading MOD: {di.Name}"));
            MPMODs.Add(new MPMOD(di, this));
        }

        await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏MOD".Translate()));

        //当前桌宠动画
        var petloader = Pets.Find(x => x.Name == SetPetGraph);
        petloader ??= Pets[0];


        //加载数据合理化:食物       
        foreach (Food f in Foods)
        {
            if (f.IsOverLoad())
            {
                f.Price = Math.Max((int)f.RealPrice, 1);
                f.isoverload = false;
            }
        }
        await Dispatcher.InvokeAsync(new Action(() =>
        {
            LoadingText.Content = "尝试加载动画和生成缓存\n该步骤可能会耗时比较长\n请耐心等待".Translate();

            Core.Graph = petloader.Graph(mw.Set.Resolution, Dispatcher);
            Main = new Main(Core);
            Main.MsgBar = new MessageBar(Main);
            Main.MsgBar.Visibility = Visibility.Collapsed;
            Main.UIGrid.Children.Add(Main.MsgBar.This);
            Main.ToolBar = new Core.ToolBar(Main);
            Main.ToolBar.Visibility = Visibility.Collapsed;
            Main.UIGrid.Children.Add(Main.ToolBar);
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchHeadLocate, Core.Graph.GraphConfig.TouchHeadSize, () => { DisplayTouchHead(); return true; }));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchBodyLocate, Core.Graph.GraphConfig.TouchBodySize, () => { DisplayTouchBody(); return true; }));
            for (int i = 0; i < 4; i++)
            {
                IGameSave.ModeType m = (IGameSave.ModeType)i;
                Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchRaisedLocate[i], Core.Graph.GraphConfig.TouchRaisedSize[i],
                    () =>
                    {
                        if (Core.Save.Mode == m)
                        {
                            Main.DisplayRaised();
                            return true;
                        }
                        else
                            return false;

                    }, true));
            }
            Task.Run(Main.Load_24_WaitAndStart);

            Main.ToolBar.MenuInteract.Items.Clear();
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Interact, "摸头".Translate(), DisplayTouchHead);
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Interact, "摸身体".Translate(), DisplayTouchBody);
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Interact, "捏脸".Translate(), () => DisplayPinch());

            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "退出访客表".Translate(), wmp.Close);
            var menuItem = new MenuItem()
            {
                Header = "置于顶层".Translate(),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsCheckable = true,
                IsChecked = Topmost
            };
            menuItem.Click += delegate
            {
                Topmost = !Topmost;
            };
            Main.ToolBar.MenuSetting.Items.Add(menuItem);

            Main.ToolBar.tfun.Visibility = Visibility.Collapsed;

            Main.EventTimer.AutoReset = false;
            Main.EventTimer.Enabled = false;

            //清空资源
            Main.Resources = Application.Current.Resources;
            Main.MsgBar.This.Resources = Application.Current.Resources;
            Main.ToolBar.Resources = Application.Current.Resources;

            HideForDesign.Children.Remove(MPTalkBox);
            Main.ToolBar.MainGrid.Children.Add(MPTalkBox);

            if (lb.GetMemberData(friend, "notouch") == "true")
            {
                NoTouchTrue();
            }
            else
            {
                NoTouchFalse();
            }

            cbTalk.Items.Add("私聊".Translate());
            cbTalk.Items.Add("公聊".Translate());
            cbTalk.Items.Add("大家".Translate());
            cbTalk.SelectedIndex = 1;

            LoadingText.Content = "正在加载游戏\n该步骤可能会耗时比较长\n请耐心等待".Translate();

            Foods.ForEach(item =>
            {
                item.ImageSource = ImageSources.FindImage("food_" + (item.Image ?? item.Name), "food");
                item.Star = mw.Set["betterbuy"]["star"].GetInfos().Contains(Name);
            });

            Main.PlayVoiceVolume = mw.Set.VoiceVolume;

            DisplayGrid.Child = Main;

            //Main.SetMoveMode(mf.Set.AllowMove, mf.Set.SmartMove, mf.Set.SmartMoveInterval * 1000);
            //Main.SetLogicInterval(1500);
            if (mw.Set.MessageBarOutside)
                Main.MsgBar.SetPlaceOUT();

            //Main.WorkCheck = mf.WorkCheck;

            //添加捏脸动画(若有)
            if (Core.Graph.GraphConfig.Data.ContainsLine("pinch"))
            {
                var pin = Core.Graph.GraphConfig.Data["pinch"];
                Main.Core.TouchEvent.Insert(0, new TouchArea(
                    new Point(pin[(gdbe)"px"], pin[(gdbe)"py"]), new Size(pin[(gdbe)"sw"], pin[(gdbe)"sh"])
                    , DisplayPinch, true));
            }
            Title = "{0}的{1}".Translate(friend.Name, Core.Save.Name);
            LoadingText.Content = "{0}的{1}".Translate(friend.Name, Core.Save.Name);
            LoadingText.Background = Function.ResourcesBrush(Function.BrushType.DARKPrimaryTransA);
            LoadingText.VerticalAlignment = VerticalAlignment.Top;

            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "吃饭".Translate(), () =>
            {
                ShowBetterBuy(Food.FoodType.Meal);
            });
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "喝水".Translate(), () =>
            {
                ShowBetterBuy(Food.FoodType.Drink);
            });
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "收藏".Translate(), () =>
            {
                ShowBetterBuy(Food.FoodType.Star);
            });
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "药品".Translate(), () =>
            {
                ShowBetterBuy(Food.FoodType.Drug);
            });
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "礼品".Translate(), () =>
            {
                ShowBetterBuy(Food.FoodType.Gift);
            });

            Loaded = true;
        }));
    }


    public winMPBetterBuy winMPBetterBuy;

    public void ShowBetterBuy(Food.FoodType foodType)
    {
        if (winMPBetterBuy != null)
            winMPBetterBuy.Show(foodType);
        else
        {
            winMPBetterBuy = new winMPBetterBuy(this);
            winMPBetterBuy.Show(foodType);
        }
    }

    public new bool Loaded = false;

    public void DisplayTouchHead()
    {
        if (NOTouch) return;
        Main.DisplayTouchHead();
    }
    public void DisplayTouchBody()
    {
        if (NOTouch) return;
        Main.DisplayTouchBody();
    }

    /// <summary>
    /// 显示捏脸情况
    /// </summary>
    public bool DisplayPinch()
    {
        if (NOTouch) return false;
        if (Core.Graph.FindGraphs("pinch", AnimatType.A_Start, Core.Save.Mode) == null)
        {
            return false;
        }
        Main.CountNomal = 0;
        Main.LabelDisplayShow("{0}在捏{1}的脸".Translate(SteamClient.Name, Core.Save.Name), 3000);
        if (Main.DisplayType.Name == "pinch")
        {
            if (Main.DisplayType.Animat == AnimatType.A_Start)
                return false;
            else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Name == "pinch" && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return true;
                }
                else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Name == "pinch" && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return true;
                }
        }
        Main.Display("pinch", AnimatType.A_Start, () =>
           Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop));
        return true;
    }
    private void DisplayPinch_loop()
    {
        Main.LabelDisplayShow("{0}在捏{1}的脸".Translate(SteamClient.Name, Core.Save.Name), 3000);
        var msg = new MPMessage() { Type = (int)MSGType.Interact, To = friend.Id };
        msg.SetContent(Interact.TouchPinch);
        wmp.SendMessageALL(msg);
        if (Main.isPress && Main.DisplayType.Name == "pinch" && Main.DisplayType.Animat == AnimatType.B_Loop)
        {
            Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop);
        }
        else
        {
            Main.DisplayCEndtoNomal("pinch");
        }
    }
    /// <summary>
    /// 显示摸头情况 (无任何计算和传导)
    /// </summary>
    public void DisplayNOCALTouchHead()
    {
        if (Main.DisplayType.Type == GraphType.Touch_Head)
        {
            if (Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        Main.Display(GraphType.Touch_Head, AnimatType.A_Start, (graphname) =>
           Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
           Main.DisplayCEndtoNomal(graphname))));
    }
    /// <summary>
    /// 显示摸身体情况 (无任何计算和传导)
    /// </summary>
    public void DisplayNOCALTouchBody()
    {
        if (Main.DisplayType.Type == GraphType.Touch_Body)
        {
            if (Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        Main.Display(GraphType.Touch_Body, AnimatType.A_Start, (graphname) =>
         Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         Main.DisplayCEndtoNomal(graphname))));
    }
    /// <summary>
    /// 显示摸身体情况 (无任何计算和传导)
    /// </summary>
    public void DisplayNOCALTouchPinch()
    {
        if (Main.DisplayType.Name == "pinch")
        {
            if (Main.DisplayType.Animat == AnimatType.A_Start)
                return;
            else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return;
                }
                else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.SetContinue();
                    return;
                }
        }
        Main.Display("pinch", AnimatType.A_Start, (graphname) =>
         Main.Display(graphname, AnimatType.B_Loop, (graphname) =>
         Main.Display(graphname, AnimatType.B_Loop, (graphname) => Main.DisplayCEndtoNomal(graphname))));
    }

    /// <summary>
    /// 收到被互动通知
    /// </summary>
    public void ActiveInteract(string byname, Interact interact)
    {
        if (!Loaded || NOTouch)
        {
            return;
        }
        if (InConvenience())
        {//忙碌时候只显示消息
            switch (interact)
            {
                case Interact.TouchHead:
                case Interact.TouchBody:
                    Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, Core.Save.Name), 3000);
                    break;
                case Interact.TouchPinch:
                    Main.LabelDisplayShow("{0}在捏{1}的脸".Translate(byname, Core.Save.Name));
                    break;
            }
            return;
        }
        switch (interact)
        {
            case Interact.TouchHead:
                DisplayNOCALTouchHead();
                Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, Core.Save.Name), 3000);
                break;
            case Interact.TouchBody:
                DisplayNOCALTouchBody();
                Main.LabelDisplayShow("{0}在摸{1}的头".Translate(byname, Core.Save.Name), 3000);
                break;
            case Interact.TouchPinch:
                DisplayNOCALTouchPinch();
                Main.LabelDisplayShow("{0}在捏{1}的脸".Translate(byname, Core.Save.Name), 3000);
                break;
        }
    }


    /// <summary>
    /// 播放关闭动画并关闭,如果10秒后还未关闭则强制关闭
    /// </summary>
    public void Quit()
    {
        try
        {
            Main.Display(GraphType.Shutdown, AnimatType.Single, () => Dispatcher.Invoke(Close));
            Task.Run(() =>
            {
                Thread.Sleep(3000);
                if (Loaded)
                    Dispatcher.Invoke(Close);
            });
        }
        catch
        {
            Close();
        }
    }

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
        if (!Loaded)
        {
            return false;
        }
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
    private void WindowX_Closed(object sender, EventArgs e)
    {
        if (Core != null && Core.Graph != null)
        {
            foreach (var igs in Core.Graph.GraphsList.Values)
            {
                foreach (var ig2 in igs.Values)
                {
                    foreach (var ig3 in ig2)
                    {
                        ig3.Stop(true);
                    }
                }
            }
        }
        SteamMatchmaking.OnLobbyMemberDataChanged -= SteamMatchmaking_OnLobbyMemberDataChanged;
        winMPBetterBuy?.Close();
        Main?.Dispose();
        mw.Windows.Remove(this);
    }
    private void tbTalk_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
        {
            Send_Click(sender, e);
            e.Handled = true;
            Main.ToolBar.Visibility = Visibility.Collapsed;
            return;
        }
        if (tbTalk.Text.Length > 0)
        {
            Main.ToolBar.CloseTimer.Stop();
        }
        else
        {
            Main.ToolBar.CloseTimer.Start();
        }
    }
    private void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(tbTalk.Text))
        {
            return;
        }
        var cont = tbTalk.Text;
        tbTalk.Text = "";
        Main.ToolBar.Visibility = Visibility.Collapsed;
        int talktype = cbTalk.SelectedIndex;

        Task.Run(() =>
        {
            MPMessage msg = new MPMessage();
            msg.Type = (int)MSGType.Chat;
            msg.SetContent(new Chat() { Content = cont, ChatType = (Chat.Type)talktype, SendName = SteamClient.Name, ToName = friend.Name });
            msg.To = SteamClient.SteamId;

            switch (talktype)
            {
                case 0:
                    wmp.SendMessage(friend.Id, msg);
                    mw.Main.Say("{0} 悄悄地对你说: {1}".Translate(SteamClient.Name, cont));
                    wmp.Log("{0} 悄悄地对你说: {1}".Translate(SteamClient.Name, cont));
                    break;
                case 1:
                    wmp.SendMessageALL(msg);
                    mw.Main.Say("{0} 对 {2} 说: {1}".Translate(SteamClient.Name, cont, friend.Name));
                    wmp.Log("{0} 对 {2} 说: {1}".Translate(SteamClient.Name, cont, friend.Name));
                    break;
                case 2:
                    wmp.SendMessageALL(msg);
                    mw.Main.Say("{0} 对大家说: {1}".Translate(SteamClient.Name, cont));
                    wmp.Log("{0} 对大家说: {1}".Translate(SteamClient.Name, cont));
                    break;
            }
        });
    }

    private void cbTalk_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (cbTalk.SelectedIndex)
        {
            case 0:
                Panuon.WPF.UI.TextBoxHelper.SetWatermark(tbTalk, "和{0}悄悄说".Translate(friend.Name));
                break;
            case 1:
                Panuon.WPF.UI.TextBoxHelper.SetWatermark(tbTalk, "和{0}说".Translate(friend.Name));
                break;
            case 2:
                Panuon.WPF.UI.TextBoxHelper.SetWatermark(tbTalk, "和大家说");
                break;
        }
    }

    public bool InConvenience() => IMPFriend.InConvenience(Main);

    public void ReSetLocal()
    {
        Main?.CleanState();
        Main?.DisplayToNomal();
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
    }
    private System.Windows.Controls.Image hashcheckimg;

}
