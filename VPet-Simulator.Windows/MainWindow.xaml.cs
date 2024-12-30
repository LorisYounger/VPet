using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.PerformanceDesktopTransparentWindow;
using Line = LinePutScript.Line;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowX
    {
        internal System.Windows.Forms.NotifyIcon notifyIcon;
        public PetHelper petHelper;
        public System.Timers.Timer AutoSaveTimer = new System.Timers.Timer();

        public MainWindow()
        {
            //处理ARGS
            Args = new LPS_D();
            foreach (var str in App.Args)
            {
                Args.Add(new Line(str));
            }

            //存档前缀
            if (Args.ContainsLine("prefix"))
            {
                PrefixSave = '-' + Args["prefix"].Info;
            }


#if X64
            PNGAnimation.MaxLoadNumber = 50;
#else
            PNGAnimation.MaxLoadNumber = 30;
#endif
            ExtensionValue.BaseDirectory = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;


            LocalizeCore.StoreTranslation = true;
            LocalizeCore.TranslateFunc = (str) =>
            {
                var destr = Sub.TextDeReplace(str);
                if (destr != str && LocalizeCore.CurrentLPS != null && LocalizeCore.CurrentLPS.Assemblage.TryGetValue(destr, out ILine line))
                {
                    return line.GetString();
                }
                if (str.Contains('_') && double.TryParse(str.Split('_').Last(), out double d))
                    return d.ToString();
                return null;
            };

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

            _dwmEnabled = Win32.Dwmapi.DwmIsCompositionEnabled();
            _hwnd = new WindowInteropHelper(this).EnsureHandle();

            GameInitialization();

            Task.Run(async () =>
            {
                //加载所有MOD
                List<DirectoryInfo> Path = new List<DirectoryInfo>();
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
                        await Dispatcher.InvokeAsync(new Action(() =>
                        {
                            LoadingText.Content = "Loading Steam Workshop\nDouble Click To Skip";
                            LoadingText.MouseDoubleClick += (_, _) =>
                            {
                                if ((string)LoadingText.Content == "Loading Steam Workshop\nDouble Click To Skip")
                                {
                                    NOCancel = false;
                                }
                            };
                        }));
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

                while (NOCancel && !tsk.IsCompleted)
                {
                    Thread.Sleep(500);
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


                Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading Translate")).Wait();
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

                await GameLoad(Path);
                if (IsSteamUser)
                {
                    //COD Check
                    if (!Set["v"][(gbol)"CODC"])
                    {
                        var di = new DirectoryInfo(ExtensionValue.BaseDirectory).Parent;
                        if (di.Exists && di.GetDirectories("*Call of Duty*").Length != 0)
                        {
                            Dispatcher.Invoke(() => NoticeBox.Show("检测到游戏库中包含使命召唤,建议不要在运行COD时运行桌宠\n根据社区反馈, COD可能会误报桌宠为作弊软件".Translate(),
                                "Call of Duty Check"));
                        }
                        Set["v"][(gbol)"CODC"] = true;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        var menuItem = new MenuItem()
                        {
                            Header = "访客表".Translate(),
                            HorizontalContentAlignment = HorizontalAlignment.Center
                        };
                        Main.ToolBar.MenuInteract.Items.Add(menuItem);

                        var menuCreate = new MenuItem()
                        {
                            Header = "创建".Translate(),
                            HorizontalContentAlignment = HorizontalAlignment.Center
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
                                winMutiPlayer.Focus();
                            }
                        };
                        menuItem.Items.Add(menuCreate);

                        var menuJoin = new MenuItem()
                        {
                            Header = "加入".Translate(),
                            HorizontalContentAlignment = HorizontalAlignment.Center
                        };
                        menuJoin.Click += (_, _) =>
                        {
                            if (winMutiPlayer == null)
                            {
                                winInputBox.Show(this, "请输入访客表ID".Translate(), "加入访客表".Translate(), "1860000", (id) =>
                                {
                                    if (ulong.TryParse(id, NumberStyles.HexNumber, null, out ulong lid))
                                    {
                                        winMutiPlayer = new winMutiPlayer(this, lid);
                                        winMutiPlayer.Show();
                                    }
                                });
                            }
                            else
                            {
                                MessageBoxX.Show("已经有加入了一个访客表,无法再创建更多".Translate());
                                winMutiPlayer.Focus();
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
            });
        }

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
                    winMutiPlayer.Focus();
                }
            });
        }

        private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
        {
            if (winMutiPlayer != null)
                return;
            if (!friend.IsPlayingThisGame)
            {
                Main.Say("你的好友{0}邀请你玩游戏,快去回应ta吧".Translate(friend.Name));
                return;
            }

            Dispatcher.Invoke(() =>
            {
                Button btn = new Button();
                btn.Content = "加入访客表";
                btn.Style = FindResource("ThemedButtonStyle") as Style;
                btn.Click += (_, _) =>
                {
                    winMutiPlayer = new winMutiPlayer(this, lobby.Id);
                    winMutiPlayer.Show();
                    Main.MsgBar.ForceClose();
                };
                Main.Say("收到来自{0}的访客邀请,是否加入?".Translate(friend.Name), msgcontent: btn);
            });
        }


        internal winMutiPlayer winMutiPlayer;

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
            if (App.MainWindows.Count == 1)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, "exe"),
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
            else
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
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                notifyIcon?.Dispose();
            }
        }


        public long lastclicktime { get; set; }

        public void LoadLatestSave(string petname)
        {
            if (Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
            {
                var ds = new List<string>(Directory.GetFiles(ExtensionValue.BaseDirectory + @"\Saves", $@"Save{PrefixSave}_*.lps"))
                    .OrderBy(x =>
                 {
                     if (int.TryParse(x.Split('_').Last().Split('.')[0], out int i))
                         return i;
                     return 0;
                 }).ToList();

                if (ds.Count != 0)
                {
                    int.TryParse(ds.Last().Split('_').Last().Split('.')[0], out int lastid);
                    if (Set.SaveTimes < lastid)
                    {
                        Set.SaveTimes = lastid;
                    }
                }
                for (int i = ds.Count - 1; i >= 0; i--)
                {
                    var latestsave = ds[i];
                    if (latestsave != null)
                    {
#if !DEBUG
                        try
                        {
#endif
                        if (SavesLoad(new LPS(File.ReadAllText(latestsave))))
                            return;
                        //MessageBoxX.Show("存档损毁,无法加载该存档\n可能是上次储存出错或Steam云同步导致的\n请在设置中加载备份还原存档", "存档损毁".Translate());
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
                        }
#endif
                    }
                }

            }
            GameSavesData = new GameSave_v2(petname.Translate());
            Core.Save = GameSavesData.GameSave;
            HashCheck = HashCheck;
            GameSavesData.GameSave.Event_LevelUp += LevelUP;
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
            CheckGalleryUnlock();
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
                (notifyIcon.ContextMenuStrip.Items.Find("NotifyIcon_HitThrough", false).First() as System.Windows.Forms.ToolStripMenuItem).Checked = HitThrough;
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
        /// <summary>
        /// 显示输入框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="text">文本</param>
        /// <param name="defaulttext">默认文本</param>
        /// <param name="ENDAction">结束事件</param>
        /// <param name="AllowMutiLine">是否允许多行输入</param>
        /// <param name="TextCenter">文本居中</param>
        /// <param name="CanHide">能否隐藏</param>
        public void ShowInputBox(string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false)
        {
            winInputBox.Show(this, title, text, defaulttext, ENDAction, AllowMutiLine, TextCenter, CanHide);
        }
    }
}
