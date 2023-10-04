using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using Application = System.Windows.Application;
using System.Timers;
using LinePutScript;
using static VPet_Simulator.Core.GraphCore;
using Panuon.WPF.UI;
using VPet_Simulator.Windows.Interface;
using System.Windows.Controls;
using System.Linq;
using LinePutScript.Localization.WPF;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using static VPet_Simulator.Windows.PerformanceDesktopTransparentWindow;
using Line = LinePutScript.Line;
using static VPet_Simulator.Core.GraphInfo;
using System.Globalization;
using static VPet_Simulator.Windows.Interface.ExtensionFunction;
using System.Web.UI.WebControls;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowX
    {
        private NotifyIcon notifyIcon;
        public PetHelper petHelper;
        public System.Timers.Timer AutoSaveTimer = new System.Timers.Timer();
        public MainWindow()
        {
#if X64
            PNGAnimation.MaxLoadNumber = 50;
#else
            PNGAnimation.MaxLoadNumber = 20;
#endif
            ExtensionValue.BaseDirectory = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;

            LocalizeCore.StoreTranslation = true;
            CultureInfo.CurrentCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
            CultureInfo.CurrentCulture.NumberFormat = new CultureInfo("en-US").NumberFormat;
            //判断是不是Steam用户,因为本软件会发布到Steam
            //在 https://store.steampowered.com/app/1920960/VPet
            try
            {
#if DEMO
                SteamClient.Init(2293870, true);
#else
                SteamClient.Init(1920960, true);
#endif
                SteamClient.RunCallbacks();
                IsSteamUser = SteamClient.IsValid;
                ////同时看看有没有买dlc,如果有就添加dlc按钮
                //if (Steamworks.SteamApps.IsDlcInstalled(1386450))
                //  dlcToolStripMenuItem.Visible = true;
            }
            catch
            {
                IsSteamUser = false;
            }
            try
            {
                //加载游戏设置
                if (new FileInfo(ExtensionValue.BaseDirectory + @"\Setting.lps").Exists)
                {
                    Set = new Setting(File.ReadAllText(ExtensionValue.BaseDirectory + @"\Setting.lps"));
                }
                else
                    Set = new Setting("Setting#VPET:|\n");

                var visualTree = new FrameworkElementFactory(typeof(Border));
                visualTree.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Window.BackgroundProperty));
                var childVisualTree = new FrameworkElementFactory(typeof(ContentPresenter));
                childVisualTree.SetValue(UIElement.ClipToBoundsProperty, true);
                visualTree.AppendChild(childVisualTree);

                Template = new ControlTemplate
                {
                    TargetType = typeof(Window),
                    VisualTree = visualTree,
                };

                _dwmEnabled = Win32.Dwmapi.DwmIsCompositionEnabled();
                _hwnd = new WindowInteropHelper(this).EnsureHandle();

                //if (File.Exists(ExtensionValue.BaseDirectory + @"\ChatGPTSetting.json"))
                //    CGPTClient = ChatGPTClient.Load(File.ReadAllText(ExtensionValue.BaseDirectory + @"\ChatGPTSetting.json"));
                //this.Width = 400 * ZoomSlider.Value;
                //this.Height = 450 * ZoomSlider.Value;
                InitializeComponent();

                this.Height = 500 * Set.ZoomLevel;
                this.Width = 500 * Set.ZoomLevel;

                double L = 0, T = 0;
                if (Set.StartRecordLast)
                {
                    var point = Set.StartRecordLastPoint;
                    if (point.X != 0 || point.Y != 0)
                    {
                        L = point.X;
                        T = point.Y;
                    }
                }
                else
                {
                    var point = Set.StartRecordPoint;
                    L = point.X; T = point.Y;
                }

                Left = L;
                Top = T;

                // control position inside bounds
                Core.Controller = new MWController(this);
                double dist;
                if ((dist = Core.Controller.GetWindowsDistanceLeft()) < 0) Left -= dist;
                if ((dist = Core.Controller.GetWindowsDistanceRight()) < 0) Left += dist;
                if ((dist = Core.Controller.GetWindowsDistanceUp()) < 0) Top -= dist;
                if ((dist = Core.Controller.GetWindowsDistanceDown()) < 0) Top += dist;

                if (Set.TopMost)
                {
                    Topmost = true;
                }
                if (Set.HitThrough)
                {
                    if (!Set["v"][(gbol)"HitThrough"])
                    {
                        Set["v"][(gbol)"HitThrough"] = true;
                        Set.HitThrough = false;
                    }
                    else
                        SetTransparentHitThrough();
                }


                //不存在就关掉
                var modpath = new DirectoryInfo(ModPath + @"\0000_core\pet\vup");
                if (!modpath.Exists)
                {
                    MessageBoxX.Show("缺少模组Core,无法启动桌宠\nMissing module Core, can't start up", "启动错误 boot error", Panuon.WPF.UI.MessageBoxIcon.Error);
                    Environment.Exit(0);
                    return;
                }
                Closed += ForceClose;

                //更新存档系统
                if (Directory.Exists(ExtensionValue.BaseDirectory + @"\BackUP"))
                {
                    if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
                        Directory.Move(ExtensionValue.BaseDirectory + @"\BackUP", ExtensionValue.BaseDirectory + @"\Saves");
                    else
                    {
                        foreach (var file in new DirectoryInfo(ExtensionValue.BaseDirectory + @"\BackUP").GetFiles())
                            if (!File.Exists(ExtensionValue.BaseDirectory + @"\Saves\" + file.Name))
                                file.MoveTo(ExtensionValue.BaseDirectory + @"\Saves\" + file.Name);
                            else
                                file.Delete();
                        Directory.Delete(ExtensionValue.BaseDirectory + @"\BackUP", true);
                    }
                }
                //if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
                //{
                //    Directory.CreateDirectory(ExtensionValue.BaseDirectory + @"\Saves");
                //}

                Task.Run(GameLoad);
            }
            catch (Exception e)
            {
                string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
              "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
              "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
              + e.ToString();
                MessageBoxX.Show(errstr, "游戏致命性错误".Translate() + ' ' + "启动错误".Translate(), Panuon.WPF.UI.MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }
        public new void Close()
        {
            if (Main == null)
            {
                base.Close();
            }
            else
            {
                Main.Display(GraphType.Shutdown, AnimatType.Single, () => Dispatcher.Invoke(base.Close));
            }
        }
        public void Restart()
        {
            this.Closed -= Window_Closed;
            this.Closed += Restart_Closed;
            base.Close();
        }
        private void ForceClose(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(10000);
                while (true)
                    Environment.Exit(0);
            });
        }

        private void Restart_Closed(object sender, EventArgs e)
        {
            CloseConfirm = false;
            try
            {
                //关闭所有插件
                foreach (MainPlugin mp in Plugins)
                    mp.EndGame();
            }
            catch { }
            Save();
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Exit();
        }
        private void Exit()
        {
            try
            {
                if (IsSteamUser)
                    SteamClient.Shutdown();//关掉和Steam的连线
                if (Core != null)
                {
                    foreach (var igs in Core.Graph.GraphsList.Values)
                    {
                        foreach (var ig2 in igs.Values)
                        {
                            foreach (var ig3 in ig2)
                            {
                                ig3.Stop();
                            }
                        }
                    }
                }
                if (Main != null)
                {
                    Main.Dispose();
                }
                if (winSetting != null)
                {
                    winSetting.Close();
                }
                AutoSaveTimer?.Stop();
                MusicTimer?.Stop();
                petHelper?.Close();
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                notifyIcon?.Dispose();
            }
            finally
            {
                Environment.Exit(0);
            }
            while (true)
                Environment.Exit(0);
        }


        public long lastclicktime { get; set; }

        public void LoadLatestSave(string petname)
        {
            if (Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
            {
                var ds = new List<string>(Directory.GetFiles(ExtensionValue.BaseDirectory + @"\Saves", "*.lps")).FindAll(x => x.Contains('_')).OrderBy(x =>
                {
                    if (int.TryParse(x.Split('_')[1].Split('.')[0], out int i))
                        return i;
                    return 0;
                }).ToList();
                if(ds.Count == 0)
                {
                    GameSavesData = new GameSave_v2(petname.Translate());
                    Core.Save = GameSavesData.GameSave;
                    return;
                }
                int.TryParse(ds.Last().Split('_')[1].Split('.')[0], out int lastid);
                if (Set.SaveTimes < lastid)
                {
                    Set.SaveTimes = lastid;
                }
                for (int i = ds.Count - 1; i >= 0; i--)
                {
                    var latestsave = ds[i];
                    if (latestsave != null)
                    {
                        try
                        {
                            if (GameLoad(new LPS(File.ReadAllText(latestsave))))
                                return;
                            //MessageBoxX.Show("存档损毁,无法加载该存档\n可能是上次储存出错或Steam云同步导致的\n请在设置中加载备份还原存档", "存档损毁".Translate());
                        }
                        catch // (Exception ex)
                        {
                            //MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
                        }
                    }
                }

            }
            else
            {
                GameSavesData = new GameSave_v2(petname.Translate());
                Core.Save = GameSavesData.GameSave;
            }
        }
        public async void GameLoad()
        {
            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());
            if (IsSteamUser)//如果是steam用户,尝试加载workshop
            {
                var workshop = Set["workshop"];
                workshop.Clear();
                await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading Steam Workshop"));
                int i = 1;
                while (true)
                {
                    var page = Steamworks.Ugc.Query.ItemsReadyToUse.GetPageAsync(i++).Result;
                    if (page.HasValue && page.Value.ResultCount != 0)
                    {
                        foreach (Steamworks.Ugc.Item entry in page.Value.Entries)
                        {
                            if (entry.Directory != null)
                            {
                                Path.Add(new DirectoryInfo(entry.Directory));
                                workshop.Add(new Sub(entry.Directory, ""));
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                var workshop = Set["workshop"];
                foreach (Sub ws in workshop)
                {
                    Path.Add(new DirectoryInfo(ws.Name));
                }
            }
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading MOD"));
            //加载mod
            foreach (DirectoryInfo di in Path)
            {
                if (!File.Exists(di.FullName + @"\info.lps"))
                    continue;
                await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = $"Loading MOD: {di.Name}"));
                CoreMODs.Add(new CoreMOD(di, this));
            }

            CoreMOD.NowLoading = null;
            //判断是否需要清空缓存
            if (Set.LastCacheDate < CoreMODs.Max(x => x.CacheDate) || Set["CGPT"][(gint)"v"] <= 1)
            {//需要清理缓存
                Set["CGPT"][(gint)"v"] = 2;
                Set.LastCacheDate = DateTime.Now;
                if (Directory.Exists(CachePath))
                {
                    Directory.Delete(CachePath, true);
                    Directory.CreateDirectory(CachePath);
                }
            }

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading Translate"));
            //加载语言
            LocalizeCore.StoreTranslation = true;
            if (Set.Language == "null")
            {
                LocalizeCore.LoadDefaultCulture();
                if (LocalizeCore.CurrentCulture == "null")
                    LocalizeCore.CurrentCulture = "en";
                Set.Language = LocalizeCore.CurrentCulture;
            }
            else
                LocalizeCore.LoadCulture(Set.Language);

            //旧版本设置兼容
            var cgpte = Set.FindLine("CGPT");
            if (cgpte != null)
            {
                var cgpteb = cgpte.Find("enable");
                if (cgpteb != null)
                {
                    if (Set["CGPT"][(gbol)"enable"])
                    {
                        Set["CGPT"][(gstr)"type"] = "API";
                    }
                    else
                    {
                        Set["CGPT"][(gstr)"type"] = "LB";
                    }
                    Set["CGPT"].Remove(cgpteb);
                }
            }
            else if (Set["CGPT"][(gstr)"type"] == "OFF")
            {//为老玩家开启选项聊天功能
                Set["CGPT"][(gstr)"type"] = "LB";
            }
            else//新玩家,默认设置为
                Set["CGPT"][(gstr)"type"] = "LB";


            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏MOD".Translate()));

            //当前桌宠动画
            var petloader = Pets.Find(x => x.Name == Set.PetGraph);
            petloader ??= Pets[0];

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏存档".Translate()));
            //加载存档
            if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.lps")) //有老的旧存档,优先旧存档
                try
                {
                    if (!GameLoad(new LpsDocument(File.ReadAllText(ExtensionValue.BaseDirectory + @"\Save.lps"))))
                    {
                        //如果加载存档失败了,试试加载备份,如果没备份,就新建一个
                        LoadLatestSave(petloader.PetName);
                    }

                }
                catch (Exception ex)
                {
                    MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
                    //如果加载存档失败了,试试加载备份,如果没备份,就新建一个
                    LoadLatestSave(petloader.PetName);
                }
            else
                //如果加载存档失败了,试试加载备份,如果没备份,就新建一个
                LoadLatestSave(petloader.PetName);

            //加载数据合理化:食物
            if (!Set["gameconfig"].GetBool("noAutoCal"))
            {
                foreach (Food f in Foods)
                {
                    if (f.IsOverLoad())
                    {
                        f.Price = Math.Max((int)f.RealPrice, 1);
                        f.isoverload = false;
                    }
                }
                //var food = new Food();
                foreach (var selet in SelectTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    //food.Exp += selet.Exp;
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                    //food.Feeling += selet.Feeling;
                    selet.Health = Math.Max(Math.Min(selet.Feeling, 100), -100);
                    //food.Health += selet.Health;
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    //food.Likability += selet.Likability;
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    //food.Price -= selet.Money;
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    //food.Strength += selet.Strength;
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    //food.StrengthDrink += selet.StrengthDrink;
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                    //food.StrengthFood += selet.StrengthFood;
                }
                //if (food.IsOverLoad())
                //{
                //    MessageBox.Show(food.RealPrice.ToString());
                //}
                foreach (var selet in ClickTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    //food.Exp += selet.Exp;
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                    //food.Feeling += selet.Feeling;
                    selet.Health = Math.Max(Math.Min(selet.Feeling, 100), -100);
                    //food.Health += selet.Health;
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    //food.Likability += selet.Likability;
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    //food.Price -= selet.Money;
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    //food.Strength += selet.Strength;
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    //food.StrengthDrink += selet.StrengthDrink;
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                    //food.StrengthFood += selet.StrengthFood;
                }
            }

            //桌宠生日:第一次启动日期
            if (GameSavesData.Data.FindLine("birthday") == null)
            {
                var sf = new FileInfo(ExtensionValue.BaseDirectory + @"\Setting.lps");
                if (sf.Exists)
                {
                    GameSavesData[(gdat)"birthday"] = sf.CreationTime.Date;
                }
                else
                    GameSavesData[(gdat)"birthday"] = DateTime.Now.Date;
            }

            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;

            if (GameSavesData.Statistics[(gdbe)"stat_bb_food"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drink"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drug"] < 0
                || GameSavesData.Statistics[(gdbe)"stat_bb_snack"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_functional"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_meal"] < 0
                || GameSavesData.Statistics[(gdbe)"stat_bb_gift"] < 0)
            {
                HashCheck = false;
            }

            if (Set.AutoSaveInterval > 0)
            {
                AutoSaveTimer.Interval = Set.AutoSaveInterval * 60000;
                AutoSaveTimer.Start();
            }
            ClickTexts.Add(new ClickText("你知道吗? 鼠标右键可以打开菜单栏"));
            ClickTexts.Add(new ClickText("你知道吗? 你可以在设置里面修改游戏的缩放比例"));
            ClickTexts.Add(new ClickText("想要宠物不乱动? 设置里可以设置智能移动或者关闭移动"));
            ClickTexts.Add(new ClickText("有建议/游玩反馈? 来 菜单-系统-反馈中心 反馈吧"));
            ClickTexts.Add(new ClickText("长按脑袋拖动桌宠到你喜欢的任意位置"));
            //"如果你觉得目前功能太少,那就多挂会机. 宠物会自己动的".Translate(),
            //"你知道吗? 你可以在设置里面修改游戏的缩放比例".Translate(),
            //"你现在乱点说话是说话系统的一部分,不过还没做,在做了在做了ing".Translate(),
            //"你添加了虚拟主播模拟器和虚拟桌宠模拟器到愿望单了吗? 快去加吧".Translate(),
            //"这游戏开发这么慢,都怪画师太咕了".Translate(),
            //"欢迎加入 虚拟主播模拟器群 430081239".Translate()

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载Steam内容".Translate()));
            //给正在玩这个游戏的主播/游戏up主做个小功能
            if (IsSteamUser)
            {
                ClickTexts.Add(new ClickText("关注 {0} 谢谢喵")
                {
                    TranslateText = "关注 {0} 谢谢喵".Translate(SteamClient.Name)
                });
                //Steam成就
                GameSavesData.Statistics.StatisticChanged += Statistics_StatisticChanged;
                //Steam通知
                SteamFriends.SetRichPresence("username", Core.Save.Name);
                SteamFriends.SetRichPresence("mode", (Core.Save.Mode.ToString() + "ly").Translate());
                SteamFriends.SetRichPresence("steam_display", "#Status_IDLE");
                SteamFriends.SetRichPresence("idel", "闲逛".Translate());
                if (HashCheck)
                {
                    SteamFriends.SetRichPresence("lv", $" (lv{GameSavesData.GameSave.Level})");
                }
                else
                {
                    SteamFriends.SetRichPresence("lv", " ");
                }
            }
            else
            {
                ClickTexts.Add(new ClickText("关注 {0} 谢谢喵")
                {
                    TranslateText = "关注 {0} 谢谢喵".Translate(Environment.UserName)
                });
            }

            //音乐识别timer加载
            MusicTimer = new System.Timers.Timer(100)
            {
                AutoReset = false
            };
            MusicTimer.Elapsed += MusicTimer_Elapsed;



            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏动画".Translate()));
            await Dispatcher.InvokeAsync(new Action(() =>
            {
                LoadingText.Content = "尝试加载动画和生成缓存".Translate();

                Core.Graph = petloader.Graph(Set.Resolution);
                Main = new Main(Core);
                Main.NoFunctionMOD = Set.CalFunState;

                //加载数据合理化:工作
                if (!Set["gameconfig"].GetBool("noAutoCal"))
                {
                    foreach (var work in Core.Graph.GraphConfig.Works)
                    {
                        if (work.IsOverLoad())
                        {
                            work.MoneyLevel = 0.5;
                            work.MoneyBase = 8;
                            if (work.Type == GraphHelper.Work.WorkType.Work)
                            {
                                work.StrengthDrink = 2.5;
                                work.StrengthFood = 3.5;
                                work.Feeling = 1.5;
                                work.FinishBonus = 0;
                            }
                            else
                            {
                                work.Feeling = 1;
                                work.FinishBonus = 0;
                                work.StrengthDrink = 1;
                                work.StrengthFood = 1;
                            }
                        }
                    }
                }


                LoadingText.Content = "正在加载游戏".Translate();
                var m = new System.Windows.Controls.MenuItem()
                {
                    Header = "MOD管理".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                m.Click += (x, y) =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    winSetting.MainTab.SelectedIndex = 5;
                    winSetting.Show();
                };
                Main.FunctionSpendHandle += lowStrength;
                Main.WorkTimer.E_FinishWork += WorkTimer_E_FinishWork;
                Main.ToolBar.MenuMODConfig.Items.Add(m);
                try
                {
                    //加载游戏创意工坊插件
                    foreach (MainPlugin mp in Plugins)
                        mp.LoadPlugin();
                }
                catch (Exception e)
                {
                    new winReport(this, "由于插件引起的游戏启动错误".Translate() + "\n" + e.ToString()).Show();
                }
                Foods.ForEach(item => item.LoadImageSource(this));
                Main.TimeHandle += Handle_Music;
                if (IsSteamUser)
                    Main.TimeHandle += Handle_Steam;
                Main.TimeHandle += (x) => DiagnosisUPLoad();

                switch (Set["CGPT"][(gstr)"type"])
                {
                    case "DIY":
                        TalkAPIIndex = TalkAPI.FindIndex(x => x.APIName == Set["CGPT"][(gstr)"DIY"]);
                        LoadTalkDIY();
                        break;
                    //case "API":
                    //    TalkBox = new TalkBoxAPI(this);
                    //    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                    //    break;
                    case "LB":
                        //if (IsSteamUser)
                        //{
                        //    TalkBox = new TalkSelect(this);
                        //    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                        //}
                        TalkBox = new TalkSelect(this);
                        Main.ToolBar.MainGrid.Children.Add(TalkBox);
                        break;
                }

                //窗口部件
                winSetting = new winGameSetting(this);
                winBetterBuy = new winBetterBuy(this);

                Main.DefaultClickAction = () =>
                {
                    if (new TimeSpan(DateTime.Now.Ticks - lastclicktime).TotalSeconds > 20)
                    {
                        lastclicktime = DateTime.Now.Ticks;
                        var rt = GetClickText();
                        if (rt != null)
                        {
                            //聊天效果
                            if (rt.Exp != 0)
                            {
                                if (rt.Exp > 0)
                                {
                                    GameSavesData.Statistics[(gint)"stat_say_exp_p"]++;
                                }
                                else
                                    GameSavesData.Statistics[(gint)"stat_say_exp_d"]++;
                            }
                            if (rt.Likability != 0)
                            {
                                if (rt.Likability > 0)
                                    GameSavesData.Statistics[(gint)"stat_say_like_p"]++;
                                else
                                    GameSavesData.Statistics[(gint)"stat_say_like_d"]++;
                            }
                            if (rt.Money != 0)
                            {
                                if (rt.Money > 0)
                                    GameSavesData.Statistics[(gint)"stat_say_money_p"]++;
                                else
                                    GameSavesData.Statistics[(gint)"stat_say_money_d"]++;
                            }
                            Main.Core.Save.EatFood(rt);
                            Main.Core.Save.Money += rt.Money;
                            Main.SayRnd(rt.TranslateText, desc: rt.FoodToDescription());
                        }
                    }
                };
                Main.PlayVoiceVolume = Set.VoiceVolume;
                Main.FunctionSpendHandle += StatisticsCalHandle;
                DisplayGrid.Child = Main;
                Task.Run(async () =>
                {
                    while (Main.IsWorking)
                    {
                        Thread.Sleep(100);
                    }
                    await Dispatcher.InvokeAsync(() => LoadingText.Visibility = Visibility.Collapsed);
                });

                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "退出桌宠".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; Close(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "开发控制台".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winConsole(this).Show(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "操作教程".Translate(), () =>
                {
                    if (LocalizeCore.CurrentCulture == "zh-Hans")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                    else if (LocalizeCore.CurrentCulture == "zh-Hant")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                    else
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "反馈中心".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winReport(this).Show(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "设置面板".Translate(), () =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    winSetting.Show();
                });

                //this.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Res/TopLogo2019.PNG")));

                //Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "喂食测试", () =>
                //    {
                //        Main.ToolBar.Visibility = Visibility.Collapsed;
                //        IRunImage eat = (IRunImage)Core.Graph.FindGraph(GraphType.Eat, GameSave.ModeType.Nomal);
                //        var b = Main.FindDisplayBorder(eat);
                //        eat.Run(b, new BitmapImage(new Uri("pack://application:,,,/Res/汉堡.png")), Main.DisplayToNomal);
                //    }
                //);
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "吃饭".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Meal);
                });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "喝水".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Drink);
                });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "收藏".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Star);
                });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "药品".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Drug);
                });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "礼品".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Gift);
                });
                Main.SetMoveMode(Set.AllowMove, Set.SmartMove, Set.SmartMoveInterval * 1000);
                Main.SetLogicInterval((int)(Set.LogicInterval * 1000));
                if (Set.MessageBarOutside)
                    Main.MsgBar.SetPlaceOUT();

                Main.ToolBar.WorkCheck = WorkCheck;

                //加载图标
                notifyIcon = new NotifyIcon();
                notifyIcon.Text = "虚拟桌宠模拟器".Translate();
                ContextMenu m_menu;

                if (Set.PetHelper)
                    LoadPetHelper();



                m_menu = new ContextMenu();
                m_menu.Popup += (x, y) => { GameSavesData.Statistics[(gint)"stat_menu_pop"]++; };
                m_menu.MenuItems.Add(new MenuItem("鼠标穿透".Translate(), (x, y) => { SetTransparentHitThrough(); }) { });
                m_menu.MenuItems.Add(new MenuItem("操作教程".Translate(), (x, y) =>
                {
                    if (LocalizeCore.CurrentCulture == "zh-Hans")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                    else if (LocalizeCore.CurrentCulture == "zh-Hant")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                    else
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                }));
                m_menu.MenuItems.Add(new MenuItem("重置位置与状态".Translate(), (x, y) =>
                {
                    Main.CleanState();
                    Main.DisplayToNomal();
                    Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                    Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                }));
                m_menu.MenuItems.Add(new MenuItem("反馈中心".Translate(), (x, y) => { new winReport(this).Show(); }));
                m_menu.MenuItems.Add(new MenuItem("开发控制台".Translate(), (x, y) => { new winConsole(this).Show(); }));

                m_menu.MenuItems.Add(new MenuItem("设置面板".Translate(), (x, y) =>
                {
                    winSetting.Show();
                }));
                m_menu.MenuItems.Add(new MenuItem("退出桌宠".Translate(), (x, y) => Close()));

                LoadDIY();

                notifyIcon.ContextMenu = m_menu;

                notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/vpeticon.ico")).Stream);

                notifyIcon.Visible = true;
                notifyIcon.BalloonTipClicked += (a, b) =>
                {
                    winSetting.Show();
                };
                if (Set.StartUPBoot == true && !Set["v"][(gbol)"newverstartup"])
                {//更新到最新版开机启动方式
                    try
                    {
                        winSetting.GenStartUP();
                        Set["v"][(gbol)"newverstartup"] = true;
                    }
                    catch
                    {

                    }
                }


                //成就和统计 
                GameSavesData.Statistics[(gint)"stat_open_times"]++;
                Main.MoveTimer.Elapsed += MoveTimer_Elapsed;
                Main.OnSay += Main_OnSay;
                Main.Event_TouchHead += Main_Event_TouchHead;
                Main.Event_TouchBody += Main_Event_TouchBody;

                HashCheck = HashCheck;

                if (File.Exists(ExtensionValue.BaseDirectory + @"\Tutorial.html") && Set["SingleTips"].GetDateTime("tutorial") <= new DateTime(2023, 6, 20))
                {
                    Set["SingleTips"].SetDateTime("tutorial", DateTime.Now);
                    if (LocalizeCore.CurrentCulture == "zh-Hans")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                    else if (LocalizeCore.CurrentCulture == "zh-Hant")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                    else
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                }
                if (!Set["SingleTips"].GetBool("helloworld"))
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(2000);
                        Set["SingleTips"].SetBool("helloworld", true);
                        NoticeBox.Show("欢迎使用虚拟桌宠模拟器!\n如果遇到桌宠爬不见了,可以在我这里设置居中或退出桌宠".Translate(),
                           "你好".Translate() + (IsSteamUser ? Steamworks.SteamClient.Name : Environment.UserName));
                        //Thread.Sleep(2000);
                        //Main.SayRnd("欢迎使用虚拟桌宠模拟器\n这是个中期的测试版,若有bug请多多包涵\n欢迎加群虚拟主播模拟器430081239或在菜单栏-管理-反馈中提交bug或建议".Translate());
                    });
                }
