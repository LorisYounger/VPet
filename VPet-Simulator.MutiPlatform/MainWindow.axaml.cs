using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization;
using Steamworks;
using Steamworks.Data;
using System;
using static VPet_Simulator.Windows.Interface.Food;
using static VPet_Simulator.Core.MutiPlatform.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠主窗口
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows/MainWindow.xaml.cs: 构造函数里装翻译与区域、认 Steam 用户、
/// 读设置; 窗口打开后扫 MOD 目录、加载语言、进 GameLoad. 顺序与那边一致, 只有"数据放哪"
/// (AppPaths) 与"怎么切线程" (Dispatcher) 是平台差异.
public partial class MainWindow : Window
{
    /// <summary>
    /// 桌宠身体的基准尺寸, 缩放倍率乘在它上面
    /// </summary>
    private const double BodySize = 500;

    /// <summary>
    /// 自动存档计时器
    /// </summary>
    /// 桌宠是长时间挂着的程序, 只在退出时存档的话一旦异常退出就白玩了
    public System.Timers.Timer AutoSaveTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false };

    public MainWindow() : this(string.Empty)
    {
    }

    /// <summary>
    /// 支持多开的启动方式
    /// </summary>
    /// <param name="prefixsave">存档前缀</param>
    /// <param name="basemw">基础窗口</param>
    /// 跨平台: 与 Windows 版同签名; 前缀规范化与 App.MainWindows 登记与那边一致
    public MainWindow(string? prefixsave, MainWindow? basemw = null)
    {
        PrefixSave = prefixsave ?? "";
        if (prefixsave != string.Empty && !PrefixSave.StartsWith("-"))
            PrefixSave = '-' + prefixsave;
        Args = new LPS_D();
        App.MainWindows.Add(this);
        InitializeComponent();

        LocalizeCore.StoreTranslation = true;
        LocalizeCore.TranslateFunc = (str) =>
        {
            var destr = Sub.TextDeReplace(str);
            if (destr != str && LocalizeCore.CurrentLPS != null && LocalizeCore.CurrentLPS.Assemblage.TryGetValue(destr, out ILine? line))
            {
                return line.GetString();
            }
            if (str.Contains('_') && double.TryParse(str.Split('_').Last(), out double d))
                return d.ToString();
            return null;
        };

        CultureInfo.CurrentCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
        CultureInfo.CurrentCulture.NumberFormat = new CultureInfo("en-US").NumberFormat;
        //跨平台: 自动存档跑在线程池线程上, 那边的数字格式也要跟着 (Windows 版的存档计时器在 UI 线程, 不用管)
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;

        //判断是不是Steam用户,因为本软件会发布到Steam
        //在 https://store.steampowered.com/app/1920960/VPet
        try
        {
            SteamClient.Init(1920960, true);
            IsSteamUser = true;
        }
        catch
        {
            IsSteamUser = false;
        }

        if (!string.IsNullOrEmpty(PrefixSave))
            Title = "VPet - " + MultiPetStore.DisplayName(PrefixSave);
        Set = Setting.Load(this, PrefixSave, out var settingWarning);
        if (settingWarning != null)
            Log($"设置: {settingWarning}");

        if (basemw != null)
        {
            Set["workshop"] = basemw.Set["workshop"];
            Set.Resolution = basemw.Set.Resolution;
        }

        Opened += Window_SourceInitialized;
    }

    /// <summary>
    /// 窗口打开: 扫 MOD、加载语言、进 GameLoad
    /// </summary>
    private async void Window_SourceInitialized(object? sender, EventArgs e)
    {
        //对话框默认挂到主窗口上, 这样它们会跟着桌宠居中而不是飞到屏幕角落.
        //多开时只认第一只, 免得后开的那只把属主抢走
        DialogService.DefaultOwner ??= this;
        ImageResources.Cache.OnError = Log;
        Log($"窗口已打开. 桌宠={(string.IsNullOrEmpty(PrefixSave) ? "默认" : MultiPetStore.DisplayName(PrefixSave))} 运行目录={ExtensionValue.BaseDirectory} MOD目录={ModPath}");
        ApplyWindowSettings();
        if (Set.OpacityMain)
            this.Opacity = Set.Opacity;

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
        MWController = new MWController(this);
        Core.Controller = MWController;
        _ = Task.Run(() =>
        {
            double dist;
            if ((dist = Core.Controller!.GetWindowsDistanceLeft()) < 0)
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(() => Left -= dist);
            }
            if ((dist = Core.Controller!.GetWindowsDistanceRight()) < 0)
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(() => Left += dist);
            }
            if ((dist = Core.Controller!.GetWindowsDistanceUp()) < 0)
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(() => Top -= dist);
            }
            if ((dist = Core.Controller!.GetWindowsDistanceDown()) < 0)
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(() => Top += dist);
            }
        });
        if (Set.TopMost)
        {
            Topmost = true;
        }

        //不存在就关掉
        var modpath = new DirectoryInfo(System.IO.Path.Combine(ModPath, "0000_core", "pet", "vup"));
        if (!modpath.Exists)
        {
            MessageBoxX.Show("缺少模组Core,无法启动桌宠\nMissing module Core, can't start up", "启动错误 boot error", MessageBoxIcon.Error);
            Close();
            return;
        }

        try
        {
            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            if (Directory.Exists(ModPath))
                Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());

            bool NOCancel = true;
            CancellationTokenSource source = new CancellationTokenSource();
            var tsk = Task.Run(async () =>
            {
                if (IsSteamUser)//如果是steam用户,尝试加载workshop
                {
                    //Leaderboard? leaderboard = await SteamUserStats.FindLeaderboardAsync("chatgpt_auth");
                    //leaderboard?.ReplaceScore(Function.Rnd.Next());
                    var workshop = new Line_D("workshop");
                    //跨平台: LoadingText 是 TextBlock, MouseDoubleClick 换成 DoubleTapped
                    await Dispatcher.InvokeAsync(() =>
                    {
                        LoadingText.Text = "Loading Steam Workshop\nDouble Click To Skip";
                        LoadingText.DoubleTapped += (_, _) =>
                        {
                            if (LoadingText.Text == "Loading Steam Workshop\nDouble Click To Skip")
                            {
                                NOCancel = false;
                            }
                        };
                    });
                    int i = 1;
                    while (true)
                    {
                        var page = await Steamworks.Ugc.Query.ItemsReadyToUse.GetPageAsync(i++);
                        if (page.HasValue && page.Value.ResultCount != 0)
                        {
                            foreach (Steamworks.Ugc.Item entry in page.Value.Entries)
                            {
                                if (!NOCancel)
                                {
                                    return;
                                }
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
                    if (workshop.Count != 0)
                        Set["workshop"] = workshop;
                }
                else
                {
                    var workshop = Set["workshop"];
                    foreach (Sub ws in workshop)
                    {
                        Path.Add(new DirectoryInfo(ws.Name));
                    }
                }
            }, source.Token);

            //跨平台: 这里已经在 UI 线程上 (Windows 版是后台 Task), 不能 Thread.Sleep
            while (NOCancel && !tsk.IsCompleted)
            {
                await Task.Delay(500);
            }
            if (!NOCancel)
            {
                source.Cancel();
                var workshop = Set["workshop"];
                foreach (Sub ws in workshop)
                {
                    Path.Add(new DirectoryInfo(ws.Name));
                }
            }



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

            await GameLoad(Path);
            //跨平台: GameLoad 在找不到宠物/动画时会停在加载提示上 (Windows 版没有这条路), 后面的都依赖 Main
            if (Main == null)
                return;
            if (IsSteamUser)
            {
                //COD Check
                if (!Set["v"][(gbol)"CODC"])
                {
                    var di = new DirectoryInfo(ExtensionValue.BaseDirectory).Parent!;
                    if (di.Exists && di.GetDirectories("*Call of Duty*").Length != 0)
                    {
                        Dispatcher.Invoke(() => NoticeBox.Show("检测到游戏库中包含使命召唤,建议不要在运行COD时运行桌宠\n根据社区反馈, COD可能会误报桌宠为作弊软件".Translate(),
                            "Call of Duty Check"));
                    }
                    Set["v"][(gbol)"CODC"] = true;
                }
                Set.SteamID = (long)SteamID;
                Dispatcher.Invoke(() =>
                {
                    var menuItem = new MenuItem()
                    {
                        Header = "访客表".Translate()
                    };
                    Main.ToolBar!.MenuInteract.Items.Add(menuItem);

                    var menuCreate = new MenuItem()
                    {
                        Header = "创建".Translate()
                    };
                    menuCreate.Click += (_, _) =>
                    {
                        if (winMutiPlayer == null)
                        {
                            winMutiPlayer = new winMutiPlayer(this);
                            winMutiPlayer.Show();
                        }
                        else
                        {
                            MessageBoxX.Show("已经有加入了一个访客表,无法再创建更多".Translate());
                            winMutiPlayer.Activate();
                        }
                    };
                    menuItem.Items.Add(menuCreate);

                    var menuJoin = new MenuItem()
                    {
                        Header = "加入".Translate()
                    };
                    menuJoin.Click += (_, _) =>
                    {
                        if (winMutiPlayer == null)
                        {
                            winInputBox.Show(this, "请输入访客表ID/固定ID".Translate(), "加入访客表".Translate(), "1860000", async (id) =>
                            {
                                if (ulong.TryParse(id, NumberStyles.HexNumber, null, out ulong lid))
                                {
                                    winMutiPlayer = new winMutiPlayer(this, lid);
                                    winMutiPlayer.Show();
                                }
                                else if ((id.StartsWith('V') || id.StartsWith('v')) && int.TryParse(id[1..], out int fixedid))
                                {
                                    if (ulong.TryParse(await GetVPetRoom("SteamRoomGetLobbyID", fixID: fixedid), out lid) && lid > 1860000)
                                    {
                                        winMutiPlayer = new winMutiPlayer(this, lid);
                                        winMutiPlayer.Show();
                                    }
                                    else
                                    {
                                        MessageBoxX.Show("未找到该固定ID,请检查输入".Translate());
                                    }
                                }
                            });
                        }
                        else
                        {
                            MessageBoxX.Show("已经有加入了一个访客表,无法再创建更多".Translate());
                            winMutiPlayer.Activate();
                        }
                    };
                    menuItem.Items.Add(menuJoin);

                    int clid = Array.IndexOf(App.Args, "+connect_lobby");
                    if (clid != -1)
                    {
                        if (ulong.TryParse(App.Args[clid + 1], out ulong lid))
                        {
                            winMutiPlayer = new winMutiPlayer(this, lid);
                            winMutiPlayer.Show();
                        }
                    }
                });
                SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
                SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
            }


            //这里写的都是限定第一个MW使用的功能, 如果写共通, 请前往

            //物品初始使用方法
            Item.UseAction.Add("Food", [(imw,Item) =>
              {//食物: 默认直接吃掉
                  if(Item is Food food)
                  {
                      imw.TakeItem(food);
                      imw.TakeItemHandle(food, 1, "item");
                      imw.DisplayFoodAnimation(food.GetGraph(), food.ImageSource);
                      Item.Consume(imw);
                      return true;
                  }
                  return false;
              }]);
            Item.UseAction.Add("Toy", [(imw,Item) =>
              {//玩具: 默认播放玩耍动画
                   var graph = imw.Core.Graph!.FindGraph(Item.Data, AnimatType.A_Start, imw.GameSavesData.GameSave.Mode);
                   imw.ActivityLogs.Add(new ActivityLog("al_take_item", Item.TranslateName));
                  if (graph == null)
                      {
                         graph = imw.Core.Graph!.FindGraph(Item.Data, AnimatType.Single, imw.GameSavesData.GameSave.Mode);
                          if(graph != null)
                            {
                                imw.Main.Display(graph, Main.DisplayToNomal);
                            }
                            else
                            {
                                imw.Main.SayRnd("这个玩具好像不能玩耍呢".Translate());
                            }
                      return true;
                      }

                    imw.Main.Display(Item.Data, AnimatType.A_Start, imw.Main.DisplayBLoopingToNomal(imw.Core.Graph!.GraphConfig.GetDuration(graph.GraphInfo.Name)));
                    return true;
              }]);
            Item.UseAction.Add("Mail", [
            //排在前面的方法优先级更高
            (imw,Item) => {
                  switch (Item.Name)
                  {
                      case "每日礼包": //每日随机礼盒: 打开后获得随机3个物品 每天获得一个
                          var moneylimit = Math.Min(20000, (50 * (imw.GameSavesData.GameSave.LevelMax + 1) + imw.GameSavesData.GameSave.Level +1) * 50);
                          var chosenfood = imw.Foods.FindAll(x=>x.Price > 10 && x.Price < moneylimit);
                          if(chosenfood.Count == 0)
                                return false;
                          imw.ItemsAdd(chosenfood[Function.Rnd.Next(chosenfood.Count)].Clone());
                          imw.ItemsAdd(chosenfood[Function.Rnd.Next(chosenfood.Count)].Clone());
                          imw.ItemsAdd(chosenfood[Function.Rnd.Next(chosenfood.Count)].Clone());
                          Item.Consume(imw);
                          return true;
                  }
                  return false;
              },
               (imw,Item) =>
              {//邮件: 打开后获得物品
                 var lps = new LpsDocument(Item.Data);
                  List<string> itemnames = new List<string>();
                  foreach(var line in lps)
                  {
                      var itm = Item.CreateItem(imw,line);
                      if(itm == null)
                          continue;
                      itm.LoadSource(this);
                      imw.ItemsAdd(itm);
                      itemnames.Add(itm.TranslateName);
                  }
                  if(itemnames.Count != 0)
                  {
                      Main.SayRnd("你打开了{0},获得了物品".Translate(Item.Name) +"\n" + string.Join(',',itemnames));
                  }
                  Item.Consume(this);
                 return true;
              }]);
            Item.UseAction.Add("Tool", [(imw,Item) =>
              {//工具: 每个工具有自己的使用方法
                 switch (Item.Name)
                  {
                      case "指南针":
                           imw.Main.DisplayMove();
                          return true;
                  }
                  return false;
              }]);
            //内置的使用处理器都注册完了, 把插件排队的那些补上
            UnifiedItemRegistry.UseActionsReady();
        }
        catch (Exception ex)
        {
            Log($"加载失败: {ex}");
            string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
          "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
          "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
          + ex.ToString();
            MessageBoxX.Show(errstr, "游戏致命性错误".Translate() + ' ' + "启动错误".Translate(), MessageBoxIcon.Error);
            Close();
        }
    }

    /// <summary>
    /// 加载游戏
    /// </summary>
    /// <param name="Path">MOD地址</param>
    public async Task GameLoad(List<DirectoryInfo> Path)
    {
        MODPath = Path.GroupBy(x => x.FullName).Select(group => group.First()).ToList();
        ShowLoading("Loading MOD");
        //加载mod: 扫描 MOD 目录会读大量小文件, 放后台线程
        await Task.Run(() =>
        {
            foreach (DirectoryInfo di in MODPath)
            {
                if (!File.Exists(System.IO.Path.Combine(di.FullName, "info.lps")))
                    continue;
                ShowLoading($"Loading MOD: {di.Name}");
                CoreMODs.Add(new CoreMOD(di, this));
            }
        });
        CoreMOD.NowLoading = null;
        //跨平台: Windows 版这里按 Set.LastCacheDate 清动画缓存目录; 这边的动画不落盘缓存 (GraphCore 直接解码), 没有这一步
        Log($"扫描到 {CoreMODs.Count} 个 MOD, 宠物 {Pets.Count} 只, 食物 {Foods.Count} 种");
        foreach (var mod in CoreMODs)
            Log($"  MOD [{mod.Name}] 内容={string.Join("/", mod.Tag)}"
                + (mod.ErrorMessage.Length > 0 ? " 问题=" + mod.ErrorMessage : ""));

        ShowLoading("Loading Translate");
        //加载语言 (与 Windows 版同序: 各 MOD 先 AddCulture, 全部扫完再 LoadCulture)
        if (App.LanguageOverride != null && App.UiWalk != null)
            LocalizeCore.LoadCulture(App.LanguageOverride);
        else if (Set.Language == "null")
        {
            LocalizeCore.LoadDefaultCulture();
            if (LocalizeCore.CurrentCulture == "null")
                LocalizeCore.CurrentCulture = "en";
            Set.Language = LocalizeCore.CurrentCulture;
        }
        else
            LocalizeCore.LoadCulture(Set.Language);
        Log($"语言={LocalizeCore.CurrentCulture} 可用={string.Join(",", LocalizeCore.AvailableCultures)}");

        ShowLoading("尝试加载游戏MOD".Translate());

        //旧版本设置兼容
        if (Set.PetGraph == "默认虚拟桌宠")
            Set.PetGraph = "vup";

        //当前桌宠动画
        var petloader = Pets.Find(x => x.Name == Set.PetGraph);
        petloader ??= Pets[0];
        //去除其他语言内容
        var tag = petloader!.Config.Data.GetString("tag", "all")!.Split(',');
        LowDrinkText.RemoveAll(x => !x.FindTag(tag));
        LowFoodText.RemoveAll(x => !x.FindTag(tag));
        ClickTexts.RemoveAll(x => !x.FindTag(tag));
        SelectTexts.RemoveAll(x => !x.FindTag(tag));

        ShowLoading("尝试加载游戏存档".Translate());
        //加载存档
        if (File.Exists(System.IO.Path.Combine(ExtensionValue.BaseDirectory, "Save.lps"))) //有老的旧存档,优先旧存档
            try
            {
                if (!SavesLoad(new LpsDocument(File.ReadAllText(System.IO.Path.Combine(ExtensionValue.BaseDirectory, "Save.lps")))))
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
            foreach (var selet in SelectTexts)
            {
                selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                selet.Feeling = Math.Max(Math.Min(selet.Feeling, 100), -100);
                selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
            }
            foreach (var selet in ClickTexts)
            {
                selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
            }
        }

        //第一次启动日期
        if (GameSavesData.Data.FindLine("birthday") == null)
        {
            var sf = new FileInfo(MultiPetStore.SettingPath(AppPaths.DataRoot, PrefixSave));
            if (sf.Exists)
            {
                GameSavesData[(gdat)"birthday"] = sf.CreationTime.Date;
            }
            else
                GameSavesData[(gdat)"birthday"] = DateTime.Now.Date;
        }

        //补充数据信息
        if (string.IsNullOrEmpty(GameSavesData.GameSave.HostName))
        {
            if (IsSteamUser)
                GameSavesData.GameSave.HostName = SteamClient.Name;
            else
                GameSavesData.GameSave.HostName = Environment.UserName;
        }

        AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
        if (GameSavesData.Statistics![(gdbe)"stat_bb_food"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drink"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drug"] < 0
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
        ClickTexts.Add(new ClickText("这游戏开发这么慢,都怪画师太咕了"));
        ClickTexts.Add(new ClickText("长按脑袋拖动桌宠到你喜欢的任意位置"));

        //给正在玩这个游戏的主播/游戏up主做个小功能
        if (IsSteamUser)
        {
            ClickTexts.Add(new ClickText("关注 {0} 谢谢喵")
            {
                TranslateText = "关注 {0} 谢谢喵".Translate(SteamClient.Name)
            });
            //Steam成就
            GameSavesData.Statistics!.StatisticChanged += Statistics_StatisticChanged;
            //Steam通知
            SteamFriends.SetRichPresence("username", Core.Save!.Name);
            SteamFriends.SetRichPresence("mode", (Core.Save!.Mode.ToString() + "ly").Translate());
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
        MusicTimer = new System.Timers.Timer(200)
        {
            AutoReset = false
        };
        MusicTimer.Elapsed += MusicTimer_Elapsed;

        ShowLoading("尝试加载动画和生成缓存\n该步骤可能会耗时比较长\n请耐心等待".Translate());
        var started = DateTime.Now;
        //跨平台: 跨平台的 PetLoader.Graph 没有 Dispatcher 参数; 解码宽度按设置里的渲染分辨率, 与 Windows 版同
        Core.Graph = await Task.Run(() => petloader.Graph(Set.Resolution));
        Log($"动画扫描完成, 耗时={(DateTime.Now - started).TotalSeconds:0.0}秒 个数={petloader.GraphCount}");

        Main = new Main(Core);
        //插件可能在桌宠建好之前就登记了语音播放器
        AttachVoicePlayer();
        //触摸区域是从宠物配置里读的, 必须在 Core.Graph 就绪之后注册
        Main.Load_2_TouchEvent();
        Main.NoFunctionMOD = Set.CalFunState;

        //清空资源
        LoadTheme(Set.Theme);
        //加载字体
        LoadFont(Set.Font);

        ShowLoading("正在加载游戏\n该步骤可能会耗时比较长\n请耐心等待".Translate());

        //加载数据合理化:工作
        if (!Set["gameconfig"].GetBool("noAutoCal"))
        {
            foreach (var work in Core.Graph!.GraphConfig!.Works)
            {
                if (work.LevelLimit > 200)//导入的最大合理工作不能超过200级
                    work.LevelLimit = 200;
                work.FixOverLoad();//导入的工作默认1.2倍
            }
        }
        //加载数据合理化:自动工作
        foreach (var stp in SchedulePackage)
            stp.FixOverLoad();

        LoadTalk(Main);
        if (Main.WorkTimer != null)
            Main.WorkTimer.E_FinishWork += WorkTimer_E_FinishWork;

        Main.TimeHandle += Handle_Music;
        if (IsSteamUser)
            Main.TimeHandle += Handle_Steam;
        Main.TimeHandle += (x) => DiagnosisUPLoad();


        var tlv = Main.ToolBar.Tlv;
        Main.ToolBar.gdPanel.Children.Remove(tlv);
        var sp = new StackPanel();
        Grid.SetColumnSpan(sp, 3);
        sp.Orientation = Avalonia.Layout.Orientation.Horizontal;
        sp.Children.Add(tlv);
        tlvplus = new TextBlock();
        tlvplus.Margin = new Thickness(1);
        tlvplus.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        tlvplus.FontSize = 18;
        tlvplus.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
        sp.Children.Add(tlvplus);
        Main.ToolBar.gdPanel.Children.Add(sp);
        Main.TimeUIHandle += MWUIHandle;
        Main.ToolBar.EventMenuPanelShow += () => MWUIHandle(Main);

        //窗口部件
        winBetterBuy = new winBetterBuy(this);
        if (Set.PetHelper)
            LoadPetHelper();
        //菜单与托盘
        LoadToolBarMenu(Main);
        Main.SetMoveMode(Set.AllowMove, Set.SmartMove, Set.SmartMoveInterval * 1000);
        Main.SetLogicInterval((int)(Set.LogicInterval * 1000));
        if (Set.MessageBarOutside)
            Main.MsgBar?.SetPlaceOUT();

        DisplayGrid.Child = Main;
        HideLoading();

        //加载统一契约插件: 存档读好、控件挂好, 插件可以挂事件和加菜单了
        foreach (var uh in UnifiedHosts)
            try
            {
                uh.OnLoadPlugin();
            }
            catch (Exception e)
            {
                NoticeBox.Show("由于插件引起的游戏启动错误".Translate() + "\n" + e.ToString(), "由于插件引起的游戏启动错误".Translate() + '-' + uh.Plugin.PluginName);
            }

        Foods.ForEach(item => item.LoadImageSource(this));
        Photos.ForEach(item => item.LoadUserInfo(this));


        //添加基本物品项目 (根据名称添加)
        if (Set.PetGraph == "vup")
        {
            if (!Items.Any(x => x.Name == "L徽章"))
            {
                var itm = new Item()
                {
                    Name = "L徽章",
                    Desc = "我有异议! 证物档案 - L徽章 - 出示!\n请勿在法庭当证据出示".Translate(),
                    ItemType = "Item",
                    Price = 100,
                    IsSingle = true,
                    CanUse = false,
                };
                itm.LoadSource(this);
                ItemsAdd(itm);
            }
            if (!Items.Any(x => x.Name == "逗猫棒"))
            {
                var itm = new Item()
                {
                    Name = "逗猫棒",
                    Desc = "钓竿式逗猫棒. 一般挂的是鼠鼠,毛球,W等。这款挂的是珠颈斑鸠呢。\n谁说逗猫棒就不能逗人?".Translate(),
                    ItemType = "Toy",
                    Price = 100,
                    IsSingle = true,
                    Data = "meow",
                };
                itm.LoadSource(this);
                ItemsAdd(itm);
            }
            if (!Items.Any(x => x.Name == "泡泡枪"))
            {
                var itm = new Item()
                {
                    Name = "泡泡枪",
                    Desc = "粉白色带蝴蝶结装饰的泡泡枪, 没见到加肥皂水的地方, 莫非是高科技?\n对小朋友来说有点幼稚，但是对萝莉丝来说刚刚好".Translate(),
                    ItemType = "Toy",
                    Price = 100,
                    IsSingle = true,
                    Data = "bubbles",
                };
                itm.LoadSource(this);
                ItemsAdd(itm);
            }
            if (!Items.Any(x => x.Name == "球拍"))
            {
                var itm = new Item()
                {
                    Name = "球拍",
                    Desc = "老板牌的最新款碳纤维球拍. 内置辅助动力, 让您可以轻松用出\"零式发球\"\"天衣无缝\"等球技\n你刚刚说了，网球?".Translate(),
                    ItemType = "Toy",
                    Price = 100,
                    IsSingle = true,
                    Data = "tennis",
                };
                itm.LoadSource(this);
                ItemsAdd(itm);
            }
            if (!Items.Any(x => x.Name == "指南针"))
            {
                var itm = new Item()
                {
                    Name = "指南针",
                    Desc = "指?针, 指针已经扭曲了, 实在看不懂在指哪边, 而且时不时还会乱晃, 有点吓人\n我亲爱的达瓦里氏,这玩意怎么在乱晃啊".Translate(),
                    ItemType = "Tool",
                    Price = 100,
                    IsSingle = true,
                };
                itm.LoadSource(this);
                ItemsAdd(itm);
            }
        }
        //每日礼盒
        everydaygift();

        switch (Set["CGPT"][(gstr)"type"])
        {
            case "DIY":
                TalkAPIIndex = TalkAPI.FindIndex(x => x.APIName == Set["CGPT"][(gstr)"DIY"]);
                LoadTalkDIY();
                break;
            case "LB":
                TalkBox = new TalkSelect(this);
                Main.ToolBar.MainGrid.Children.Add(TalkBox);
                break;
        }

        Main.FunctionSpendHandle += StatisticsCalHandle;

        //窗口部件
        winSetting = new winGameSetting(this);

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

        // 等动画真正就绪再开始播放, 否则第一帧会是空的.
        // 交给 Main.Load_2_WaitGraph: 它会把加载失败的动画从 GraphsList 里摘掉
        // 并把原因记进 ErrorMessage, 而不是像之前那样干等到超时
        await Main.Load_2_WaitGraph(waited => ShowLoading("尝试加载动画和生成缓存\n该步骤可能会耗时比较长\n请耐心等待".Translate() + $"\n  {waited} / {petloader.GraphCount}"));

        Log($"宠物={petloader.PetName} 动画总数={Core.Graph.GraphsALL.Count}"
            + $" 就绪={Core.Graph.GraphsALL.Count(x => x.IsReady)} 失败={Main.ErrorMessage.Count}");
        foreach (var message in Main.ErrorMessage.Take(3))
            Log($"加载失败: {message}");
        HideLoading();

        Main.Load_4_Start();
        Log($"桌宠已启动, 当前动画={Main.DisplayType}");
        //成就和统计 
        GameSavesData.Statistics![(gint)"stat_open_times"]++;
        Main.MoveTimer.Elapsed += MoveTimer_Elapsed;
        Main.SayProcess.Add(Main_OnSay);
        Main.Event_TouchHead += Main_Event_TouchHead;
        Main.Event_TouchBody += Main_Event_TouchBody;

        HashCheck = HashCheck;

        //添加捏脸动画(若有)
        if (Core.Graph!.GraphConfig.Data.ContainsLine("pinch"))
        {
            var pin = Core.Graph!.GraphConfig.Data["pinch"];
            Main.Core.TouchEvent.Insert(0, new TouchArea(
                new Point(pin[(gdbe)"px"], pin[(gdbe)"py"]), new Size(pin[(gdbe)"sw"], pin[(gdbe)"sh"])
                , DisplayPinch, true));
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

        if (Set["SingleTips"].GetDateTime("tutorial") <= new DateTime(2023, 10, 20) && App.MainWindows.Count == 1)
        {
            Set["SingleTips"].SetDateTime("tutorial", DateTime.Now);
            if (LocalizeCore.CurrentCulture == "zh-Hans")
                ExtensionFunction.StartURL("https://wiki.exlb.net/vpet/tutorial");
            else if (LocalizeCore.CurrentCulture == "zh-Hant")
                ExtensionFunction.StartURL("https://wiki.exlb.net/zh-hant/vpet/tutorial");
            else
                ExtensionFunction.StartURL("https://wiki.exlb.net/en/vpet/tutorial");
        }
        if (!Set["SingleTips"].GetBool("helloworld"))
        {
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                Set["SingleTips"].SetBool("helloworld", true);
                NoticeBox.Show("欢迎使用虚拟桌宠模拟器!\n如果遇到桌宠爬不见了,可以在我这里设置居中或退出桌宠".Translate(),
                   "你好".Translate() + (IsSteamUser ? SteamClient.Name : Environment.UserName), 5000);
                //Thread.Sleep(2000);
                //Main.SayRnd("欢迎使用虚拟桌宠模拟器\n这是个中期的测试版,若有bug请多多包涵\n欢迎加群虚拟主播模拟器430081239或在菜单栏-管理-反馈中提交bug或建议".Translate());
            });
        }
        if (Set["v"][(gint)"rank"] != DateTime.Now.Year && GameSavesData.Statistics?[(gint)"stat_total_time"] > 3600)
        {//年度报告提醒
            Task.Run(() =>
            {
                Thread.Sleep(Function.Rnd.Next(200000, 400000));
                Set["v"][(gint)"rank"] = DateTime.Now.Year;
                var btn = Dispatcher.Invoke(() =>
                {
                    var button = new Button()
                    {
                        Content = "点击前往查看".Translate(),
                        FontSize = 20,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                        Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                    };
                    button.Click += (x, y) =>
                    {
                        var panelWindow = new winCharacterPanel(this);
                        panelWindow.MainTab.SelectedIndex = 1;
                        panelWindow.Show();
                        Main.MsgBar?.ForceClose();
                    };
                    return button;
                });
                Main.Say("哼哼~主人，我的考试成绩出炉了哦，快来和我一起看我的成绩单喵".Translate(), btn, "shining");
            });
        }
        //生日设置提醒
        if (GameSavesData.Data.FindLine("HostBDay") == null)
        {
            Task.Run(() =>
            {
                Thread.Sleep(Function.Rnd.Next(100000, 200000));
                var btn = Dispatcher.Invoke(() =>
                {
                    var button = new Button()
                    {
                        Content = "设置".Translate(),
                        FontSize = 20,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                        Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                    };
                    button.Click += (x, y) =>
                    {
                        ShowSetting(2);
                    };
                    return button;
                });
                Main.Say("不要忘记设置生日时间哦 {0}，我会偷偷给你准备礼物的。".Translate(GameSavesData.GameSave.HostName), btn, "shining");
            });
        }
        else
        {
            var bdt = GameSavesData.GetDateTime("HostBDay");
            if (DateTime.Now.Month == bdt.Month && DateTime.Now.Day == bdt.Day)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(Function.Rnd.Next(100000, 200000));
                    HostBDay();
                });
            }
        }
        //生日蛋糕默认为加满的
        var food = new Food()
        {
            Name = "生日蛋糕",
            Likability = 5,
            Exp = 1000,
            Feeling = 100,
            StrengthDrink = Core.Save!.StrengthMax,
            StrengthFood = Core.Save!.StrengthMax,
            Type = FoodType.Food,
            isoverload = false,
            Desc = "萝莉丝的专属生日蛋糕，由3桶牛奶+2份糖+1个鸡蛋+3份小麦合。制作而成。营养丰富，可使所有状态回满。只有在萝莉丝生日才能吃的到哦。"
        };
        food.LoadImageSource(this);
