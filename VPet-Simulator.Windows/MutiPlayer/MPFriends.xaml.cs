using LinePutScript.Dictionary;
using LinePutScript;
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
using LinePutScript.Localization.WPF;
using System.Threading;
using VPet_Simulator.Windows.Interface;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphHelper;
using System.Drawing;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;
/// <summary>
/// MPFriends.xaml 的交互逻辑
/// </summary>
public partial class MPFriends : WindowX
{
    public Lobby lb;
    MainWindow mw;
    public Friend friend;
    public winMutiPlayer wmp;
    public GameCore Core { get; set; } = new GameCore();
    public List<Food> Foods { get; } = new List<Food>();
    public ImageResources ImageSources { get; } = new ImageResources();
    public List<PetLoader> Pets { get; set; } = new List<PetLoader>();
    public ILine OnMod { get; set; }

    public string SetPetGraph;
    public bool IsOnMod(string ModName)
    {
        if (CoreMOD.OnModDefList.Contains(ModName))
            return true;
        return OnMod.Find(ModName.ToLower()) != null;
    }

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

            //MGrid.Height = 500 * mw.Set.ZoomLevel;
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


        ImageSources.AddRange(mw.ImageSources);


        //加载所有MOD
        List<DirectoryInfo> Path = new List<DirectoryInfo>();
        Path.AddRange(new DirectoryInfo(mw.ModPath).EnumerateDirectories());

        var workshop = mw.Set["workshop"];
        foreach (Sub ws in workshop)
        {
            Path.Add(new DirectoryInfo(ws.Name));
        }

        Task.Run(async () =>
        {
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

            await GameLoad(Path);
        });

    }
    public List<MPMOD> MPMODs = new List<MPMOD>();
    public Main Main { get; set; }
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

            Core.Graph = petloader.Graph(mw.Set.Resolution);
            Main = new Main(Core);

            //清空资源
            Main.Resources = Application.Current.Resources;
            Main.MsgBar.This.Resources = Application.Current.Resources;
            Main.ToolBar.Resources = Application.Current.Resources;
            Main.ToolBar.LoadClean();

            LoadingText.Content = "正在加载游戏\n该步骤可能会耗时比较长\n请耐心等待".Translate();

            Main.PlayVoiceVolume = mw.Set.VoiceVolume;

            DisplayGrid.Child = Main;

            Main.SetMoveMode(mw.Set.AllowMove, mw.Set.SmartMove, mw.Set.SmartMoveInterval * 1000);
            Main.SetLogicInterval(1500);
            if (mw.Set.MessageBarOutside)
                Main.MsgBar.SetPlaceOUT();

            Main.WorkCheck = mw.WorkCheck;


            //添加捏脸动画(若有)
            if (Core.Graph.GraphConfig.Data.ContainsLine("pinch"))
            {
                var pin = Core.Graph.GraphConfig.Data["pinch"];
                Main.Core.TouchEvent.Insert(0, new TouchArea(
                    new Point(pin[(gdbe)"px"], pin[(gdbe)"py"]), new Size(pin[(gdbe)"sw"], pin[(gdbe)"sh"])
                    , DisplayPinch, true));
            }
            SteamMatchmaking.OnLobbyMemberDataChanged += SteamMatchmaking_OnLobbyMemberDataChanged;
            SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
            Loaded = true;

            LoadingText.Content = "{0}的{1}".Translate(friend.Name, Core.Save.Name);
            LoadingText.Background = Function.ResourcesBrush(Function.BrushType.DARKPrimaryTransA);
            LoadingText.VerticalAlignment = VerticalAlignment.Top;
        }));
    }
    public new bool Loaded = false;
    private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id && friend.Id == this.friend.Id)
            Quit();
    }

    private void SteamMatchmaking_OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        if (lobby.Id == lb.Id && friend.Id == this.friend.Id)
        {
            Core.Save = GameSave_VPet.Load(new Line(lb.GetMemberData(friend, "save")));
        }
    }

    /// <summary>
    /// 显示捏脸情况
    /// </summary>
    public bool DisplayPinch()
    {
        if (Core.Graph.FindGraphs("pinch", AnimatType.A_Start, Core.Save.Mode) == null)
        {
            return false;
        }
        Main.CountNomal = 0;

        if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < 100)
        {
            Core.Save.StrengthChange(-2);
            Core.Save.FeelingChange(1);
            Core.Save.Mode = Core.Save.CalMode();
            Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
        }
        if (Main.DisplayType.Name == "pinch")
        {
            if (Main.DisplayType.Animat == AnimatType.A_Start)
                return false;
            else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Name == "pinch" && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.IsContinue = true;
                    return true;
                }
                else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Name == "pinch" && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig2.IsContinue = true;
                    return true;
                }
        }
        Main.Display("pinch", AnimatType.A_Start, () =>
           Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop));
        return true;
    }
    private void DisplayPinch_loop()
    {
        if (Main.isPress && Main.DisplayType.Name == "pinch" && Main.DisplayType.Animat == AnimatType.B_Loop)
        {
            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < 100)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                Core.Save.Mode = Core.Save.CalMode();
                Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
            }
            Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop);
        }
        else
        {
            Main.DisplayCEndtoNomal("pinch");
        }
    }

    private void WindowX_Closed(object sender, EventArgs e)
    {
        wmp.MPFriends.Remove(this);
        Loaded = false;
    }
    /// <summary>
    /// 播放关闭动画并关闭,如果10秒后还未关闭则强制关闭
    /// </summary>
    public void Quit()
    {
        Main.Display(GraphType.Shutdown, AnimatType.Single, () => Dispatcher.Invoke(Close));
        Task.Run(() =>
        {
            Thread.Sleep(5000);
            if (Loaded)
                Dispatcher.Invoke(Close);
        });
    }
}