#if DEMO
                else
                {
                    notifyIcon.ShowBalloonTip(10, "正式版更新通知".Translate(), //本次更新内容
                        "虚拟桌宠模拟器 现已发布正式版, 赶快前往下载吧!", ToolTipIcon.Info);
                    Process.Start("https://store.steampowered.com/app/1920960/VPet/");
                }
#else
                //else if (Set["SingleTips"].GetDateTime("update") <= new DateTime(2023, 8, 11) && LocalizeCore.CurrentCulture.StartsWith("cn"))
                //{
                //    if (Set["SingleTips"].GetDateTime("update") > new DateTime(2023, 8, 1)) // 上次更新日期时间
                //        notifyIcon.ShowBalloonTip(10, "更新通知 08/11", //本次更新内容
                //        "新增跳舞功能,桌宠会在播放音乐的时候跳舞\n新增不开心大部分系列动画\n更好买支持翻页", ToolTipIcon.Info);
                //    else// 累计更新内容
                //        notifyIcon.ShowBalloonTip(10, "更新通知 08/01",
                //    "更新了新的动画系统\n新增桌宠会在播放音乐的时候跳舞\n新增不开心大部分系列动画\n更好买支持翻页", ToolTipIcon.Info);
                //    Set["SingleTips"].SetDateTime("update", DateTime.Now);
                //}