#if BDAY
        food.Star = true;
#endif
        food.Price = (int)Math.Max(0, food.RealPrice * .5);
        Foods.Add(food);
        //SR2
        food = new Food()
        {
            Name = "生日蛋糕2",//2nd 惊喜生日蛋糕
            Likability = Core.Save!.Level / 10,
            Exp = Core.Save!.Level,
            Feeling = Core.Save!.FeelingMax / 20,
            StrengthDrink = Core.Save!.StrengthMax / 20,
            StrengthFood = Core.Save!.StrengthMax / 20,
            Type = FoodType.Food,
            isoverload = false,
            Desc = "主人给萝莉丝制作的惊喜蛋糕，每次品尝都会随机回满一个状态或者获得一次收益，还有神秘惊喜奖励!\n具体配方是：取出 香草 草中的 香草籽 并立刻将 香草荚 研磨投入 热牛奶 中， 香草籽 需要在含有 糖分 的瞬间投入 蛋糊，且需添加 柠檬 的 皮 之气息。 巧克力甘纳许 需要使用 秋 季后的 可可豆 并在 温热 的状态下使用 鲜奶油 进行混合。添加 天然 鲸油（澄清黄油）， 香草荚 需要在不切割的情况下萃取出 风味 并 避免 接触 金属，黏度维持在 绸缎状 以上。需要制备后时长不超过 4小时 的 香缇奶油，在将其粉碎（打发）前使其维持 冷藏 状态并在 冰水浴 之下 打发。"
        };
        food.LoadImageSource(this);
