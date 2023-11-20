using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using System.Windows.Forms;
using System.Timers;
using LinePutScript;
using Panuon.WPF.UI;
using VPet_Simulator.Windows.Interface;
using System.Linq;
using LinePutScript.Localization.WPF;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using static VPet_Simulator.Windows.PerformanceDesktopTransparentWindow;
using Line = LinePutScript.Line;
using static VPet_Simulator.Core.GraphInfo;
using System.Globalization;
using LinePutScript.Dictionary;

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

            Task.Run(() =>
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

                GameLoad(Path);
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
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            else
            {
                new MainWindow(PrefixSave).Show();
            }
            Exit();
        }
        private void Exit()
        {
            if (App.MainWindows.Count <= 1)
            {
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
                                    ig3.Stop();
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
                                ig3.Stop();
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
                        try
                        {
                            if (SavesLoad(new LPS(File.ReadAllText(latestsave))))
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
            GameSavesData = new GameSave_v2(petname.Translate());
            Core.Save = GameSavesData.GameSave;
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
                notifyIcon.ContextMenu.MenuItems.Find("NotifyIcon_HitThrough", false).First().Checked = HitThrough;
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