#endif
                //MOD报错
                foreach (CoreMOD cm in CoreMODs)
                    if (!cm.SuccessLoad)
                        if (cm.Tag.Contains("该模组已损坏"))
                            MessageBoxX.Show("模组 {0} 插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "该模组已损坏".Translate());
                        else if (Set.IsPassMOD(cm.Name))
                            MessageBoxX.Show("模组 {0} 的代码插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "{0} 未加载代码插件".Translate(cm.Name));
                        else if (Set.IsMSGMOD(cm.Name))
                            MessageBoxX.Show("由于 {0} 包含代码插件\n虚拟桌宠模拟器已自动停止加载该插件\n请手动前往设置允许启用该mod 代码插件".Translate(cm.Name), "{0} 未加载代码插件".Translate(cm.Name));

            }));

            ////游戏提示
            //if (Set["SingleTips"][(gint)"open"] == 0 && Set.StartUPBoot == true && Set.StartUPBootSteam == true)
            //{
            //    await Dispatcher.InvokeAsync(new Action(() =>
            //    {
            //        MessageBoxX.Show("检测到您开启了开机启动, 以下是开机启动相关提示信息: (仅显示一次)".Translate() + "\n------\n" +
            //             "游戏开机启动的实现方式是创建快捷方式,不是注册表,更健康,所以游戏卸了也不知道\n如果游戏打不开,可以去这里手动删除游戏开机启动快捷方式:\n%appdata%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\".Translate()
            //          , "关于卸载不掉的问题是因为开启了开机启动".Translate(), Panuon.WPF.UI.MessageBoxIcon.Info);
            //        Set["SingleTips"][(gint)"open"] = 1;
            //    }));
            //}

        }

        private void M_menu_Popup(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void WorkTimer_E_FinishWork(WorkTimer.FinishWorkInfo obj)
        {
            if (obj.work.Type == GraphHelper.Work.WorkType.Work)
            {
                GameSavesData.Statistics[(gint)"stat_single_profit_money"] = (int)obj.count;
            }
            else
            {
                GameSavesData.Statistics[(gint)"stat_single_profit_exp"] = (int)obj.count;
            }
        }

        private void Main_Event_TouchBody()
        {
            GameSavesData.Statistics[(gint)"stat_touch_body"]++;
        }

        private void Main_Event_TouchHead()
        {
            GameSavesData.Statistics[(gint)"stat_touch_head"]++;
        }

        private void Main_OnSay(string obj)
        {
            GameSavesData.Statistics[(gint)"stat_say_times"]++;
        }

        private void MoveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GameSavesData.Statistics[(gint)"stat_move_length"] += (int)(Math.Abs(Main.MoveTimerPoint.X) + Math.Abs(Main.MoveTimerPoint.Y));
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Save();
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            CloseConfirm = false;
            try
            {
                //关闭所有插件
                foreach (MainPlugin mp in Plugins)
                    mp.EndGame();
            }
            catch { }
            Save();
            Exit();
        }

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            //const int WS_EX_TRANSPARENT = 0x20;
            //const int GWL_EXSTYLE = -20;
            //IntPtr hwnd = new WindowInteropHelper(this).Handle;
            //uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            //SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                //想要让窗口透明穿透鼠标和触摸等，需要同时设置 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式，
                //确保窗口始终有 WS_EX_LAYERED 这个样式，并在开启穿透时设置 WS_EX_TRANSPARENT 样式
                //但是WPF窗口在未设置 AllowsTransparency = true 时，会自动去掉 WS_EX_LAYERED 样式（在 HwndTarget 类中)，
                //如果设置了 AllowsTransparency = true 将使用WPF内置的低性能的透明实现，
                //所以这里通过 Hook 的方式，在不使用WPF内置的透明实现的情况下，强行保证这个样式存在。
                if (msg == (int)Win32.WM.STYLECHANGING && (long)wParam == (long)Win32.GetWindowLongFields.GWL_EXSTYLE)
                {
                    var styleStruct = (STYLESTRUCT)Marshal.PtrToStructure(lParam, typeof(STYLESTRUCT));
                    styleStruct.styleNew |= (int)Win32.ExtendedWindowStyles.WS_EX_LAYERED;

                    // Hide windows from alt+tab: https://stackoverflow.com/questions/357076/best-way-to-hide-a-window-from-the-alt-tab-program-switcher
                    if (Set.HideFromTaskControl)
                    {
                        styleStruct.styleNew |= (int)Win32.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                    }

                    Marshal.StructureToPtr(styleStruct, lParam, false);
                    handled = true;
                }
                return IntPtr.Zero;
            });
        }
        private readonly bool _dwmEnabled;
        private readonly IntPtr _hwnd;
        public bool HitThrough { get; private set; } = false;
        public bool MouseHitThrough
        {
            get => HitThrough;
            set
            {
                if (value != HitThrough)
                    SetTransparentHitThrough();
            }
        }
        /// <summary>
        /// 设置点击穿透到后面透明的窗口
        /// </summary>
        public void SetTransparentHitThrough()
        {
            if (_dwmEnabled)
            {
                //const int WS_EX_TRANSPARENT = 0x20;
                //const int GWL_EXSTYLE = -20;
                //IntPtr hwnd = new WindowInteropHelper(this).Handle;
                //uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                //SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                HitThrough = !HitThrough;
                if (HitThrough)
                {
                    Win32.User32.SetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE,
                        (IntPtr)(int)((long)Win32.User32.GetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE) | (long)Win32.ExtendedWindowStyles.WS_EX_TRANSPARENT));
                    petHelper?.SetOpacity(false);
                }
                else
                {
                    Win32.User32.SetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE,
                  (IntPtr)(int)((long)Win32.User32.GetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE) & ~(long)Win32.ExtendedWindowStyles.WS_EX_TRANSPARENT));
                    petHelper?.SetOpacity(true);

                }
            }
        }
        private void WindowX_LocationChanged(object sender, EventArgs e)
        {
            petHelper?.SetLocation();
        }
        //public void DEBUGValue()
        //{
        //  Dispatcher.Invoke(() =>
        //  {
        //    Console.WriteLine("Left:" + mwc.GetWindowsDistanceLeft());
        //    Console.WriteLine("Right:" + mwc.GetWindowsDistanceRight());
        //  });
        //  Thread.Sleep(1000);
        //  DEBUGValue(); 
        //}
        //
    }
}