#if BDAY
        food.Star = true;
#endif
        food.Price = food.RealPrice;
        Foods.Add(food);
        //SR3
        food = new Food()
        {
            Name = "生日蛋糕3",//3rd 互动生日蛋糕
            Exp = Core.Save!.Level,
            Feeling = Core.Save!.FeelingMax / 20,
            StrengthDrink = Core.Save!.StrengthMax / 20,
            StrengthFood = Core.Save!.StrengthMax / 20,
            Type = FoodType.Food,
            isoverload = false,
            Desc = "为了报答主人去年生日制作的惊喜蛋糕，今年萝莉丝打算扳回一局，制作了带考验的生日蛋糕，每个蛋糕都有一个关于桌宠的问题，只有回答正确才能好好享用生日蛋糕，回答错误虽然也可以享用惊喜蛋糕但是会被调皮萝莉丝恶作剧，真是记仇呢。"
        };
        food.LoadImageSource(this);
#if BDAY
        food.Star = true;
#endif
        food.Price = food.RealPrice;
        Foods.Add(food);

#if BDAY
        if (DateTime.Now < new DateTime(2026, 8, 22) && DateTime.Now >= new DateTime(2026, 8, 14))
        {

            Task.Run(() =>
            {
                Thread.Sleep(10000);
                var btn = Dispatcher.Invoke(() =>
                {
                    var button = new Button()
                    {
                        Content = "查看生日公告/视频".Translate(),
                        FontSize = 20,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                        Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                    };
                    button.Click += (x, y) =>
                    {
                        if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                            ExtensionFunction.StartURL("https://www.bilibili.com/opus/1236088065966997511");
                        else
                            ExtensionFunction.StartURL("https://store.steampowered.com/news/app/1920960/view/705528385946780595");
                    };
                    return button;
                });
                string bdt;
                switch (DateTime.Now.Day)
                {
                    case 14:
                        bdt = "祝我生日快乐~祝我生日快乐~♪急急急，怎么晚上才能过生日喵！别藏了喵！{hostname}生日会的准备和生日礼物早就被聪明的{name}看透了喵！谢谢{hostname}！最喜欢你了喵！";
                        break;
                    case 15:
                        bdt = "昨天真开心喵，好想再过一次生日会喵，要不{hostname}陪我再看一遍生日会二创视频回放，然后在做好吃的给我！";
                        break;
                    case 16:
                        bdt = "三。三。。三。。。啊，已经是第三天了，我见到三就想起了三周年的事喵";
                        break;
                    case 17:
                        bdt = "四周年生日快乐{hostname}！什么？是第四天不是第四年喵？";
                        break;
                    case 18:
                        bdt = "啊！{hostname}！我在看前几天生日会拍的照片和视频！真开心呀！";
                        break;
                    case 19:
                        bdt = "嗯哼哼~怎么啦{hostname}？人家在想象明年{hostname}会怎么给{name}过生日喵~";
                        break;
                    default:
                        bdt = "生日已经过去了一个星期吗？和{hostname}在一起的时间真是过得很快喵~今年请继续和{name}在一起喵！";
                        break;
                }
                Main.Say(IText.ConverText(bdt.Translate(), Main), btn, "self");
                //Main.Say(bdt.Translate(), "self");
            });
        }
