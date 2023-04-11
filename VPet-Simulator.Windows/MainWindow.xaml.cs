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
using System.Diagnostics;
using ChatGPT.API.Framework;
using static VPet_Simulator.Core.GraphCore;
using Panuon.WPF.UI;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowX
    {
        private NotifyIcon notifyIcon;
        public System.Timers.Timer AutoSaveTimer = new System.Timers.Timer();
        public MainWindow()
        {
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
            //给正在玩这个游戏的主播/游戏up主做个小功能
            if (IsSteamUser)
            {
                rndtext.Add(new Tuple<string, Helper.SayType>($"关注 {SteamClient.Name} 谢谢喵", Helper.SayType.Shining));
            }
            else
            {
                rndtext.Add(new Tuple<string, Helper.SayType>($"关注 {Environment.UserName} 谢谢喵", Helper.SayType.Shining));
            }
            //加载游戏设置
            if (new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps").Exists)
            {
                Set = new Setting(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps"));
            }
            else
                Set = new Setting("Setting#VPET:|\n");

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\ChatGPTSetting.json"))
                CGPTClient = ChatGPTClient.Load(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\ChatGPTSetting.json"));
            //this.Width = 400 * ZoomSlider.Value;
            //this.Height = 450 * ZoomSlider.Value;
            InitializeComponent();

            this.Height = 500 * Set.ZoomLevel;
            this.Width = 500 * Set.ZoomLevel;

            if (Set.StartRecordLast)
            {
                var point = Set.StartRecordLastPoint;
                if (point.X != 0 || point.Y != 0)
                {
                    this.Left = point.X;
                    this.Top = point.Y;
                }
            }
            else
            {
                var point = Set.StartRecordPoint;
                Left = point.X; Top = point.Y;
            }

            //不存在就关掉
            var modpath = new DirectoryInfo(ModPath + @"\0000_core\pet\vup");
            if (!modpath.Exists)
            {
                MessageBox.Show("缺少模组Core,无法启动桌宠", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            Task.Run(GameLoad);
        }
        public new void Close()
        {
            if (Main == null)
            {
                base.Close();
            }
            else
            {
                Main.DisplayClose(() => Dispatcher.Invoke(base.Close));
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
            Save();
            try
            {
                //关闭所有插件
                foreach (MainPlugin mp in Plugins)
                    mp.EndGame();
            }
            catch { }
            Main?.Dispose();
            notifyIcon?.Dispose();
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            System.Environment.Exit(0);
        }

        private List<Tuple<string, Helper.SayType>> rndtext = new List<Tuple<string, Helper.SayType>>
        {
              new Tuple<string, Helper.SayType>("你知道吗? 鼠标右键可以打开菜单栏", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("如果你觉得目前功能太少,那就多挂会机. 宠物会自己动的", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("你知道吗? 你可以在设置里面修改游戏的缩放比例", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("想要宠物不乱动? 设置里可以设置智能移动或者关闭移动", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("有建议/游玩反馈? 来 菜单-系统-反馈中心 反馈吧", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("你现在乱点说话是说话系统的一部分,不过还没做,在做了在做了ing", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("你添加了虚拟主播模拟器和虚拟桌宠模拟器到愿望单了吗? 快去加吧", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("这游戏开发这么慢,都怪画师太咕了.\n记得多催催画师(@叶书天)画桌宠, 催的越快更新越快!", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("长按脑袋拖动桌宠到你喜欢的任意位置", Helper.SayType.Serious),
              new Tuple<string, Helper.SayType>("欢迎加入 虚拟主播模拟器群 430081239", Helper.SayType.Shining),
        };
        public long lastclicktime { get; set; }
        public void GameLoad()
        {
            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());
            if (IsSteamUser)//如果是steam用户,尝试加载workshop
            {
                Dispatcher.Invoke(new Action(() => LoadingText.Content = "尝试加载 Steam Workshop"));
                int i = 1;
                while (true)
                {
                    var page = Steamworks.Ugc.Query.ItemsReadyToUse.GetPageAsync(i++).Result;
                    if (page.HasValue && page.Value.ResultCount != 0)
                    {
                        foreach (Steamworks.Ugc.Item entry in page.Value.Entries)
                        {
                            if (entry.Directory != null)
                                Path.Add(new DirectoryInfo(entry.Directory));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            //加载mod
            foreach (DirectoryInfo di in Path)
            {
                if (!File.Exists(di.FullName + @"\info.lps"))
                    continue;
                Dispatcher.Invoke(new Action(() => LoadingText.Content = $"尝试加载 MOD数据: {di.Name}"));
                CoreMODs.Add(new CoreMOD(di, this));
            }
            //foreach (CoreMOD cm in CoreMODs)
            //    if (!cm.SuccessLoad)
            //        MessageBox.Show($"由于 {cm.Name} 包含代码插件\n虚拟桌宠模拟器已自动停止加载该插件\n请手动前往设置允许启用该mod 代码插件", $"{cm.Name} 未加载代码插件");
            Dispatcher.Invoke(new Action(() => LoadingText.Content = "尝试加载游戏存档"));

            //加载游戏内容
            Core.Controller = new MWController(this);
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps"))
                Core.Save = new Save(new LpsDocument(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps")).First());
            else
                Core.Save = new Save("萝莉斯");

            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;

            if (Set.AutoSaveInterval > 0)
            {
                AutoSaveTimer.Interval = Set.AutoSaveInterval * 60000;
                AutoSaveTimer.Start();
            }

            Dispatcher.Invoke(new Action(() =>
            {
                LoadingText.Content = "尝试加载动画和生成缓存";
                var pl = Pets.Find(x => x.Name == Set.PetGraph);
                Core.Graph = pl == null ? Pets[0].Graph() : pl.Graph();
                LoadingText.Content = "正在加载CGPT";

                winSetting = new winGameSetting(this);
                Main = new Main(Core) { };              
                if (!Set["CGPT"][(gbol)"enable"] && IsSteamUser)
                {
                    TalkBox = new TalkBox(this);
                    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                }
                else if (Set["CGPT"][(gbol)"enable"])
                {
                    TalkBox = new TalkBoxAPI(this);
                    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                }
                LoadingText.Content = "正在加载游戏";
                try
                {
                    //加载游戏创意工坊插件
                    foreach (MainPlugin mp in Plugins)
                        mp.LoadPlugin();
                }
                catch (Exception e)
                {
                    new winReport(this, "由于插件引起的游戏启动错误\n" + e.ToString()).Show();
                }
                Main.DefaultClickAction = () =>
                {
                    if (new TimeSpan(DateTime.Now.Ticks - lastclicktime).TotalSeconds > 20)
                    {
                        lastclicktime = DateTime.Now.Ticks;
                        var v = rndtext[Function.Rnd.Next(rndtext.Count)];
                        Dispatcher.Invoke(() => Main.Say(v.Item1, v.Item2));
                    }
                };
                DisplayGrid.Child = Main;
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "退出桌宠", () => { Close(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "开发控制台", () => { new winConsole(this).Show(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "反馈中心", () => { new winReport(this).Show(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "设置面板", () =>
            {
                Topmost = false;
                winSetting.Show();
            });

                Main.SetMoveMode(Set.AllowMove, Set.SmartMove, Set.SmartMoveInterval * 1000);
                Main.SetLogicInterval((int)(Set.LogicInterval * 1000));
                LoadingText.Visibility = Visibility.Collapsed;
                //加载图标
                notifyIcon = new NotifyIcon();
                notifyIcon.Text = "虚拟桌宠模拟器";
                ContextMenu m_menu;

                m_menu = new ContextMenu();
                m_menu.MenuItems.Add(new MenuItem("操作教程", (x, y) => { Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\Tutorial.html"); }));
                m_menu.MenuItems.Add(new MenuItem("重置状态", (x, y) =>
          {
              Main.CleanState();
              Main.DisplayNomal();
              Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
              Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
          }));
                m_menu.MenuItems.Add(new MenuItem("反馈中心", (x, y) => { new winReport(this).Show(); }));
                m_menu.MenuItems.Add(new MenuItem("开发控制台", (x, y) => { new winConsole(this).Show(); }));

                m_menu.MenuItems.Add(new MenuItem("设置面板", (x, y) =>
          {
              Topmost = false;
              winSetting.Show();
          }));
                m_menu.MenuItems.Add(new MenuItem("退出桌宠", (x, y) => Close()));

                LoadDIY();

                notifyIcon.ContextMenu = m_menu;

                notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/vpeticon.ico")).Stream);

                notifyIcon.Visible = true;
                notifyIcon.BalloonTipClicked += (a, b) =>
          {
              Topmost = false;
              winSetting.Show();
          };

                if (Set["SingleTips"].GetDateTime("tutorial") <= new DateTime(2023, 2, 23))
                {
                    Set["SingleTips"].SetDateTime("tutorial", DateTime.Now);
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\Tutorial.html");
                }
                if (!Set["SingleTips"].GetBool("helloworld"))
                {
                    Task.Run(() =>
              {
                  Thread.Sleep(2000);
                  Set["SingleTips"].SetBool("helloworld", true);
                  notifyIcon.ShowBalloonTip(10, "你好" + (IsSteamUser ? Steamworks.SteamClient.Name : Environment.UserName),
             "欢迎使用虚拟桌宠模拟器!\n如果遇到桌宠爬不见了,可以在我这里设置居中或退出桌宠", ToolTipIcon.Info);
                  Thread.Sleep(2000);
                  Main.Say("欢迎使用虚拟桌宠模拟器\n这是个早期的测试版,若有bug请多多包涵\n欢迎在菜单栏-管理-反馈中提交bug或建议", GraphCore.Helper.SayType.Shining);
              });
                }
                else if (Set["SingleTips"].GetDateTime("update") <= new DateTime(2023, 3, 27))
                {
                    if (Set["SingleTips"].GetDateTime("update") <= new DateTime(2023, 3, 4))
                        notifyIcon.ShowBalloonTip(10, "更新通知 03/04",
                   "现已接入ChatGPT, 右键和桌宠说话吧.\n已根据steamID独立创建的聊天API,调教你独属的桌宠吧", ToolTipIcon.Info);
                    else
                        notifyIcon.ShowBalloonTip(10, "更新通知 03/27",
                   "全新图形核心引擎,现在桌宠对内存的占用更小!", ToolTipIcon.Info);
                    Set["SingleTips"].SetDateTime("update", DateTime.Now);
                }
                Save();
            }));
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Save();
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            Save();
            try
            {
                //关闭所有插件
                foreach (MainPlugin mp in Plugins)
                    mp.EndGame();
            }
            catch { }
            Main?.Dispose();
            notifyIcon?.Dispose();
            System.Environment.Exit(0);
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

    }
}
