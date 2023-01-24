using Steamworks;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphCore;
using Microsoft.Win32;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using Application = System.Windows.Application;
using System.Timers;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        public System.Timers.Timer AutoSaveTimer = new System.Timers.Timer();
        public MainWindow()
        {
            //判断是不是Steam用户,因为本软件会发布到Steam
            //在 https://store.steampowered.com/app/1920960/VPet
            try
            {
#if DEBUG
                SteamClient.Init(2293870, true);
#else
                SteamClient.Init(1920960, true);
#endif
                SteamClient.RunCallbacks();
                IsSteamUser = SteamClient.IsValid;
                ////同时看看有没有买dlc,如果有就添加dlc按钮
                //if (Steamworks.SteamApps.IsDlcInstalled(1386450))
                //    dlcToolStripMenuItem.Visible = true;
            }
            catch
            {
                IsSteamUser = false;
            }

            //加载游戏设置
            if (new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps").Exists)
            {
                Set = new Setting(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps"));
            }
            else
                Set = new Setting("Setting#VPET:|\n");

            InitializeComponent();

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
            Dispatcher.Invoke(new Action(() => LoadingText.Content = "尝试加载游戏内容"));

            //加载游戏内容
            Core.Controller = new MWController(this);
            Core.Save = new Save("萝莉斯");

            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;

            if (Set.AutoSaveInterval > 0)
            {
                AutoSaveTimer.Interval = Set.AutoSaveInterval * 60000;
                AutoSaveTimer.Start();
            }


            Dispatcher.Invoke(new Action(() =>
            {
                Core.Graph = Pets[0].Graph();
                LoadingText.Visibility = Visibility.Collapsed;
                winSetting = new winGameSetting(this);
                Main = new Main(Core) { };
                Main.DefaultClickAction = () => { Dispatcher.Invoke(() => { Main.Say("你知道吗? 鼠标右键可以打开菜单栏"); }); };
                DisplayGrid.Child = Main;
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "退出桌宠", () => { Close(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "反馈中心", () => { new winReport(this).Show(); });
                Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Setting, "设置面板", () =>
                    {
                        Topmost = false;
                        winSetting.Show();
                    });

                Main.SetMoveMode(Set.AllowMove, Set.SmartMove, Set.SmartMoveInterval * 1000);
                Main.SetLogicInterval((int)(Set.LogicInterval * 1000));
                //加载图标
                notifyIcon = new NotifyIcon();
                ContextMenu m_menu;

                m_menu = new ContextMenu();
                m_menu.MenuItems.Add(new MenuItem("重置状态", (x, y) =>
                {
                    Main.CleanState();
                    Main.DisplayNomal();
                }));
                m_menu.MenuItems.Add(new MenuItem("屏幕居中", (x, y) =>
                {
                    Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                    Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                }));
                m_menu.MenuItems.Add(new MenuItem("反馈中心", (x, y) => { new winReport(this).Show(); }));

                m_menu.MenuItems.Add(new MenuItem("设置面板", (x, y) =>
                {
                    Topmost = false;
                    winSetting.Show();
                }));
                m_menu.MenuItems.Add(new MenuItem("退出桌宠", (x, y) => Close()));
                notifyIcon.ContextMenu = m_menu;

                notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/vpeticon.ico")).Stream);

                notifyIcon.Visible = true;

                if (Set["SingleTips"].GetBool("helloworld"))
                {
                    Set["SingleTips"].SetBool("helloworld", true);
                    notifyIcon.ShowBalloonTip(10, "你好 " + (IsSteamUser ? Steamworks.SteamClient.Name : Environment.UserName),
                        "欢迎使用虚拟桌宠模拟器!\n如果遇到桌宠爬不见了,可以在我这里设置居中或退出桌宠", ToolTipIcon.Info);
                    Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        Main.Say("欢迎使用虚拟桌宠模拟器\n这是个早期的测试版,若有bug请多多包涵\n欢迎在菜单栏-管理-反馈中提交bug或建议");
                    });
                }
            }));
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Save();
        }

        public Main Main;

        private void Window_Closed(object sender, EventArgs e)
        {
            Save();
            Main?.Dispose();
            notifyIcon?.Dispose();
            System.Environment.Exit(0);
        }

        //public void DEBUGValue()
        //{
        //    Dispatcher.Invoke(() =>
        //    {
        //        Console.WriteLine("Left: " + mwc.GetWindowsDistanceLeft());
        //        Console.WriteLine("Right: " + mwc.GetWindowsDistanceRight());
        //    });
        //    Thread.Sleep(1000);
        //    DEBUGValue(); 
        //}

    }
}