#endif
        newday = DateTime.Now.Day;
        Main.TimeHandle += NewDayHandle;
        Event_NewDay += () =>
        {
            var bdt = GameSavesData.GetDateTime("HostBDay");
            if (DateTime.Now.Month == bdt.Month && DateTime.Now.Day == bdt.Day)
            {
                HostBDay();
            }
        };
        Event_NewDay += everydaygift;

        //生日蛋糕的特殊功能
        Event_TakeItem += MainWindow_Event_TakeItem;
        //添加购买事件
        Event_TakeItemHandle += (item, count, from) => ActivityLogs.Add(new ActivityLog("take_" + from, item.TranslateName, count.ToString()));
        //添加工作事件
        Main.Event_WorkStart += (work) => ActivityLogs.Add(new ActivityLog("work_start", work.NameTrans));
        Main.Event_WorkEnd += (workinfo) => ActivityLogs.Add(new ActivityLog("work_end", workinfo.work.NameTrans, workinfo.Reason.ToString(), workinfo.spendtime.ToString("f0"), workinfo.count.ToString("f0")));
        Main.SayProcess.Add((sayinfo) =>
        {
            Task.Run(async () =>
            {
                ActivityLogs.Add(new ActivityLog("petsay", await sayinfo.GetSayText()));
            });
        });
        //if (DateTime.Now.DayOfYear == 1)
        //{
        //    Task.Run(() =>
        //    {
        //        Thread.Sleep(5000);
        //        Main.SayRnd("25年都跨过去了, 还有什么是跨不过的呢? {0}这一年辛苦了! 新年请多多指教!".Translate(GameSavesData.GameSave.HostName));
        //    });
        //}
        //修复因为26年元旦bug导致HashCheck失效的问题
        //简单来讲就是给所有有 2026跨年 这个照片的用户恢复一次HashCheck, 就当福利了(, 因为有这个照片的基本上都在bug周期里
        //请看到这个代码的人不要外传, 避免滥用
        //var photo25 = Photos.Find(x => x.Name == "2026跨年");
        //if (photo25?.IsUnlock == true && GameSavesData.HashCheck == false && GameSavesData.Data["debug"][(gbol)"fix26"] == false)
        //{
        //    GameSave_v2 ogs = GameSavesData;
        //    GameSavesData = new GameSave_v2(ogs.GameSave.Name);
        //    GameSavesData.Data = ogs.Data;
        //    GameSavesData.GameSave = ogs.GameSave;
        //    GameSavesData.Statistics = ogs.Statistics;
        //    HashCheck = true;
        //}
        //GameSavesData.Data["debug"][(gbol)"fix26"] = true;
        if (GameSavesData.HashCheck == false && GameSavesData["debug"].Find("losthash") == null)
        {
            GameSavesData["debug"][(gdat)"losthash"] = DateTime.Now;
        }
#if NewYear
        //仅新年功能
        if (DateTime.Now < new DateTime(2026, 2, 25))
        {
            Event_NewDay += NewYearSay;
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                NewYearSay();
            });
        }
#endif
        //MOD报错
        foreach (CoreMOD cm in CoreMODs)
            if (!cm.SuccessLoad)
                if (cm.Tag.Contains("该模组已损坏"))
                    MessageBoxX.Show("模组 {0} 插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系MOD作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "该模组已损坏".Translate());
                else if (Set.IsPassMOD(cm.Name) || !string.IsNullOrEmpty(cm.ErrorMessage))
                    MessageBoxX.Show("模组 {0} 的代码插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系MOD作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "{0} 未加载代码插件".Translate(cm.Name));
                else if (Set.IsMSGMOD(cm.Name))
                    MessageBoxX.Show("由于 {0} 包含代码插件\n虚拟桌宠模拟器已自动停止加载该插件\n请手动前往设置允许启用该mod 代码插件".Translate(cm.Name), "{0} 未加载代码插件".Translate(cm.Name));
        //动画错误
        if (Main.ErrorMessage.Count != 0)
        {
            var errstr = string.Join("\n------\n", Main.ErrorMessage);
            if (errstr.Contains("0000_core"))
            {
                MessageBoxX.Show("动画加载错误,请尝试以下解决方法修复问题:\n\t1. 删除游戏根目录`Cache`文件夹\n\t2. 删除游戏根目录`mod\\0000_core\\pet`文件夹,并在Steam验证游戏完整性".Translate(), "动画加载错误".Translate());
                var winrep = new winReport(this, errstr);
                winrep.tDescription.Text = "动画加载错误".Translate();
                winrep.Show();
            }
            else
                MessageBoxX.Show("动画加载错误\n虚拟桌宠模拟器未能成功加载该动画\n请联系MOD作者修复该问题".Translate() + '\n' + errstr, "动画加载错误".Translate());

            Main.ErrorMessage.Clear();
        }
        //这里写的都是共通的功能, 如果限定第一个MW使用的功能, 请前往

        if (GameSavesData.GameSave.Likability < 520)
            Core.Graph!.GraphsName[GraphType.Idel].Remove("like520");
        else if (Core.Graph!.FindGraph("like520", AnimatType.Single, IGameSave.ModeType.Happy) != null)
        {
            Event_NewDay += like520;
            like520();
        }
        if (Set.DeBug)
            ActivityLogs.CollectionChanged += ActivityLogs_WriteFile;

        // 界面走查 (调试参数): 桌宠跑起来之后再按脚本注入输入. 走查本身跑在 UI 线程上 (中间用 await 让出)
        if (App.UiWalk is { } walk)
            _ = Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(1000);
                await UiWalk.RunAsync(this, Main, walk.Script, walk.Output);
            });
    }

    /// <summary>
    /// 加载主题
    /// </summary>
    /// 找不到设置里指定的那套就退回第一套, 一套都没有就用 App.axaml 里的默认值 ——
    /// 主题缺了只该是不好看, 不该起不来
    public void LoadTheme(string themename)
    {
        if (Themes.Count == 0)
            return;
        Theme = Themes.Find(x => x.xName == themename) ?? Themes[0];
        var count = Dispatcher.Invoke(() => ThemeLoader.Apply(Theme.ThemeColor));
        var missing = ThemeLoader.MissingKeys(Theme.ThemeColor);
        Log($"主题: {Theme.xName} 写入 {count} 个配色"
            + (missing.Count > 0 ? $", 缺 {missing.Count} 个键: {string.Join(",", missing)}" : ""));
        //主题自带的图片包覆盖在 MOD 图片之上, 与 Windows 版同序
        foreach (var image in Theme.Images)
            ImageSources.AddorReplaceLine(image);
    }

    /// <summary>
    /// 加载字体
    /// </summary>
    public void LoadFont(string fontname)
    {
        IFont? cfont = Fonts.Find(x => x.Name == fontname);
        if (cfont == null)
        {
            return;
        }
        var font = cfont.Font;
        Application.Current!.Resources["MainFont"] = font;
        Log($"字体: {fontname}" + (FontLoader.IsRegistered(fontname) ? "" : " (ttf 没登记上, 用默认字体)"));
        //跨平台: 没有 Panuon 的 GlobalSettings, 全局字体由 App.axaml 里 Window 的样式读 MainFont
    }

    /// <summary>
    /// 把设置应用到窗口上
    /// </summary>
    private void ApplyWindowSettings()
    {
        MGrid.Width = BodySize * Set.ZoomLevel;
        Width = BodySize * Set.ZoomLevel;
        Height = BodySize * Set.ZoomLevel;
        Topmost = Set.TopMost;
    }

    public void SetZoomLevel(double zl)
    {
        Set.ZoomLevel = zl;
        //this.Height = 500 * zl;
        MGrid.Width = 500 * zl;
        //跨平台: 窗口尺寸也要跟着 (WPF 那边靠 SizeToContent)
        Width = BodySize * zl;
        Height = BodySize * zl;
        if (petHelper != null)
        {
            petHelper.Width = 50 * zl;
            petHelper.Height = 50 * zl;
            petHelper.ReloadLocation();
        }
    }

    /// <summary>
    /// 桌宠当前所在屏幕的缩放倍率
    /// </summary>
    /// 跨平台: WPF 的 Left/Top/ActualWidth 都是设备无关单位, Avalonia 的 Window.Position 是物理像素,
    /// 这四个属性把两边接起来, MWController 与各窗口的代码才能与 Windows 版逐字相同.
    /// 优先取屏幕自己的缩放而不是 RenderScaling: 窗口刚 Opened 时 RenderScaling 还可能是 1 (尚未落到具体屏幕上)
    /// 窗口关掉之后 (Closed 里的 Save 还要读 Left/Top 记退出位置) 原生窗口已经没了, 用最后一次记下的值 —— WPF 关了窗口 Left/Top 照样能读
    private double Scaling
    {
        get
        {
            if (PlatformImpl == null)
                return lastScaling;
            var scaling = Screens.ScreenFromWindow(this)?.Scaling ?? 0;
            if (scaling <= 0)
                scaling = RenderScaling;
            lastScaling = scaling > 0 ? scaling : 1;
            return lastScaling;
        }
    }
    private double lastScaling = 1;
    private PixelPoint lastPosition;
    /// <summary>
    /// 窗口左边缘的屏幕坐标 (设备无关单位), 对应 WPF 的 Window.Left
    /// </summary>
    public double Left
    {
        get => (PlatformImpl == null ? lastPosition : Position).X / Scaling;
        set => Position = new PixelPoint((int)Math.Round(value * Scaling), Position.Y);
    }
    /// <summary>
    /// 窗口上边缘的屏幕坐标 (设备无关单位), 对应 WPF 的 Window.Top
    /// </summary>
    public double Top
    {
        get => (PlatformImpl == null ? lastPosition : Position).Y / Scaling;
        set => Position = new PixelPoint(Position.X, (int)Math.Round(value * Scaling));
    }
    /// <summary>
    /// 对应 WPF 的 ActualWidth. 以显式设定的 Width 为准, Bounds 只是兜底: 桌宠窗口的尺寸是按缩放倍率算出来直接赋值的,
    /// 而 Bounds 要等一次排版才会跟上
    /// </summary>
    public double ActualWidth => double.IsNaN(Width) ? Bounds.Width : Width;
    /// <summary>
    /// 对应 WPF 的 ActualHeight
    /// </summary>
    public double ActualHeight => double.IsNaN(Height) ? Bounds.Height : Height;

    //跨平台: 与 Windows 版相同的两个 Steam 回调; Style→Theme, ToolTip→ToolTip.SetTip, Focus→Activate
    private void SteamFriends_OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        Dispatcher.Invoke(() =>
        {
            if (winMutiPlayer == null)
            {
                winMutiPlayer = new winMutiPlayer(this, lobby.Id.Value);
                winMutiPlayer.Show();
            }
            else
            {
                MessageBoxX.Show("已经有加入了一个访客表,无法再创建更多".Translate());
                winMutiPlayer.Activate();
            }
        });
    }

    private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
    {
        if (Set["banuser"][(gbol)friend.Id.Value.ToString()])
            return;
        if (!friend.IsPlayingThisGame)
        {
            ActivityLogs.Add(new ActivityLog("stream_invite_other", friend.Name));
            var tb = new TextBlock() { Text = "SID:" + friend.Id.Value, FontSize = 18 };
            ToolTip.SetTip(tb, "SID:" + friend.Id.Value);
            Button btn = new Button();
            btn.Content = "屏蔽该用户".Translate();
            btn.Theme = this.FindResource("ThemedButtonStyle") as ControlTheme;
            btn.FontSize = 18;
            btn.Padding = new Thickness(2, 0, 2, 0);
            btn.Margin = new Thickness(3, 0, 0, 0);
            btn.Click += (_, _) =>
            {
                Set["banuser"][(gbol)friend.Id.Value.ToString()] = true;
                Main.MsgBar?.ForceClose();
            };
            var stackpanal = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            stackpanal.Children.Add(tb);
            stackpanal.Children.Add(btn);
            Main.Say("你的好友{0}邀请你玩游戏,快去回应ta吧".Translate(friend.Name), msgcontent: stackpanal);
            return;
        }

        Dispatcher.Invoke(() =>
        {
            Button btn = new Button();
            btn.Content = "加入访客表".Translate();
            btn.Theme = this.FindResource("ThemedButtonStyle") as ControlTheme;
            btn.Click += (_, _) =>
            {
                if (winMutiPlayer == null)
                {
                    winMutiPlayer = new winMutiPlayer(this, lobby.Id);
                    winMutiPlayer.Show();
                    Main.MsgBar?.ForceClose();
                }
                else
                {
                    MessageBoxX.Show("已经有加入了一个访客表,无法再创建更多".Translate());
                    winMutiPlayer.Activate();
                }
            };
            ActivityLogs.Add(new ActivityLog("stream_invite_vpet", friend.Name));
            Main.Say("收到来自{0}的访客邀请,是否加入?".Translate(friend.Name), msgcontent: btn);
        });
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

    private void Restart_Closed(object? sender, EventArgs? e)
    {
        CloseConfirm = false;
        //统一契约插件: 单独兜一层
        foreach (var uh in UnifiedHosts)
            try
            {
                uh.OnEndGame();
            }
            catch { }
        Save();
        if (App.MainWindows.Count == 1)
        {
            //跨平台: 可执行文件路径从 Environment.ProcessPath 取 (Linux/macOS 上没有 .exe)
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, "exe"),
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        else
        {
            new MainWindow(PrefixSave, this).Show();
        }
        Exit();
    }
    private void Exit()
    {
        if (App.MainWindows.Count <= 1)
        {
            Task.Run(() =>
            {
                Thread.Sleep(10000);//等待10秒不退出强退
                Environment.Exit(0);
            });
            try
            {
                if (Core != null && Core.Graph != null)
                {
                    foreach (var igs in Core.Graph!.GraphsList.Values)
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
                while (Windows.Count != 0)
                {
                    var w = Windows[0];
                    w.Close();
                    Windows.Remove(w);
                }
                Main?.Dispose();
                AutoSaveTimer?.Stop();
                MusicTimer?.Stop();
                petHelper?.Close();
                winSetting?.Close();
                winBetterBuy?.Close();
                winWorkMenu?.Close();
                winGallery?.Close();
                if (winMutiPlayer != null)
                {
                    winMutiPlayer.lb.Leave();
                    winMutiPlayer.lb = default;
                    winMutiPlayer.Close();
                }

                if (IsSteamUser)
                    SteamClient.Shutdown();//关掉和Steam的连线
                if (notifyIcon != null)
                {
                    notifyIcon.IsVisible = false;
                    notifyIcon.Dispose();
                }
                notifyIcon?.Dispose();
            }
            finally
            {
                //跨平台: Windows 版这里是 Environment.Exit(0). 这个方法跑在窗口 Closed 回调里, 即 Cocoa 事件循环的调用栈上,
                //macOS 上在这儿直接 Environment.Exit 会在收拾原生对象时 abort() (退出时弹"意外退出"). 改走 Avalonia 的生命周期:
                //等这次 Closed 回调走完再 desktop.Shutdown() (它会结束主循环, 进程随之退出); 上面 10 秒的看门狗照旧兜底
                App.MainWindows.Remove(this);
                Dispatcher.Post(() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown());
            }
        }
        else
        {
            if (Core != null && Core.Graph != null)
            {
                foreach (var igs in Core.Graph!.GraphsList.Values)
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
            while (Windows.Count != 0)
            {
                Windows[0].Close();
            }
            Main?.Dispose();
            AutoSaveTimer?.Stop();
            MusicTimer?.Stop();
            petHelper?.Close();
            winSetting?.Close();
            winBetterBuy?.Close();
            winWorkMenu?.Close();
            winGallery?.Close();
            if (winMutiPlayer != null)
            {
                winMutiPlayer.lb.Leave();
                winMutiPlayer.lb = default;
                winMutiPlayer.Close();
            }
            App.MainWindows.Remove(this);
            if (notifyIcon != null)
            {
                notifyIcon.IsVisible = false;
                notifyIcon.Dispose();
            }
        }
    }

    private void WorkTimer_E_FinishWork(WorkTimer.FinishWorkInfo obj)
    {
        if (obj.work.Type == GraphHelper.Work.WorkType.Work)
        {
            GameSavesData.Statistics![(gint)"stat_single_profit_money"] = (int)obj.count;
        }
        else
        {
            GameSavesData.Statistics![(gint)"stat_single_profit_exp"] = (int)obj.count;
        }
    }

    private void Main_Event_TouchBody()
    {
        GameSavesData.Statistics![(gint)"stat_touch_body"]++;
    }

    private void Main_Event_TouchHead()
    {
        GameSavesData.Statistics![(gint)"stat_touch_head"]++;
    }

    private void Main_OnSay(SayInfo obj)
    {
        GameSavesData.Statistics![(gint)"stat_say_times"]++;
    }

    private void MoveTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        GameSavesData.Statistics![(gint)"stat_move_length"] += (int)(Math.Abs(Main.MoveTimerPoint.X) + Math.Abs(Main.MoveTimerPoint.Y));
    }

    private void AutoSaveTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        CheckGalleryUnlock();
        Save();
    }


    private void Window_Closed(object? sender, EventArgs? e)
    {
        CloseConfirm = false;
        //统一契约插件: 单独兜一层
        foreach (var uh in UnifiedHosts)
            try
            {
                uh.OnEndGame();
            }
            catch { }
        Save();
        Exit();
    }

    private void WindowX_LocationChanged(object? sender, PixelPointEventArgs e)
    {
        lastPosition = e.Point;
        petHelper?.SetLocation();
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    /// 跨平台: 原文复制自 Windows 版 MainWindow.cs 的 Save; 没有老式 MainPlugin, 存档目录按 ExtensionValue.BaseDirectory (运行目录)
    public void Save()
    {
        //保存日程表
        ScheduleTask?.Save();

        //保存物品栏
        foreach (var v in GameSavesData.Data.Assemblage.Keys.Where(x => x.StartsWith("item")).ToList())
            GameSavesData.Data.Remove(v);
        for (int i = 0; i < Items.Count; i++)
        {
            GameSavesData.Data.Add(LPSConvert.SerializeObjectToLine<Line>(Items[i], "item" + i.ToString()));
        }

        try
        {
            //统一契约插件
            foreach (var uh in UnifiedHosts)
                uh.Plugin.Save();
        }
        catch (Exception e)
        {
            MessageBoxX.Show(e.ToString(), "由于插件引起的保存错误".Translate());
        }
        //游戏存档
        if (Set != null)
        {
            var st = Set.SaveTimesPP;
            if (Main != null)
            {
                Set.VoiceVolume = Main.PlayVoiceVolume;
                List<string> list = new List<string>();
                Foods.FindAll(x => x.Star).ForEach(x => list.Add(x.Name));
                Set["betterbuy"]["star"].info = string.Join(",", list);
            }
            Set.StartRecordLastPoint = new Point(Dispatcher.Invoke(() => Left), Dispatcher.Invoke(() => Top));
            if (PrefixSave == "" && File.Exists(Path.Combine(ExtensionValue.BaseDirectory, "Setting.lps")))
            {//对于主设置的备份
                if (new FileInfo(Path.Combine(ExtensionValue.BaseDirectory, "Setting.lps")).Length < 10)
                {//文件大小小于10字节,可能是损坏的文件
                    File.Delete(Path.Combine(ExtensionValue.BaseDirectory, "Setting.lps"));
                }
                else
                {
                    if (File.Exists(Path.Combine(ExtensionValue.BaseDirectory, "Setting.bkp")))
                        File.Delete(Path.Combine(ExtensionValue.BaseDirectory, "Setting.bkp"));
                    File.Move(Path.Combine(ExtensionValue.BaseDirectory, "Setting.lps"), Path.Combine(ExtensionValue.BaseDirectory, "Setting.bkp"));
                }

            }
            File.WriteAllText(Path.Combine(ExtensionValue.BaseDirectory, $"Setting{PrefixSave}.lps"), Set.ToString());

            if (Core != null && Core.Save != null)
            {
                var saveslps = GameSavesData.ToLPS();
                var savesdata = saveslps.ToString();
                if (savesdata == null)
                    throw new Exception("Save data is null");

                //命名、轮换、备份都走共享后端, Windows 版用的是同一份实现
                SaveCatalog.Write(saveslps,
                    SaveCatalog.SaveDirectory(ExtensionValue.BaseDirectory),
                    SaveCatalog.BackupDirectory(ExtensionValue.BaseDirectory),
                    PrefixSave, st, Set.BackupSaveMaxNum);

                //老版本的存档在根目录, 读过一次之后就让位
                SaveCatalog.MigrateLegacy(ExtensionValue.BaseDirectory);

                //Steam云存档
                if (IsSteamUser)
                {
                    //云存档名里的编号是十六进制的, 排序交给共享后端, 免得两处解析方式不一样
                    var steamsave = SaveCatalog.ListCloud(SteamRemoteStorage.Files, PrefixSave);
                    while (steamsave.Count > Set.BackupSaveMaxNum)
                    {
                        SteamRemoteStorage.FileDelete(steamsave[0]);
                        steamsave.RemoveAt(0);
                    }
                    SteamRemoteStorage.FileWrite(SaveCatalog.CloudName(PrefixSave, DateTime.Now), Encoding.UTF8.GetBytes(savesdata));
                }
            }
        }
    }

    public void LoadLatestSave(string petname)
    {
        //设置里记的编号可能比目录里实际存在的小(比如换台机器把存档拷回来),
        //以目录里最大的为准, 免得新存档把旧的覆盖掉
        var saveDir = SaveCatalog.SaveDirectory(ExtensionValue.BaseDirectory);
        Set.SaveTimes = SaveCatalog.SyncSaveTimes(saveDir, PrefixSave, Set.SaveTimes);

        //从新到旧挨个试, 第一个读得动的就是要用的那份
        foreach (var latestsave in SaveCatalog.LoadCandidates(saveDir, PrefixSave))
            if (TryLoadSaveFile(latestsave))
                return;

        GameSavesData = new GameSave_v2(petname.Translate());
        //看看有没有备份,和备份对比下 (新建游戏)
        CheckBackupConsistency(GameSavesData, "New Game");
        Core.Save = GameSavesData.GameSave;
        HashCheck = HashCheck;
        GameSavesData.GameSave.Event_LevelUp += LevelUP;
    }

    /// <summary>
    /// 尝试加载指定存档文件
    /// </summary>
    /// <param name="saveFilePath"></param>
    /// <returns></returns>
    private bool TryLoadSaveFile(string saveFilePath)
    {
        if (string.IsNullOrEmpty(saveFilePath))
            return false;
#if !DEBUG
        try
        {
#endif
        var content = File.ReadAllText(saveFilePath);
        GameSave_v2 gs = new GameSave_v2(new LPS(content));
        // 检查备份一致性
        CheckBackupConsistency(gs, new FileInfo(saveFilePath).Name);

        if (SavesLoad(new LPS(content)))
            return true;
#if !DEBUG
        }
        catch (Exception ex)
        {
            MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
        }
#endif
        return false;
    }

    /// <summary>
    /// 与最新备份对比并提示用户
    /// </summary>
    private void CheckBackupConsistency(GameSave_v2 gs, string currentName)
    {
        try
        {
            //备份目录里最新的那份
            var bks = SaveCatalog.ListAll(ExtensionValue.BaseDirectory, PrefixSave)
                .Where(x => x.IsBackup).Cast<SaveCatalog.SaveFile?>().FirstOrDefault();
            if (bks != null)
            {
                try
                {
                    var gs2 = new GameSave_v2(new LPS(File.ReadAllText(bks.Value.Path)));
                    if (!(gs2.GameSave.Level == gs.GameSave.Level &&
                        gs2.GameSave.Exp == gs.GameSave.Exp &&
                        gs2.GameSave.Money == gs.GameSave.Money))
                    {
                        //和备份不一样,说明可能有问题, 提示用户
                        MessageBoxX.Show("检测到存档和备份不一致\n当前存档:{0} Lv{1} ${4:f0}\n备份存档:{2} Lv{3} ${5:f0}\n如需还原请在设置中加载备份还原存档"
                            .Translate(currentName, gs.GameSave.Level, Path.GetFileName(bks.Value.Path), gs2.GameSave.Level, gs.GameSave.Money, gs2.GameSave.Money)
                            , "存档不一致提示".Translate());

                    }
                }
                catch
                {
                    //备份损坏了,那就不管了
                }
            }
        }
        catch
        {

        }
    }

    /// <summary>
    /// 关闭防作弊
    /// </summary>
    public void HashCheckOff()
    {
        HashCheck = false;
    }

    /// <summary>
    /// 是否显示吃东西动画
    /// </summary>
    bool showeatanm = true;
    /// <summary>
    /// 显示吃东西(夹层)动画
    /// </summary>
    /// <param name="graphName">夹层动画名</param>
    /// <param name="imageSource">被夹在中间的图片</param>
    public void DisplayFoodAnimation(string graphName, Bitmap imageSource)
    {
        if (showeatanm)
        {//显示动画
            showeatanm = false;
            Main.Display(graphName, imageSource, () =>
            {
                showeatanm = true;
                if (Core.Controller!.EnableFunction)
                {
                    var newmod = Core.Save!.CalMode();
                    if (Core.Save!.Mode != newmod)
                    {
                        //魔改下参数以免不播放切换动画
                        Main.DisplayType!.Type = GraphType.Default;
                        //切换显示动画
                        Main.PlaySwitchAnimat(Core.Save!.Mode, newmod);
                        Core.Save!.Mode = newmod;
                    }
                    else
                        Main.DisplayToNomal();
                }
                else
                    Main.DisplayToNomal();
            });
        }
        else
        {//如果不显示动画, 则看看是不是有覆盖
            if (Main.DisplayType!.Animat != AnimatType.Single && Main.DisplayType.Name != graphName)
            {
                showeatanm = true;
            }
        }
    }

    /// <summary>
    /// 显示输入框
    /// </summary>
    public void ShowInputBox(string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false)
    {
        Dispatcher.Post(() => winInputBox.Show(this, title, text, defaulttext, ENDAction, AllowMutiLine, TextCenter, CanHide));
    }
    /// <summary>
    /// 从VPET服务器获取访客表相关信息的接口
    /// </summary>
    public async Task<string> GetVPetRoom(string action, int fixID = 0, ulong lobbyid = 0)
    {
        var checkkey = await GenerateAuthKey();
        string RequestURL = $"https://report.exlb.net/VPET/{action}?hoststeamid={SteamID}&fixid={fixID}&lobbyid={lobbyid}&checkkey={checkkey}";
        using System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        try
        {
            return client.GetStringAsync(RequestURL).Result;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    /// <summary>
    /// 写入运行日志
    /// </summary>
    /// 桌宠是常驻后台的程序, 出问题时用户看不到任何控制台输出, 必须有日志可查.
    /// 与 Windows 版的 Logs*.txt 作用一致.
    internal static void Log(string message)
    {
        try
        {
            var path = System.IO.Path.Combine(AppPaths.EnsureDirectory(AppPaths.DataRoot), "vpet.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 日志写不了也不能影响桌宠运行
        }
    }

    private void ShowLoading(string text) => Dispatcher.Post(() =>
    {
        LoadingText.Text = text;
        LoadingText.IsVisible = true;
    });

    private void HideLoading() => Dispatcher.Post(() => LoadingText.IsVisible = false);
}
